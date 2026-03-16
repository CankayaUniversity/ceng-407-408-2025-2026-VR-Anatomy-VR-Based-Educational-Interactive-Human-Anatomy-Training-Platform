using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class QuizManager : MonoBehaviour
{
    [Header("Config (Fallback)")]
    [Tooltip("Soru süresizse timer gösterilmez. Bu değer sadece istersen fallback olarak kullanılabilir.")]
    public float totalQuizTime = 120f;

    [Header("Motion System Quiz Settings")]
    public int questionsPerRegion = 5;

    private readonly string[] motionRegions = new string[]
    {
        "Head_Face",
        "Trunk",
        "Upper_Extremity",
        "Lower_Extremity",
        "Joints",
        "Muscle"
    };

    [Header("Basic Concepts Quiz Settings")]
    public int questionsPerConceptType = 5;

    private readonly string[] basicConceptTypes = new string[]
    {
        "Latin",
        "Abbreviation"
    };

    [Header("References")]
    public QuizUIController ui;

    [Header("Data (Resources paths, without extension)")]
    public string basicConceptsJsonPath = "JsonFiles/Quiz/basic_concepts_quiz_data";
    public string motionSystemJsonPath = "JsonFiles/Quiz/motion_system_quiz_data";
    public string circulationJsonPath = "JsonFiles/Quiz/circulation_system_quiz_data";
    public string allQuestionsJsonPath = "JsonFiles/Quiz/quiz_data";

    private QuestionList questionList;
    private int currentQuestionIndex = 0;

    private float remainingTime;
    private bool isTimedQuestion;
    private bool quizFinished = false;
    private bool isTimerPaused = false;

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
        if (!isTimedQuestion) return;
        if (isTimerPaused) return;

        remainingTime -= Time.deltaTime;
        ui.UpdateTimer(remainingTime);

        if (remainingTime <= 0f)
        {
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

        ui.ShowTimeUpResult("Süreniz doldu. Cevap işaretlenmediği için bu soru boş sayıldı.");
    }

    private List<Question> BuildMotionSystemQuiz(List<Question> allMotionQuestions)
    {
        List<Question> finalQuizQuestions = new List<Question>();

        foreach (string region in motionRegions)
        {
            List<Question> regionQuestions = allMotionQuestions
                .Where(q => !string.IsNullOrEmpty(q.region) &&
                            q.region.Trim().Equals(region, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => UnityEngine.Random.value)
                .ToList();

            Debug.Log($"[QuizManager] Region {region} question count: {regionQuestions.Count}");

            if (regionQuestions.Count < questionsPerRegion)
            {
                Debug.LogError(
                    $"Quiz başlatılamadı. Region {region} içinde en az {questionsPerRegion} soru olmalı. " +
                    $"Bulunan: {regionQuestions.Count}"
                );
                return null;
            }

            finalQuizQuestions.AddRange(regionQuestions.Take(questionsPerRegion));
        }

        finalQuizQuestions = finalQuizQuestions
            .OrderBy(q => UnityEngine.Random.value)
            .ToList();

        Debug.Log($"[QuizManager] Final Motion System quiz question count: {finalQuizQuestions.Count}");

        return finalQuizQuestions;
    }

    private List<Question> BuildBasicConceptsQuiz(List<Question> allBasicQuestions)
    {
        List<Question> finalQuizQuestions = new List<Question>();

        foreach (string conceptType in basicConceptTypes)
        {
            List<Question> conceptQuestions = allBasicQuestions
                .Where(q => !string.IsNullOrEmpty(q.concept_type) &&
                            q.concept_type.Trim().Equals(conceptType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => UnityEngine.Random.value)
                .ToList();

            Debug.Log($"[QuizManager] ConceptType {conceptType} question count: {conceptQuestions.Count}");

            if (conceptQuestions.Count < questionsPerConceptType)
            {
                Debug.LogError(
                    $"Quiz başlatılamadı. ConceptType {conceptType} içinde en az {questionsPerConceptType} soru olmalı. " +
                    $"Bulunan: {conceptQuestions.Count}"
                );
                return null;
            }

            finalQuizQuestions.AddRange(conceptQuestions.Take(questionsPerConceptType));
        }

        finalQuizQuestions = finalQuizQuestions
            .OrderBy(q => UnityEngine.Random.value)
            .ToList();

        Debug.Log($"[QuizManager] Final Basic Concepts quiz question count: {finalQuizQuestions.Count}");

        return finalQuizQuestions;
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

        string json = jsonFile.text?.Trim();

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[QuizManager] JSON file is empty.");
            questionList = new QuestionList { questions = new List<Question>() };
            return;
        }

        try
        {
            // 1) Eğer kök dizi ise
            if (json.StartsWith("["))
            {
                Question[] loadedQuestions = JsonHelper.FromJson<Question>(json);

                if (loadedQuestions == null || loadedQuestions.Length == 0)
                    questionList = new QuestionList { questions = new List<Question>() };
                else
                    questionList = new QuestionList { questions = new List<Question>(loadedQuestions) };
            }
            // 2) Eğer kökte "questions" objesi varsa
            else if (json.StartsWith("{"))
            {
                QuestionList loadedList = JsonUtility.FromJson<QuestionList>(json);

                if (loadedList == null || loadedList.questions == null || loadedList.questions.Count == 0)
                    questionList = new QuestionList { questions = new List<Question>() };
                else
                    questionList = loadedList;
            }
            else
            {
                Debug.LogError("[QuizManager] Unknown JSON format.");
                questionList = new QuestionList { questions = new List<Question>() };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuizManager] Failed to parse JSON: {e.Message}");
            questionList = new QuestionList { questions = new List<Question>() };
        }

        if (NavigationState.CurrentQuizCategory == QuizCategory.BasicConcepts)
        {
            List<Question> selectedQuestions = BuildBasicConceptsQuiz(questionList.questions);

            if (selectedQuestions == null || selectedQuestions.Count != basicConceptTypes.Length * questionsPerConceptType)
            {
                Debug.LogError("[QuizManager] Basic Concepts quiz oluşturulamadı.");
                questionList = new QuestionList { questions = new List<Question>() };
                return;
            }

            questionList.questions = selectedQuestions;
        }

        if (NavigationState.CurrentQuizCategory == QuizCategory.MotionSystem)
        {
            List<Question> selectedQuestions = BuildMotionSystemQuiz(questionList.questions);

            if (selectedQuestions == null || selectedQuestions.Count != motionRegions.Length * questionsPerRegion)
            {
                Debug.LogError("[QuizManager] Motion System quiz oluşturulamadı.");
                questionList = new QuestionList { questions = new List<Question>() };
                return;
            }

            questionList.questions = selectedQuestions;
        }

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

        timeUpHandled = false;
        questionLocked = false;
        isTimerPaused = false;

        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);

        Debug.Log("===== CURRENT QUESTION DEBUG =====");
        Debug.Log("id: " + q.id);
        Debug.Log("doc_type: " + q.doc_type);
        Debug.Log("body: " + q.body);
        Debug.Log("question_text: " + q.question_text);
        Debug.Log("rationale: " + q.rationale);
        Debug.Log("A: " + q.A);
        Debug.Log("B: " + q.B);
        Debug.Log("C: " + q.C);
        Debug.Log("D: " + q.D);
        Debug.Log("answer_text: " + q.answer_text);
        Debug.Log("time_limit_sec: " + q.time_limit_sec);

        if (q.data != null && q.data.options != null)
        {
            Debug.Log("data.options count: " + q.data.options.Count);
            for (int i = 0; i < q.data.options.Count; i++)
            {
                Debug.Log("option[" + i + "]: " + q.data.options[i]);
            }
        }
        else
        {
            Debug.Log("data.options is NULL");
        }

        if (q.answer != null)
        {
            Debug.Log("answer.correct_index: " + q.answer.correct_index);
        }
        else
        {
            Debug.Log("answer is NULL");
        }

        isTimedQuestion = q.time_limit_sec > 0;

        if (isTimedQuestion)
        {
            remainingTime = q.time_limit_sec;
            ui.UpdateTimer(remainingTime);
        }
        else
        {
            ui.UpdateTimer(-1f);
        }

        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[QuizManager] Matching question: {q.id}");
            ui.ShowQuestion(q);
            return;
        }

        ui.ShowQuestion(q);
    }

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
            level = raw.level,
            hint = raw.hint,

            rationale = raw.rationale,

            body = raw.body,
            A = raw.A,
            B = raw.B,
            C = raw.C,
            D = raw.D,
            answer_text = raw.answer_text,

            question_text = raw.question_text,
            option_a = raw.option_a,
            option_b = raw.option_b,
            option_c = raw.option_c,
            option_d = raw.option_d,
            correct_answer = raw.correct_answer,

            data = raw.data,
            answer = raw.answer
        };

        if (string.IsNullOrWhiteSpace(q.body) && !string.IsNullOrWhiteSpace(q.question_text))
            q.body = q.question_text;

        bool hasABCD =
            !(string.IsNullOrWhiteSpace(q.A) &&
              string.IsNullOrWhiteSpace(q.B) &&
              string.IsNullOrWhiteSpace(q.C) &&
              string.IsNullOrWhiteSpace(q.D));

        if (!hasABCD)
        {
            if (!string.IsNullOrWhiteSpace(q.option_a)) q.A = q.option_a;
            if (!string.IsNullOrWhiteSpace(q.option_b)) q.B = q.option_b;
            if (!string.IsNullOrWhiteSpace(q.option_c)) q.C = q.option_c;
            if (!string.IsNullOrWhiteSpace(q.option_d)) q.D = q.option_d;
        }

        if (string.IsNullOrWhiteSpace(q.answer_text) && !string.IsNullOrWhiteSpace(q.correct_answer))
            q.answer_text = q.correct_answer.Trim();

        if (q.data != null && q.data.options != null && q.data.options.Count > 0)
        {
            q.A = q.data.options.Count > 0 ? q.data.options[0] : q.A;
            q.B = q.data.options.Count > 1 ? q.data.options[1] : q.B;
            q.C = q.data.options.Count > 2 ? q.data.options[2] : q.C;
            q.D = q.data.options.Count > 3 ? q.data.options[3] : q.D;
        }

        if (string.IsNullOrWhiteSpace(q.answer_text) && q.answer != null)
        {
            if (string.IsNullOrEmpty(q.doc_type) || !q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
            {
                q.answer_text = IndexToLetter(q.answer.correct_index);
            }
        }

        if (q.time_limit_sec < 0)
            q.time_limit_sec = 0;

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

    public void SubmitAnswer(string selectedOption)
    {
        if (quizFinished) return;
        if (questionLocked) return;

        Question q = NormalizeForUI(questionList.questions[currentQuestionIndex]);

        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[QuizManager] SubmitAnswer called for matching question - evaluation TBD.");
            return;
        }

        isTimerPaused = true;
        questionLocked = true;

        ui.ShowAnswerResult(
            correctOption: q.answer_text,
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