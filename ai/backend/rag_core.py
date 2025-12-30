import os
from dotenv import load_dotenv

from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient
from openai import AzureOpenAI

load_dotenv(override=True)


def get_env(name: str) -> str:
    v = os.getenv(name)
    if not v:
        raise RuntimeError(f"Missing env var: {name}")
    return v


def answer_question(question: str) -> dict:

    search_client = SearchClient(
        endpoint=get_env("AZURE_SEARCH_ENDPOINT").rstrip("/"),
        index_name=get_env("AZURE_SEARCH_INDEX"),
        credential=AzureKeyCredential(get_env("AZURE_SEARCH_KEY")),
    )

    results = search_client.search(search_text=question, top=5)
    docs = [dict(r) for r in results]

    chunks = []
    for i, d in enumerate(docs, start=1):
        chunk = d.get("chunk")
        if chunk:
            chunks.append(f"[{i}] {chunk}")

    if not chunks:
        return {
            "answer": "Kaynaklarda bulamadım.",
            "sources": []
        }

    context = "\n---\n".join(chunks)

    client = AzureOpenAI(
        api_key=get_env("AZURE_OPENAI_API_KEY"),
        azure_endpoint=get_env("AZURE_OPENAI_ENDPOINT").rstrip("/"),
        api_version=get_env("AZURE_OPENAI_API_VERSION"),
    )

    resp = client.chat.completions.create(
        model=get_env("AZURE_OPENAI_CHAT_DEPLOYMENT"),
        messages=[
            {
                "role": "system",
                "content": (
                    "Sen 9. sınıf anatomi yardımcı asistanısın. "
                    "SADECE verilen kaynaklara dayanarak cevap ver. "
                    "Kaynaklarda yoksa 'Kaynaklarda bulamadım' de. "
                    "Cevabın sonunda [1], [2] gibi kaynak numaralarını yaz."
                ),
            },
            {
                "role": "user",
                "content": f"Soru: {question}\n\nKaynaklar:\n{context}"
            },
        ],
    )

    return {
        "answer": resp.choices[0].message.content,
        "sources": list(range(1, len(chunks) + 1))
    }
