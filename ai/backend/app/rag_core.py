import os
import json
from dotenv import load_dotenv

import chromadb
import google.generativeai as genai


# --------------------
# Setup
# --------------------
load_dotenv(override=True)

DIST_THRESHOLD = float(os.environ.get("RAG_DIST_THRESHOLD", "0.6"))

api_key = os.environ.get("GEMINI_API_KEY")
if not api_key:
    raise RuntimeError("GEMINI_API_KEY bulunamadı.")

genai.configure(api_key=api_key)

DB_PATH = os.environ.get("CHROMA_DB_PATH", "./chroma_db")
COLLECTION_NAME = "vr_anatomy_book"
client = chromadb.PersistentClient(path=DB_PATH)
collection = client.get_or_create_collection(COLLECTION_NAME)


# --------------------
# Simple memory
# Son 1 soru-cevap çiftini tutar
# --------------------
last_turn = {
    "question": None,
    "answer": None
}

FOLLOWUP_PHRASES = [
    "anlamadım",
    "tekrar anlat",
    "daha basit",
    "basit anlat",
    "bir daha açıkla",
    "başka şekilde anlat",
    "açıklar mısın",
    "yeniden anlat"
]


def short_for_context(doc: str, max_chars: int = 900) -> str:
    text = (doc or "").strip().replace("\n", " ")
    return text if len(text) <= max_chars else text[:max_chars].rstrip() + "..."


def parse_gemini_json(raw: str):
    raw = (raw or "").strip()

    if raw.startswith("```"):
        raw = raw.strip("`").strip()
        if raw.lower().startswith("json"):
            raw = raw[4:].strip()

    parsed = json.loads(raw)

    answer = parsed.get("answer")
    used_pages = parsed.get("used_pages") or []

    normalized_pages = []
    for page in used_pages:
        try:
            normalized_pages.append(int(page))
        except Exception:
            pass

    normalized_pages = list(dict.fromkeys(normalized_pages))

    if isinstance(answer, list):
        answer_text = "\n".join(f"- {item}" for item in answer if item)
    elif isinstance(answer, str):
        answer_text = answer.strip()
    else:
        answer_text = str(answer).strip() if answer is not None else ""

    return answer_text, normalized_pages


def generate_with_gemini(prompt: str) -> str:
    try:
        model = genai.GenerativeModel("models/gemini-2.5-flash")
        response = model.generate_content(prompt)
        return (response.text or "").strip()

    except Exception as e:
        msg = str(e)

        if "RESOURCE_EXHAUSTED" in msg or "429" in msg:
            raise RuntimeError("Generate rate limit/quota doldu. Biraz sonra tekrar dene.")

        if "NOT_FOUND" in msg or "404" in msg:
            raise RuntimeError("Model bulunamadı. Model adını kontrol et: models/gemini-2.5-flash")

        raise RuntimeError(f"Generate hatası: {msg}")


def embed_question(question: str):
    try:
        emb_resp = genai.embed_content(
            model="models/gemini-embedding-001",
            content=question,
            task_type="retrieval_query"
        )

        if isinstance(emb_resp, dict):
            return emb_resp["embedding"]

        return emb_resp.embedding

    except Exception as e:
        msg = str(e)

        if "RESOURCE_EXHAUSTED" in msg or "429" in msg:
            raise RuntimeError("Embed rate limit/quota doldu. Biraz sonra tekrar dene.")

        raise RuntimeError(f"Embed hatası: {msg}")


