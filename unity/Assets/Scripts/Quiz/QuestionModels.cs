using System;
using System.Collections.Generic;

[Serializable]
public class QuestionList
{
    public List<Question> questions;
}

[Serializable]
public class Question
{
    // -----------------------------
    // Ana metadata (her dataset)
    // -----------------------------
    public string id;
    public string valence;
    public string doc_type;        // "mcq_single", "true_false", "matching", ...
    public string title;
    public string tags;
    public string source;

    public string topic;           // movement excelinde var
    public string region;          // movement excelinde var
    public string concept_type;    // basic excelinde olabilir (varsa kaybetmeyelim)

    // -----------------------------
    // Süre (soru bazlı)
    // -----------------------------
    // Kural: 0 veya negatif => "süresiz"
    // Excel boş gelirse genelde 0/NaN gibi dönüşür; JSON üretirken 0 basmak en temizi.
    public int time_limit_sec;

    // -----------------------------
    // UI'nin en stabil kullandığı alanlar (normalize edilmiş)
    // -----------------------------
    public string body;

    public string A;
    public string B;
    public string C;
    public string D;

    // MCQ için standart: "A"/"B"/"C"/"D"
    // Movement datasetinde answer bazen JSON string olabilir -> normalize aşamasında letter’a çevireceğiz.
    public string answer;

    public string rationale;

    // -----------------------------
    // Ham excel alanları (kayıpsız olsun)
    // -----------------------------
    // Basic excel
    public string question_text;
    public string option_a;
    public string option_b;
    public string option_c;
    public string option_d;
    public string correct_answer;  // genelde "A"/"B"/"C"/"D"

    // Movement excel (mcq/true_false/matching)
    public string data;            // JSON string: {"options":[...]} veya {"left":[...],"right":[...]}
    // answer zaten yukarıda var; movement'ta JSON string gelebiliyor: {"correct_index":1} veya {"pairs":[[0,2],...]}
}