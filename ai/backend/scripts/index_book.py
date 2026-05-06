import json
import os
import re
import shutil
import time
from pathlib import Path

import chromadb
import google.generativeai as genai
from dotenv import load_dotenv
from google.api_core.exceptions import ResourceExhausted
from pypdf import PdfReader


BASE_DIR = Path(__file__).resolve().parents[1]

PDF_PATH = BASE_DIR / "data" / "book.pdf"
CHUNKS_PATH = BASE_DIR / "data" / "book_chunks.jsonl"
DB_PATH = BASE_DIR / "chroma_db"

COLLECTION_NAME = os.getenv("CHROMA_COLLECTION_NAME", "vr_anatomy_book")
EMBEDDING_MODEL = os.getenv("GEMINI_EMBEDDING_MODEL", "models/gemini-embedding-001")


def clean_text(text: str) -> str:
    text = text.replace("\x00", " ")
    text = re.sub(r"\s+", " ", text)
    return text.strip()


def chunk_text(text: str, max_chars: int = 1200, overlap: int = 200):
    text = clean_text(text)

    if not text:
        return []

    chunks = []
    start = 0

    while start < len(text):
        end = start + max_chars
        chunk = text[start:end].strip()

        if chunk:
            chunks.append(chunk)

        if end >= len(text):
            break

        start = end - overlap

    return chunks


def load_records_from_pdf():
    print(f"PDF bulundu, PDF üzerinden indexlenecek: {PDF_PATH}")

    reader = PdfReader(str(PDF_PATH))
    records = []

    for page_index, page in enumerate(reader.pages):
        page_number = page_index + 1
        page_text = clean_text(page.extract_text() or "")

        chunks = chunk_text(page_text)

        for chunk_index, chunk in enumerate(chunks):
            records.append(
                {
                    "id": f"page_{page_number}_chunk_{chunk_index}",
                    "page": page_number,
                    "chunk": chunk_index,
                    "source": "book.pdf",
                    "text": chunk,
                }
            )

        print(f"Sayfa {page_number}: {len(chunks)} chunk hazırlandı.")

    return records


def load_records_from_jsonl():
    print(f"PDF yok, hazır chunk dosyası kullanılacak: {CHUNKS_PATH}")

    records = []

    with open(CHUNKS_PATH, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()

            if not line:
                continue

            record = json.loads(line)

            text = clean_text(record.get("text", ""))

            if not text:
                continue

            records.append(
                {
                    "id": record["id"],
                    "page": record["page"],
                    "chunk": record["chunk"],
                    "source": record.get("source", "book.pdf"),
                    "text": text,
                }
            )

    print(f"JSONL içinden {len(records)} chunk okundu.")
    return records


def load_records():
    if PDF_PATH.exists():
        return load_records_from_pdf()

    if CHUNKS_PATH.exists():
        return load_records_from_jsonl()

    raise FileNotFoundError(
        "Ne PDF ne de chunk dosyası bulundu.\n"
        f"PDF_PATH: {PDF_PATH}\n"
        f"CHUNKS_PATH: {CHUNKS_PATH}"
    )


def get_embedding(text: str, max_retries: int = 3):
    for attempt in range(max_retries):
        try:
            response = genai.embed_content(
                model=EMBEDDING_MODEL,
                content=text,
                task_type="retrieval_document",
            )

            if isinstance(response, dict):
                return response["embedding"]

            return response.embedding

        except ResourceExhausted:
            wait_seconds = 10 * (attempt + 1)
            print(f"Embedding rate limit yedi. {wait_seconds} saniye bekleniyor...")
            time.sleep(wait_seconds)

        except Exception as e:
            message = str(e)

            if "429" in message or "RESOURCE_EXHAUSTED" in message:
                wait_seconds = 10 * (attempt + 1)
                print(f"Embedding quota/rate limit. {wait_seconds} saniye bekleniyor...")
                time.sleep(wait_seconds)
                continue

            raise

    raise RuntimeError("Embedding rate limit/quota nedeniyle indexleme tamamlanamadı.")


def reset_chroma_db():
    if DB_PATH.exists():
        print(f"Eski ChromaDB siliniyor: {DB_PATH}")
        shutil.rmtree(DB_PATH)

    DB_PATH.mkdir(parents=True, exist_ok=True)


def main():
    load_dotenv(BASE_DIR / ".env")

    api_key = os.getenv("GEMINI_API_KEY")

    if not api_key:
        raise RuntimeError("GEMINI_API_KEY bulunamadı. Render Environment içine eklenmeli.")

    genai.configure(api_key=api_key)

    print("Indexleme başlıyor...")
    print(f"BASE_DIR: {BASE_DIR}")
    print(f"PDF_PATH: {PDF_PATH}")
    print(f"CHUNKS_PATH: {CHUNKS_PATH}")
    print(f"DB_PATH: {DB_PATH}")
    print(f"COLLECTION_NAME: {COLLECTION_NAME}")
    print(f"EMBEDDING_MODEL: {EMBEDDING_MODEL}")

    records = load_records()

    if not records:
        raise RuntimeError("Indexlenecek chunk bulunamadı.")

    reset_chroma_db()

    chroma_client = chromadb.PersistentClient(path=str(DB_PATH))
    collection = chroma_client.get_or_create_collection(name=COLLECTION_NAME)

    ids = []
    documents = []
    embeddings = []
    metadatas = []

    total_chunks = 0

    for record in records:
        chunk_id = str(record["id"])
        chunk_text_value = clean_text(record["text"])
        page_number = int(record["page"])
        chunk_index = int(record["chunk"])

        print(f"Embedding hazırlanıyor: sayfa {page_number}, chunk {chunk_index}")

        embedding = get_embedding(chunk_text_value)

        ids.append(chunk_id)
        documents.append(chunk_text_value)
        embeddings.append(embedding)
        metadatas.append(
            {
                "page": page_number,
                "chunk": chunk_index,
                "source": record.get("source", "book.pdf"),
            }
        )

        total_chunks += 1

        if len(ids) >= 50:
            collection.add(
                ids=ids,
                documents=documents,
                embeddings=embeddings,
                metadatas=metadatas,
            )

            print(f"{total_chunks} chunk ChromaDB'ye yazıldı.")

            ids.clear()
            documents.clear()
            embeddings.clear()
            metadatas.clear()

        time.sleep(0.1)

    if ids:
        collection.add(
            ids=ids,
            documents=documents,
            embeddings=embeddings,
            metadatas=metadatas,
        )

    print("Indexleme tamamlandı.")
    print(f"Toplam chunk: {total_chunks}")
    print(f"Collection count: {collection.count()}")


if __name__ == "__main__":
    main()