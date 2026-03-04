import os, re
from dotenv import load_dotenv
from pypdf import PdfReader
import chromadb
import google.generativeai as genai
import time
from google.api_core.exceptions import ResourceExhausted
# API key yükle
load_dotenv()
api_key = os.environ.get("GEMINI_API_KEY")
if not api_key:
    raise RuntimeError("GEMINI_API_KEY bulunamadı.")
genai.configure(api_key=api_key)

PDF_PATH = "book.pdf"
DB_PATH = "./chroma_db"
COLLECTION_NAME = "vr_anatomy_book"

def clean_text(t: str) -> str:
    t = t.replace("\x00", " ")
    t = re.sub(r"[ \t]+", " ", t)
    t = re.sub(r"\n{3,}", "\n\n", t)
    return t.strip()

def chunk_text(text: str, size=3500, overlap=500):
    chunks = []
    i = 0
    while i < len(text):
        j = min(len(text), i + size)
        chunk = text[i:j].strip()
        if chunk:
            chunks.append(chunk)
        i += (size - overlap)
    return chunks
def id_exists(collection, doc_id: str) -> bool:
    """Bu id daha önce Chroma'ya yazıldı mı?"""
    try:
        got = collection.get(ids=[doc_id])
        return bool(got and got.get("ids"))
    except Exception:
        return False

def embed_with_retry(chunk: str, max_retries=6, base_sleep=5.0):
    """429 olursa bekleyip tekrar dener (backoff)."""
    for attempt in range(max_retries):
        try:
            resp = genai.embed_content(
                model="models/gemini-embedding-001",
                content=chunk,
                task_type="retrieval_document"
            )
            return resp["embedding"] if isinstance(resp, dict) else resp.embedding
        except ResourceExhausted:
            wait = base_sleep * (2 ** attempt)  # 5,10,20,40,80...
            print(f"⚠️ 429 quota/rate limit. {wait:.0f}s bekliyorum... ({attempt+1}/{max_retries})")
            time.sleep(wait)
    raise RuntimeError("Quota/rate limit nedeniyle embedding alınamadı. Daha sonra tekrar dene.")
def main():
    reader = PdfReader(PDF_PATH)
    client = chromadb.PersistentClient(path=DB_PATH)
    collection = client.get_or_create_collection(COLLECTION_NAME)

    total = 0

    for page_no, page in enumerate(reader.pages, start=1):
        raw_text = page.extract_text() or ""
        text = clean_text(raw_text)

        if not text:
            continue

        chunks = chunk_text(text)

        for idx, chunk in enumerate(chunks):
            doc_id = f"p{page_no}_c{idx}"

            #  Daha önce eklendiyse atla (resume)
            if id_exists(collection, doc_id):
                continue

            #  Embedding'i retry ile al
            vec = embed_with_retry(chunk)

            collection.add(
                ids=[doc_id],
                embeddings=[vec],
                documents=[chunk],
                metadatas=[{"page": page_no}]
            )

            total += 1

            #  Her isteğe küçük ara (rate limit riskini azaltır)
            time.sleep(0.6)

        print(f"Sayfa {page_no}: {len(chunks)} chunk eklendi.")

    print(f"🎉 Index tamamlandı. Toplam chunk: {total}")

if __name__ == "__main__":
    main()