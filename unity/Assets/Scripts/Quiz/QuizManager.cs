using UnityEngine;
using System;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    [Header("Config (Fallback)")]
    [Tooltip("Soru süresizse timer gösterilmez. Bu değer sadece istersen fallback olarak kullanılabilir.")]
    public float totalQuizTime = 120f;

    [Header("References")]
    public QuizUIController ui;

    [Header("Data (Resources paths, without extension)")]
    public string basicConceptsJsonPath = "JsonFiles/Quiz/basic_concepts_quiz_data";
    public string motionSystemJsonPath   = "JsonFiles/Quiz/motion_system_quiz_data";
    public string circulationJsonPath    = "JsonFiles/Quiz/circulation_system_quiz_data";
    public string allQuestionsJsonPath   = "JsonFiles/Quiz/quiz_data";

    private QuestionList questionList;
    private int currentQuestionIndex = 0;

    // ✅ Per-question timer state
    private float remainingTime;
    private bool isTimedQuestion;
    private bool quizFinished = false;
    private bool isTimerPaused = false;

    void Start()
    {
        LoadQuestionsByCategory();
        ShuffleQuestions();

        currentQuestionIndex = 0;
        PrepareCurrentQuestionAndShow();
    }

    void Update()
    {
        if (quizFinished) return;

        // ✅ Süre sadece timed sorularda akar
        if (!isTimedQuestion) return;
        if (isTimerPaused) return;

        remainingTime -= Time.deltaTime;
        ui.UpdateTimer(remainingTime);

        if (remainingTime <= 0f)
        {
            // Süre bitti => bu soruyu bitir, quiz'i bitir veya otomatik ileri (senin tercihin)
            EndQuiz();
        }
    }

    void LoadQuestionsByCategory()
    {
        string selectedPath = allQuestionsJsonPath;

        switch (NavigationState.CurrentQuizCategory)
        {
            case QuizCategory.BasicConcepts:
                selectedPath = basicConceptsJsonPath;
                break;
            case QuizCategory.MotionSystem:
                selectedPath = motionSystemJsonPath;
                break;
            case QuizCategory.CirculationSystem:
                selectedPath = circulationJsonPath;
                break;
            case QuizCategory.AllQuestions:
            default:
                selectedPath = allQuestionsJsonPath;
                break;
        }

        Debug.Log($"[QuizManager] Category={NavigationState.CurrentQuizCategory} | Loading: Resources/{selectedPath}.json");

        TextAsset jsonFile = Resources.Load<TextAsset>(selectedPath);
        if (jsonFile == null)
        {
            Debug.LogError($"[QuizManager] JSON not found at Resources/{selectedPath}.json");
            questionList = new QuestionList { questions = new List<Question>() };
            return;
        }

        questionList = JsonUtility.FromJson<QuestionList>(jsonFile.text);
        if (questionList?.questions == null) questionList = new QuestionList { questions = new List<Question>() };

        Debug.Log($"[QuizManager] Loaded questions count = {questionList.questions.Count}");
    }

    void ShuffleQuestions()
    {
        if (questionList?.questions == null) return;

        for (int i = 0; i < questionList.questions.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, questionList.questions.Count);
            (questionList.questions[i], questionList.questions[r]) = (questionList.questions[r], questionList.questions[i]);
        }
    }

    void PrepareCurrentQuestionAndShow()
    {
        if (questionList == null || questionList.questions == null || questionList.questions.Count == 0)
        {
            EndQuiz();
            return;
        }

        if (currentQuestionIndex >= questionList.questions.Count)
        {
            EndQuiz();
            return;
        }

        // ✅ Normalize: Basic/Motion/Matching hepsi UI alanlarına otursun
        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);
        isTimerPaused = false; // yeni soru geldi, timer tekrar akabilir

        // ✅ Per-question time limit:
        // Kural: time_limit_sec <= 0 => süresiz
        isTimedQuestion = q.time_limit_sec > 0;

        if (isTimedQuestion)
        {
            remainingTime = q.time_limit_sec;
            ui.UpdateTimer(remainingTime);
        }
        else
        {
            // Süresiz soru: timer'ı kapat/hide et
            // (QuizUIController'da bunu desteklemek için küçük bir ek öneriyorum, aşağıda)
            ui.UpdateTimer(-1f);
        }

        // ✅ doc_type'a göre gösterim
        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            // Eğer UI'da matching ekranı varsa burada çağıracağız.
            // Şimdilik güvenli fallback: matching olduğunu logla + normal show çağır (istersen farklı yaparsın).
            Debug.Log($"[QuizManager] Matching question: {q.id}");

            // UI'nızda ShowMatchingQuestion yoksa bile kırılmasın diye normal ShowQuestion'a düşürüyoruz.
            // İleride UI'a ShowMatchingQuestion eklersen burayı ona çeviririz.
            ui.ShowQuestion(q);
            return;
        }

        ui.ShowQuestion(q);
    }

    // ✅ Basic + Movement + Matching normalize
    Question NormalizeForUI(Question raw)
    {
        // Kopya üzerinden çalışalım ki orijinal veri bozulmasın
        Question q = new Question
        {
            id = raw.id,
            valence = raw.valence,
            doc_type = raw.doc_type,
            title = raw.title,
            time_limit_sec = raw.time_limit_sec,
            tags = raw.tags,
            source = raw.source,
            topic = raw.topic,
            region = raw.region,
            concept_type = raw.concept_type,

            rationale = raw.rationale,

            // normalize edilecek alanlar
            body = raw.body,
            A = raw.A, B = raw.B, C = raw.C, D = raw.D,
            answer = raw.answer,

            // ham alanları da koru
            question_text = raw.question_text,
            option_a = raw.option_a,
            option_b = raw.option_b,
            option_c = raw.option_c,
            option_d = raw.option_d,
            correct_answer = raw.correct_answer,
            data = raw.data
        };

        // 1) Eğer body boşsa question_text'ten doldur
        if (string.IsNullOrWhiteSpace(q.body) && !string.IsNullOrWhiteSpace(q.question_text))
            q.body = q.question_text;

        // 2) Eğer A/B/C/D boşsa ama option_a.. varsa Basic format
        bool hasABCD = !(string.IsNullOrWhiteSpace(q.A) && string.IsNullOrWhiteSpace(q.B) && string.IsNullOrWhiteSpace(q.C) && string.IsNullOrWhiteSpace(q.D));
        if (!hasABCD)
        {
            if (!string.IsNullOrWhiteSpace(q.option_a)) q.A = q.option_a;
            if (!string.IsNullOrWhiteSpace(q.option_b)) q.B = q.option_b;
            if (!string.IsNullOrWhiteSpace(q.option_c)) q.C = q.option_c;
            if (!string.IsNullOrWhiteSpace(q.option_d)) q.D = q.option_d;
        }

        // 3) Basic correct_answer varsa ve answer boşsa, answer'ı harfe çek
        if (string.IsNullOrWhiteSpace(q.answer) && !string.IsNullOrWhiteSpace(q.correct_answer))
            q.answer = q.correct_answer.Trim();

        // 4) Movement format: data içinde options[] var, answer içinde {"correct_index":n} var
        // true_false / mcq_single için A/B/C/D'yi data.options'tan üret
        if (!string.IsNullOrWhiteSpace(q.data))
        {
            try
            {
                var parsed = JsonUtility.FromJson<OptionsWrapper>(q.data);
                if (parsed != null && parsed.options != null && parsed.options.Length > 0)
                {
                    // sadece A,B gerekli olabilir; ama güvenli şekilde dolduralım
                    q.A = parsed.options.Length > 0 ? parsed.options[0] : q.A;
                    q.B = parsed.options.Length > 1 ? parsed.options[1] : q.B;
                    q.C = parsed.options.Length > 2 ? parsed.options[2] : q.C;
                    q.D = parsed.options.Length > 3 ? parsed.options[3] : q.D;
                }
            }
            catch { /* data matching olabilir; ignore */ }
        }

        // answer JSON ise correct_index -> "A/B/C/D"
        if (!string.IsNullOrWhiteSpace(q.answer) && q.answer.TrimStart().StartsWith("{"))
        {
            // matching mi?
            if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
            {
                // matching: answer JSON pairs olarak kalabilir. UI bunu ayrıca işleyecek.
                // burada dokunmuyoruz
            }
            else
            {
                try
                {
                    var a = JsonUtility.FromJson<CorrectIndexWrapper>(q.answer);
                    if (a != null)
                    {
                        q.answer = IndexToLetter(a.correct_index);
                    }
                }
                catch { /* ignore */ }
            }
        }

        // time_limit_sec boş/NaN JSON'da genelde 0 gelmeli; ama güvenlik:
        if (q.time_limit_sec < 0) q.time_limit_sec = 0;

        return q;
    }

    string IndexToLetter(int index)
    {
        return index switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            _ => ""
        };
    }

    [Serializable]
    private class OptionsWrapper
    {
        public string[] options;
    }

    [Serializable]
    private class CorrectIndexWrapper
    {
        public int correct_index;
    }

    public void SubmitAnswer(string selectedOption)
    {
        if (quizFinished) return;

        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);

        // Matching sorular için (ileride UI değişince) burada ayrı kontrol yapılacak.
        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            // Şimdilik: matching UI geldikten sonra burada evaluate edeceğiz.
            // Crash etmesin diye sadece finished/result gösterme yapmıyoruz.
            Debug.Log("[QuizManager] SubmitAnswer called for matching question - evaluation TBD.");
            return;
        }

        isTimerPaused = true;
        
        // selectedOption burada "A/B/C/D" harfi olarak gelmeli
        ui.ShowAnswerResult(
            correctOption: q.answer,
            selectedOption: selectedOption,
            rationale: q.rationale
        );
    }

    public void NextQuestion()
    {
        if (quizFinished) return;

        currentQuestionIndex++;
        PrepareCurrentQuestionAndShow();
    }

    void EndQuiz()
    {
        quizFinished = true;
        ui.ShowQuizFinished();
    }
}