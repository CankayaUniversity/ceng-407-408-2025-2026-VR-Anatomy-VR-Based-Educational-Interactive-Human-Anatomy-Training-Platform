import os
import sys
from dotenv import load_dotenv

from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient

from openai import AzureOpenAI


def get_env(name: str, default: str | None = None) -> str:
    v = os.getenv(name, default)
    if not v:
        raise RuntimeError(f"Missing env var: {name}")
    return v


def try_vector_hybrid_search(search_client: SearchClient, question: str, k: int):
    """
    Hybrid: text + vector (if possible).
    If SDK/model/fields mismatch occurs, caller will fallback to text search.
    """

    embed_deployment = os.getenv("AZURE_OPENAI_EMBED_DEPLOYMENT")
    if not embed_deployment:
        raise RuntimeError("AZURE_OPENAI_EMBED_DEPLOYMENT not set; cannot do vector search.")

    oai = AzureOpenAI(
        api_key=get_env("AZURE_OPENAI_API_KEY"),
        azure_endpoint=get_env("AZURE_OPENAI_ENDPOINT").rstrip("/"),
        api_version=get_env("AZURE_OPENAI_API_VERSION"),
    )

    emb = oai.embeddings.create(model=embed_deployment, input=question).data[0].embedding

    from azure.search.documents.models import VectorizedQuery

    vector_query = VectorizedQuery(vector=emb, k_nearest_neighbors=k, fields="text_vector")

    results = search_client.search(
        search_text=question,         
        vector_queries=[vector_query], 
        top=k
    )
    return list(results)


def text_search(search_client: SearchClient, question: str, k: int):
    results = search_client.search(search_text=question, top=k)
    return list(results)


def build_context(docs: list[dict], max_chars: int = 6000) -> str:
    chunks = []
    used = 0
    for i, d in enumerate(docs, start=1):
        chunk = (d.get("chunk") or "").strip()
        if not chunk:
            continue

        meta_parts = []
        for key in ["source", "sourcefile", "file", "filename", "page", "pagenumber", "chunk_id", "id"]:
            if key in d and d[key] is not None:
                meta_parts.append(f"{key}={d[key]}")
        meta = (" | " + ", ".join(meta_parts)) if meta_parts else ""

        block = f"[{i}]{meta}\n{chunk}\n"
        if used + len(block) > max_chars:
            break
        chunks.append(block)
        used += len(block)

    return "\n---\n".join(chunks).strip()


def main():
    load_dotenv(override=True)

    question = " ".join(sys.argv[1:]).strip() if len(sys.argv) > 1 else ""
    if not question:
        print('Usage: python rag_chat.py "Kalbin görevi nedir?"')
        sys.exit(1)

    search_client = SearchClient(
        endpoint=get_env("AZURE_SEARCH_ENDPOINT").rstrip("/"),
        index_name=get_env("AZURE_SEARCH_INDEX"),
        credential=AzureKeyCredential(get_env("AZURE_SEARCH_KEY")),
    )

    TOP_K = 5
    docs = None

    try:
        docs = try_vector_hybrid_search(search_client, question, TOP_K)
        mode = "hybrid (text+vector)"
    except Exception:
        docs = text_search(search_client, question, TOP_K)
        mode = "text-only"

    docs_dicts = [dict(d) for d in docs]
    context = build_context(docs_dicts)

    print(f"\n[Retrieval mode: {mode}] Top results: {len(docs_dicts)}")
    if not context:
        print("No chunk text found in results. Check your index field names.")
        sys.exit(1)

    oai = AzureOpenAI(
        api_key=get_env("AZURE_OPENAI_API_KEY"),
        azure_endpoint=get_env("AZURE_OPENAI_ENDPOINT").rstrip("/"),
        api_version=get_env("AZURE_OPENAI_API_VERSION"),
    )

    chat_deployment = get_env("AZURE_OPENAI_CHAT_DEPLOYMENT")

    resp = oai.chat.completions.create(
        model=chat_deployment,
        messages=[
            {
                "role": "system",
                "content": (
                    "Sen 9. sınıf anatomi yardımcı asistanısın. "
                    "SADECE verilen kaynak parçalarına dayanarak cevap ver. "
                    "Kaynaklarda yoksa 'Kaynaklarda bulamadım' de. "
                    "Cevabın sonunda kullandığın kaynak numaralarını [1], [2] şeklinde belirt."
                ),
            },
            {"role": "user", "content": f"Soru: {question}\n\nKaynak parçalar:\n{context}"},
        ],
    )

    print("\n=== Answer ===")
    print(resp.choices[0].message.content)


if __name__ == "__main__":
    main()