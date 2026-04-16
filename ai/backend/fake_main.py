from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class AskRequest(BaseModel):
    question: str

@app.get("/")
def root():
    return {"status": "ok"}

@app.post("/ask")
def ask(req: AskRequest):
    q = req.question.lower().strip()

    if "kalp" in q and ("kaç" in q or "oda" in q or "odalı" in q):
        answer = "Kalp 4 odacıklıdır: sağ atrium, sağ ventrikül, sol atrium ve sol ventrikül."
    elif "kalp" in q:
        answer = "Kalp, dolaşım sisteminin merkezinde bulunan ve kanı vücuda pompalayan kas yapılı bir organdır."
    elif "aort" in q:
        answer = "Aort, kalpten çıkan en büyük atardamardır."
    elif "akciğer toplardamarı" in q or "pulmoner ven" in q:
        answer = "Akciğer toplardamarları, oksijenlenmiş kanı akciğerlerden kalbin sol atriumuna taşır."
    elif "frontal" in q:
        answer = "Frontal kemik, alın bölgesinde bulunan ve kafatasının ön kısmını oluşturan kemiktir."
    elif "kemik" in q:
        answer = "Kemikler vücuda destek sağlar, organları korur ve hareket sistemine katkıda bulunur."
    elif "dolaşım" in q:
        answer = "Dolaşım sistemi; kalp, kan ve damarlardan oluşur. Vücuda oksijen ve besin taşır."
    else:
        answer = f"Sorun alındı: '{req.question}'. Bu şu anda demo için çalışan fake API cevabıdır."

    return {"answer": answer}