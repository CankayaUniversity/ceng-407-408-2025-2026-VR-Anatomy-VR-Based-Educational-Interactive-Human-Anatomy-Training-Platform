from fastapi import FastAPI
from pydantic import BaseModel
from rag_core import answer_question

app = FastAPI(title="VR Anatomy RAG API")
from fastapi.middleware.cors import CORSMiddleware

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  
    allow_credentials=True,
    allow_methods=["*"],  
    allow_headers=["*"],
)


@app.get("/health")
def health():
    return {"status": "ok"}

class ChatRequest(BaseModel):
    question: str

@app.post("/ask")
def ask(req: ChatRequest):
    return answer_question(req.question)