def answer_question(question: str, top_k: int = 6) -> dict:
    global last_turn

    question = (question or "").strip()

    if not question:
        return {
            "answer": "Lütfen bir soru yaz.",
            "used_pages": []
        }

    q_lower = question.lower()

    is_followup = (
        any(phrase in q_lower for phrase in FOLLOWUP_PHRASES)
        and last_turn["answer"]
    )

    # --------------------
    # 0) Follow-up cevabı
    # --------------------
    if is_followup:
        prompt = f"""
Sen bir anatomi eğitmenisin.

Öğrenci önceki cevabı anlamadığını söylüyor.
Aynı cevabı tekrar etme.
Aynı konuyu daha sade, daha kısa, daha anlaşılır ve farklı cümlelerle açıkla.
Gerekirse günlük hayattan basit bir benzetme kullan.
Cevap öğrenci dostu olsun.

Önceki soru:
{last_turn["question"]}

Önceki cevap:
{last_turn["answer"]}

Öğrencinin yeni mesajı:
{question}

Sadece JSON üret. Başka hiçbir şey yazma.

JSON:
{{"answer":"", "used_pages":[]}}
"""

        raw = generate_with_gemini(prompt)

        try:
            answer_text, used_pages = parse_gemini_json(raw)
        except Exception:
            answer_text = "Yanıt üretilemedi. Lütfen soruyu yeniden sor."
            used_pages = []

        last_turn["question"] = question
        last_turn["answer"] = answer_text

        return {
            "answer": answer_text,
            "used_pages": used_pages
        }

    # --------------------
    # 1) Question embedding
    # --------------------
    qvec = embed_question(question)

    # --------------------
    # 2) ChromaDB retrieval
    # --------------------
    results = collection.query(
        query_embeddings=[qvec],
        n_results=top_k,
        include=["documents", "metadatas", "distances"]
    )

    docs = (results.get("documents") or [[]])[0]
    metas = (results.get("metadatas") or [[]])[0]
    dists = (results.get("distances") or [[]])[0]

    if not docs:
        return {
            "answer": "Bu soruya kitapta doğrudan bir bölüm bulamadım.",
            "used_pages": []
        }

    best_dist = dists[0] if dists else 999.0

    print(
        f"[RAG] best_dist={best_dist:.3f} "
        f"threshold={DIST_THRESHOLD:.3f} "
        f"top_k={top_k} "
        f"q={question}"
    )

    print("[RAG] top distances:", [round(x, 4) for x in dists[:min(len(dists), 5)]])

    if best_dist > DIST_THRESHOLD:
        return {
            "answer": "Bu bilgi kitapta yok.",
            "used_pages": []
        }

    # --------------------
    # 3) Context hazırlama
    # --------------------
    context_blocks = []

    for doc, meta in zip(docs, metas):
        page = meta.get("page")
        context_blocks.append(f"(Sayfa {page}): {short_for_context(doc)}")

    context = "\n\n".join(context_blocks)

    prev_q = last_turn["question"]
    prev_a = last_turn["answer"]

    prompt = f"""
Sen bir anatomi eğitmenisin.
Sadece verilen BAĞLAM'a dayanarak cevap ver.

Kurallar:
- Eğer BAĞLAM soruyu cevaplamaya yetmiyorsa:
  answer = "Bu bilgi kitapta yok."
  used_pages = []
- Eğer BAĞLAM yeterliyse:
  answer: kısa, net, maddeli olabilir.
  used_pages: cevabı yazarken kullandığın BAĞLAM parçalarının sayfa numaraları. Tekrar yazma.
- Eğer yeni soru önceki konuşmayla ilişkili değilse:
  sadece yeni soruyu ve verilen BAĞLAM'ı dikkate al.
- 9. sınıf öğrencisinin anlayacağı sade Türkçe kullan.

Sadece JSON üret. Başka hiçbir şey yazma.

ÖNCEKİ SORU:
{prev_q}

ÖNCEKİ CEVAP:
{prev_a}

BAĞLAM:
{context}

SORU:
{question}

JSON:
{{"answer":"", "used_pages":[]}}
"""

    # --------------------
    # 4) Gemini cevap üretimi
    # --------------------
    raw = generate_with_gemini(prompt)

    try:
        answer_text, used_pages = parse_gemini_json(raw)

        if answer_text != "Bu bilgi kitapta yok." and not used_pages:
            fallback_pages = []

            for meta in metas:
                page = meta.get("page")
                try:
                    fallback_pages.append(int(page))
                except Exception:
                    pass

            used_pages = list(dict.fromkeys(fallback_pages))

    except Exception:
        answer_text = "Yanıt üretilemedi. Lütfen soruyu yeniden sor."
        used_pages = []

    last_turn["question"] = question
    last_turn["answer"] = answer_text

    return {
        "answer": answer_text,
        "used_pages": used_pages
    }


def print_available_models():
    print("=== Available models: generateContent destekleyenler ===")

    for model in genai.list_models():
        methods = getattr(model, "supported_generation_methods", [])
        if "generateContent" in methods:
            print(model.name, methods)


if __name__ == "__main__":
    print_available_models()