# Veri Katmanı

Excel dosyalarınızı `raw/` içine koyun. Dışa aktarılan JSON'ları `exports/mcq/` veya `exports/facts/` içine atın.

## MCQ JSON şeması (örnek)
{
  "id": "os_mandibula_mcq_01",
  "valence": "assess",
  "doc_type": "mcq_single",
  "title": "Diş kemerleri - Konumu",
  "time_limit_sec": 30,
  "body": "Soru gövdesi",
  "steps": ["A) ...", "B) ...", "C) ...", "D) ..."],
  "answer": "A",
  "rationale": "Kısa açıklama",
  "source": "Ders kitabı s.53"
}
