from fastapi import FastAPI

app = FastAPI(title="VR Anatomy RAG API")

@app.get("/health")
def health():
    return {"status": "ok"}
