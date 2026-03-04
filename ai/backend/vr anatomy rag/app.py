import os
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from dotenv import load_dotenv
import chromadb
import google.generativeai as genai
import json

# --------------------
# Setup
# --------------------
load_dotenv()
# Distance threshold (0 iyi, büyüdükçe alakasız)
# Başlangıç için makul bir değer; sonra loglara bakıp ayarlayacağız.
DIST_THRESHOLD = float(os.environ.get("RAG_DIST_THRESHOLD", "0.58"))
api_key = os.environ.get("GEMINI_API_KEY")
if not api_key:
    raise RuntimeError("GEMINI_API_KEY bulunamadı.")
genai.configure(api_key=api_key)

DB_PATH = "./chroma_db"
COLLECTION_NAME = "vr_anatomy_book"

client = chromadb.PersistentClient(path=DB_PATH)
collection = client.get_or_create_collection(COLLECTION_NAME)

app = FastAPI(title="VR Anatomy RAG")


class AskRequest(BaseModel):
    question: str
    top_k: int = 4


@app.get("/health")
def health():
    return {"ok": True}



def short_for_context(doc: str, max_chars=900) -> str:
    t = (doc or "").strip().replace("\n", " ")
    return t if len(t) <= max_chars else (t[:max_chars].rstrip() + "...")


@app.post("/ask")
def ask(req: AskRequest):
    # 1) Embed the question
    try:
        emb_resp = genai.embed_content(
            model="models/gemini-embedding-001",
            content=req.question,
            task_type="retrieval_query"
        )
        qvec = emb_resp["embedding"] if isinstance(emb_resp, dict) else emb_resp.embedding
    except Exception as e:
        
        msg = str(e)
        if "RESOURCE_EXHAUSTED" in msg or "429" in msg:
            raise HTTPException(status_code=429, detail="Embed rate limit/quota doldu. Biraz sonra tekrar dene.")
        raise HTTPException(status_code=500, detail=f"Embed hatası: {msg}")

    # 2) Retrieve from Chroma (distances + threshold)
    results = collection.query(
        query_embeddings=[qvec],
        n_results=req.top_k,
        include=["documents", "metadatas", "distances"]
    )

    docs = (results.get("documents") or [[]])[0]
    metas = (results.get("metadatas") or [[]])[0]
    dists = (results.get("distances") or [[]])[0]

    if not docs:
        return {
            "answer": "Bu soruya kitapta doğrudan bir bölüm bulamadım.",
            #"sources": [],
            "used_pages": []
        }

    # En iyi (en yakın) sonucun distance'ı
    best_dist = dists[0] if dists else 999.0
    print(f"[RAG] best_dist={best_dist:.3f} threshold={DIST_THRESHOLD:.3f} top_k={req.top_k} q={req.question}")
  
    # Fren: En iyi sonuç bile uzaksa LLM'e gitme
    if best_dist > DIST_THRESHOLD:
        return {
            "answer": "Bu bilgi kitapta yok.",
            #"sources": [],
            "used_pages": []
        }
    print("[RAG] top distances:", [round(x, 4) for x in (dists[:min(len(dists), 5)] if dists else [])])
    # Context: her kaynağı sayfa numarasıyla ve kısaltılmış metinle ver
    context_blocks = []
    for d, m in zip(docs, metas):
        page = m.get("page")
        context_blocks.append(f"(Sayfa {page}): {short_for_context(d)}")
    context = "\n\n".join(context_blocks)

    prompt = f"""
Sen bir anatomi eğitmenisin.
Sadece verilen BAĞLAM'a dayanarak cevap ver.

Kurallar:
- Eğer BAĞLAM soruyu cevaplamaya yetmiyorsa:
  answer = "Bu bilgi kitapta yok."
  used_pages = []
- Eğer BAĞLAM yeterliyse:
  answer: kısa, net, maddeli olabilir
  used_pages: cevabı yazarken kullandığın BAĞLAM parçalarının sayfa numaraları (tekrar yok)

Sadece JSON üret. Başka hiçbir şey yazma.

BAĞLAM:
{context}

SORU:
{req.question}

JSON:
{{"answer":"", "used_pages":[]}}
"""

    # 3) Generate answer
    try:
        model = genai.GenerativeModel("models/gemini-2.5-flash")
        out = model.generate_content(prompt)
        raw = (out.text or "").strip()

        # Gemini bazen JSON'u ```json ... ``` içinde döndürür
        if raw.startswith("```"):
            raw = raw.strip("`").strip()
            if raw.lower().startswith("json"):
                raw = raw[4:].strip()

        # JSON parse etmeye çalış
        try:
            parsed = json.loads(raw)

            ans = parsed.get("answer")
            used_pages = parsed.get("used_pages") or []
                        # used_pages'i int'e normalize et (string gelebiliyor)
            norm_pages = []
            for p in used_pages:
                try:
                    norm_pages.append(int(p))
                except Exception:
                    pass
            # tekrar yok, sırası korunsun
            used_pages = list(dict.fromkeys(norm_pages))
                # used_pages boş geldiyse fallback: retrieval'dan gelen sayfaları kullan
            if not used_pages:
                fallback_pages = []
                for m in metas:
                    p = m.get("page")
                    try:
                        fallback_pages.append(int(p))
                    except Exception:
                        pass
                used_pages = list(dict.fromkeys(fallback_pages))

            # answer bazen string, bazen liste gelebilir
            if isinstance(ans, list):
                # listeyi maddeli string'e çevir
                answer_text = "\n".join(f"- {a}" for a in ans if a)
            elif isinstance(ans, str):
                answer_text = ans.strip()
            else:
                answer_text = (str(ans) if ans is not None else "").strip()
        except Exception:
            answer_text = "Yanıt üretilemedi. Lütfen soruyu yeniden sor."
            used_pages = []

    except Exception as e:
        msg = str(e)
        if "RESOURCE_EXHAUSTED" in msg or "429" in msg:
            raise HTTPException(status_code=429, detail="Generate rate limit/quota doldu. Biraz sonra tekrar dene.")
        if "NOT_FOUND" in msg or "404" in msg:
            raise HTTPException(status_code=500, detail="Model bulunamadı. Model adını kontrol et (örn: models/gemini-2.5-flash).")
        raise HTTPException(status_code=500, detail=f"Generate hatası: {msg}")

 
    return {
        "answer": answer_text,
       # "sources": filtered_sources,
        "used_pages": used_pages
    }


def print_available_models():
    print("=== Available models (generateContent destekleyenler) ===")
    for m in genai.list_models():
        methods = getattr(m, "supported_generation_methods", [])
        if "generateContent" in methods:
            print(m.name, methods)


if __name__ == "__main__":
    print_available_models()