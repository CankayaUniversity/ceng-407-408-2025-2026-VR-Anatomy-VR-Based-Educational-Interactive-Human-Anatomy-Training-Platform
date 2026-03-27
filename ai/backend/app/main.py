import io
import logging
import os
import sys

# rag_core.py üst dizinde (backend/) olduğu için path'e ekliyoruz
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import Response
from pydantic import BaseModel

from rag_core import answer_question

logger = logging.getLogger(__name__)

app = FastAPI(title="VR Anatomy RAG API")

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
    try:
        return answer_question(req.question)
    except Exception as e:
        logger.error("ASK endpoint hatası: %s", e)
        return JSONResponse(
            status_code=500,
            content={
                "answer": "Şu anda cevap üretirken bir hata oluştu. Lütfen tekrar deneyin.",
                "error": str(e),
            },
        )


# ── Speech-to-Text (free Google Web Speech API via SpeechRecognition) ──

@app.post("/stt")
def speech_to_text(file: UploadFile = File(...)):
    import speech_recognition as sr

    recognizer = sr.Recognizer()
    audio_bytes = file.file.read()

    try:
        audio_io = io.BytesIO(audio_bytes)
        with sr.AudioFile(audio_io) as source:
            audio = recognizer.record(source)

        text = recognizer.recognize_google(audio, language="tr-TR")
        return {"text": text}

    except sr.UnknownValueError:
        return {"text": ""}
    except sr.RequestError as e:
        logger.error("STT servis hatası: %s", e)
        return {"text": "", "error": str(e)}
    except Exception as e:
        logger.error("STT beklenmeyen hata: %s", e)
        return {"text": "", "error": str(e)}


# ── Text-to-Speech (free Google Translate TTS via gTTS) ──

class TtsRequest(BaseModel):
    text: str


@app.post("/tts")
def text_to_speech(req: TtsRequest):
    from gtts import gTTS

    if not req.text.strip():
        return Response(status_code=400, content=b"", media_type="text/plain")

    try:
        tts = gTTS(text=req.text, lang="tr")
        buffer = io.BytesIO()
        tts.write_to_fp(buffer)
        buffer.seek(0)
        return Response(content=buffer.read(), media_type="audio/mpeg")

    except Exception as e:
        logger.error("TTS hatası: %s", e)
        return Response(
            status_code=500,
            content=str(e).encode(),
            media_type="text/plain",
        )
