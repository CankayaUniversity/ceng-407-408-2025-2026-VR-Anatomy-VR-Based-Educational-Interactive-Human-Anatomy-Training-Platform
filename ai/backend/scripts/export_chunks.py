import json
import re
from pathlib import Path
from pypdf import PdfReader


BASE_DIR = Path(__file__).resolve().parents[1]
PDF_PATH = BASE_DIR / "data" / "book.pdf"
OUTPUT_PATH = BASE_DIR / "data" / "book_chunks.jsonl"
CHUNKS_PATH = BASE_DIR / "data" / "book_chunks.jsonl"


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


def main():
    if not PDF_PATH.exists():
        raise FileNotFoundError(f"PDF bulunamadı: {PDF_PATH}")

    reader = PdfReader(str(PDF_PATH))

    total_chunks = 0

    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        for page_index, page in enumerate(reader.pages):
            page_number = page_index + 1
            page_text = clean_text(page.extract_text() or "")

            chunks = chunk_text(page_text)

            for chunk_index, chunk in enumerate(chunks):
                record = {
                    "id": f"page_{page_number}_chunk_{chunk_index}",
                    "page": page_number,
                    "chunk": chunk_index,
                    "source": "book.pdf",
                    "text": chunk,
                }

                f.write(json.dumps(record, ensure_ascii=False) + "\n")
                total_chunks += 1

            print(f"Sayfa {page_number}: {len(chunks)} chunk çıkarıldı.")

    print(f"Tamamlandı. Toplam chunk: {total_chunks}")
    print(f"Çıktı dosyası: {OUTPUT_PATH}")


if __name__ == "__main__":
    main()