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

    // ✅ NEW: time-up state (sadece 1 kez tetiklensin + şıklar kilitlensin)
    private bool timeUpHandled = false;
    private bool questionLocked = false;

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
            // ✅ Eskiden: EndQuiz();
            // ✅ Şimdi: quiz bitmesin, bu soruyu "time up" olarak göster + Next ile devam ettir
            if (!timeUpHandled)
            {
                HandleTimeUp();
            }
        }
    }

    private void HandleTimeUp()
    {
        timeUpHandled = true;
        questionLocked = true;
        isTimerPaused = true;

        remainingTime = 0f;
        ui.UpdateTimer(0f);

        // ✅ Burada "quiz finished" yerine, normalde cevap seçince çıkan result + next akışını göstereceğiz.
        // Bunun için UI'ya küçük bir fonksiyon ekleyeceğiz: ui.ShowTimeUpResult(...)
        ui.ShowTimeUpResult("Süreniz doldu. Cevap işaretlenmediği için bu soru boş sayıldı.");

        // Not: Next butonunun tıklaması zaten UI'da QuizManager.NextQuestion() çağırıyorsa
        // ekstra bir şey yapmamıza gerek yok.
        // Eğer UI next butonunu "answer sonrası" aktif ediyorsa, ShowTimeUpResult onunla aynı paneli açmalı.
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

        // ✅ NEW: yeni soruya geçerken state reset
        timeUpHandled = false;
        questionLocked = false;
        isTimerPaused = false;

        // ✅ Normalize: Basic/Motion/Matching hepsi UI alanlarına otursun
        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);

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
            ui.UpdateTimer(-1f);
        }

        // ✅ doc_type'a göre gösterim
        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[QuizManager] Matching question: {q.id}");
            ui.ShowQuestion(q);
            return;
        }

        ui.ShowQuestion(q);
    }

    // ✅ Basic + Movement + Matching normalize
    Question NormalizeForUI(Question raw)
    {
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

            body = raw.body,
            A = raw.A, B = raw.B, C = raw.C, D = raw.D,
            answer = raw.answer,

            question_text = raw.question_text,
            option_a = raw.option_a,
            option_b = raw.option_b,
            option_c = raw.option_c,
            option_d = raw.option_d,
            correct_answer = raw.correct_answer,
            data = raw.data
        };

        if (string.IsNullOrWhiteSpace(q.body) && !string.IsNullOrWhiteSpace(q.question_text))
            q.body = q.question_text;

        bool hasABCD = !(string.IsNullOrWhiteSpace(q.A) && string.IsNullOrWhiteSpace(q.B) && string.IsNullOrWhiteSpace(q.C) && string.IsNullOrWhiteSpace(q.D));
        if (!hasABCD)
        {
            if (!string.IsNullOrWhiteSpace(q.option_a)) q.A = q.option_a;
            if (!string.IsNullOrWhiteSpace(q.option_b)) q.B = q.option_b;
            if (!string.IsNullOrWhiteSpace(q.option_c)) q.C = q.option_c;
            if (!string.IsNullOrWhiteSpace(q.option_d)) q.D = q.option_d;
        }

        if (string.IsNullOrWhiteSpace(q.answer) && !string.IsNullOrWhiteSpace(q.correct_answer))
            q.answer = q.correct_answer.Trim();

        if (!string.IsNullOrWhiteSpace(q.data))
        {
            try
            {
                var parsed = JsonUtility.FromJson<OptionsWrapper>(q.data);
                if (parsed != null && parsed.options != null && parsed.options.Length > 0)
                {
                    q.A = parsed.options.Length > 0 ? parsed.options[0] : q.A;
                    q.B = parsed.options.Length > 1 ? parsed.options[1] : q.B;
                    q.C = parsed.options.Length > 2 ? parsed.options[2] : q.C;
                    q.D = parsed.options.Length > 3 ? parsed.options[3] : q.D;
                }
            }
            catch { /* ignore */ }
        }

        if (!string.IsNullOrWhiteSpace(q.answer) && q.answer.TrimStart().StartsWith("{"))
        {
            if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
            {
                // matching: dokunma
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

        // ✅ NEW: süre dolduysa / soru kilitliyse tıklamayı yok say
        if (questionLocked) return;

        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);

        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[QuizManager] SubmitAnswer called for matching question - evaluation TBD.");
            return;
        }

        isTimerPaused = true;

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