import os
import json
import re
import time
from typing import Optional, List

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from google import genai
from google.genai import types


# --------------------------------------------------
# .env dosyasını yükle
# --------------------------------------------------

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
ENV_PATH = os.path.join(BASE_DIR, ".env")

load_dotenv(dotenv_path=ENV_PATH)

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.5-flash")
GEMINI_FALLBACK_MODEL = os.getenv("GEMINI_FALLBACK_MODEL", "gemini-2.5-flash-lite")

if not GEMINI_API_KEY:
    raise RuntimeError("GEMINI_API_KEY bulunamadı. .env dosyasını kontrol et.")


# --------------------------------------------------
# Gemini client
# --------------------------------------------------

client = genai.Client(api_key=GEMINI_API_KEY)


# --------------------------------------------------
# FastAPI app
# --------------------------------------------------

app = FastAPI(
    title="VR Anatomy Learning Review Backend",
    description="Öğrenmeye Başla modu için bilgi kartı metnini öğrenci seviyesinde basitleştirir.",
    version="1.0.0"
)


# Unity'den istek gelebilmesi için CORS açık
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# --------------------------------------------------
# Request / Response modelleri
# --------------------------------------------------

class SimpleBoneExplanationRequest(BaseModel):
    bone_name: str
    unit_name: Optional[str] = None
    original_text: Optional[str] = None


class SimpleBoneExplanationResponse(BaseModel):
    bone_name: str
    unit_name: Optional[str]
    simple_explanation: str
    key_points: List[str]
    speech_text: str


# --------------------------------------------------
# Basit test endpointleri
# --------------------------------------------------

@app.get("/")
def root():
    return {
        "message": "VR Anatomy Learning Review Backend is running."
    }


@app.get("/health")
def health():
    return {
        "status": "ok",
        "primary_model": GEMINI_MODEL,
        "fallback_model": GEMINI_FALLBACK_MODEL
    }


# --------------------------------------------------
# Yardımcı fonksiyonlar
# --------------------------------------------------

def clean_json_text(text: str) -> str:
    """
    Gemini bazen JSON'u ```json ... ``` içinde döndürebilir.
    Bu fonksiyon o gereksiz markdown işaretlerini temizler.
    """
    if not text:
        return ""

    text = text.strip()

    text = re.sub(r"^```json", "", text)
    text = re.sub(r"^```", "", text)
    text = re.sub(r"```$", "", text)

    return text.strip()


def build_prompt(req: SimpleBoneExplanationRequest) -> str:
    unit_name = req.unit_name or "Genel anatomi"

    if req.original_text and req.original_text.strip():
        source_part = f"""
Öğrencinin daha önce gördüğü bilgi kartı metni:
{req.original_text.strip()}

Bu bilgi kartındaki bilgilerin dışına çıkmadan daha basit ve anlaşılır anlat.
"""
    else:
        source_part = """
Bilgi kartı metni gönderilmedi.
Bu kemiği genel anatomi bilgisine dayanarak öğrenci seviyesinde basitçe anlat.
"""

    prompt = f"""
Sen bir VR anatomi eğitim uygulamasında konuşan öğretici avatarsın.

Öğrenci, daha önce anlatılan konuyu tam anlayamadı ve şu kemiği tekrar seçti:

Kemik adı: {req.bone_name}
Ünite: {unit_name}

{source_part}

Görevin:
Seçilen kemiğin bilgisini öğrenci seviyesinde, basit ve anlaşılır bir şekilde yeniden anlatmak.

Kurallar:
- Türkçe anlat.
- Eğer bilgi kartı metni verildiyse, sadece o metindeki bilgileri kullan.
- Kaynak metinde olmayan yeni tıbbi detaylar ekleme.
- Öğrencinin konuyu anlamakta zorlandığını düşün.
- Çok teknik tıbbi terimleri azalt.
- Gerekirse günlük hayattan kısa bir benzetme yap.
- Cümleleri kısa tut.
- En fazla 120 kelime kullan.
- Bilimsel olarak güvenli ve genel anatomi bilgisi ver.
- Tanı, tedavi veya hastalık tavsiyesi verme.
- Öğrenciye destekleyici, sakin ve açıklayıcı bir tonda konuş.
- Markdown kullanma.
- Cevabı sadece geçerli JSON olarak döndür.

JSON formatı:
{{
  "simple_explanation": "Kemiğin basit açıklaması.",
  "key_points": [
    "Birinci kısa madde.",
    "İkinci kısa madde.",
    "Üçüncü kısa madde."
  ],
  "speech_text": "Avatarın sesli okuyacağı doğal metin."
}}
"""

    return prompt


def generate_content_with_fallback_model(prompt: str):
    """
    Önce ana Gemini modelini dener.
    Ana model yoğunluk / hata verirse yedek modeli dener.
    """

    models_to_try = []

    if GEMINI_MODEL:
        models_to_try.append(GEMINI_MODEL)

    if GEMINI_FALLBACK_MODEL and GEMINI_FALLBACK_MODEL not in models_to_try:
        models_to_try.append(GEMINI_FALLBACK_MODEL)

    last_error = None

    for index, model_name in enumerate(models_to_try):
        try:
            print(f"Gemini modeli deneniyor: {model_name}")

            response = client.models.generate_content(
                model=model_name,
                contents=prompt,
                config=types.GenerateContentConfig(
                    temperature=0.4,
                    response_mime_type="application/json"
                )
            )

            print(f"Gemini başarılı model: {model_name}")
            return response

        except Exception as e:
            last_error = e

            print("\n--- GEMINI MODEL ERROR ---")
            print("Model:", model_name)
            print("Error:", str(e))
            print("--- END GEMINI MODEL ERROR ---\n")

            if index < len(models_to_try) - 1:
                print("Ana model başarısız oldu. Yedek modele geçiliyor...")
                time.sleep(1)

    raise last_error


# --------------------------------------------------
# Ana endpoint
# --------------------------------------------------

@app.post(
    "/learning/simple-bone-explanation",
    response_model=SimpleBoneExplanationResponse
)
def simple_bone_explanation(req: SimpleBoneExplanationRequest):
    if not req.bone_name or not req.bone_name.strip():
        raise HTTPException(
            status_code=400,
            detail="bone_name boş olamaz."
        )

    prompt = build_prompt(req)

    try:
        response = generate_content_with_fallback_model(prompt)

        raw_text = response.text or ""
        cleaned_text = clean_json_text(raw_text)

        try:
            parsed = json.loads(cleaned_text)
        except json.JSONDecodeError:
            parsed = {
                "simple_explanation": cleaned_text,
                "key_points": [],
                "speech_text": cleaned_text
            }

        simple_explanation = parsed.get("simple_explanation", "")
        key_points = parsed.get("key_points", [])
        speech_text = parsed.get("speech_text", simple_explanation)

        if not isinstance(simple_explanation, str):
            simple_explanation = str(simple_explanation)

        if not isinstance(speech_text, str):
            speech_text = str(speech_text)

        if not isinstance(key_points, list):
            key_points = []

        simple_explanation = simple_explanation.strip()
        speech_text = speech_text.strip()

        if not simple_explanation:
            simple_explanation = "Bu kemik hakkında basit açıklama üretilemedi."
            speech_text = simple_explanation

        return SimpleBoneExplanationResponse(
            bone_name=req.bone_name,
            unit_name=req.unit_name,
            simple_explanation=simple_explanation,
            key_points=key_points,
            speech_text=speech_text
        )

    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Gemini isteği başarısız oldu: {str(e)}"
        )