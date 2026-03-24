using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[Serializable]
public class RegionQuizState
{
    public string regionName;
    public int askedCount = 0;
    public string currentLevel = "medium";
    public List<string> askedQuestionIds = new List<string>();
}

[Serializable]
public class QuestionResult
{
    public string questionId;
    public string topic;
    public string region;
    public string level;
    public string docType;

    public bool isCorrect;
    public float scoreRatio;

    public string selectedAnswer;
    public string correctAnswer;
}

public enum RegionPerformanceStatus
{
    Green,
    Yellow,
    Red
}

[Serializable]
public class RegionAnalysisResult
{
    public string region;
    public int totalQuestions;
    public int wrongCount;
    public int correctCount;
    public float averageScoreRatio;
    public RegionPerformanceStatus status;
}
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
    private Dictionary<string, List<Question>> motionQuestionsByRegion = new Dictionary<string, List<Question>>();
    
    private Dictionary<string, RegionQuizState> regionStates = new Dictionary<string, RegionQuizState>();
    private List<string> motionRegionQueue = new List<string>();
    private Question currentQuestion;
    private string currentQuestionRegion;
    private List<QuestionResult> quizResults = new List<QuestionResult>();
    private List<RegionAnalysisResult> finalRegionAnalysis = new List<RegionAnalysisResult>();
    private int finalTotalQuestions = 0;
    private int finalTotalCorrect = 0;
    private int finalTotalWrong = 0;
    private float finalOverallAverageScore = 0f;
    private int currentQuestionIndex = 0;

    private float remainingTime;
    private bool isTimedQuestion;
    private bool quizFinished = false;
    private bool isTimerPaused = false;

    private bool timeUpHandled = false;
    private bool questionLocked = false;

    private IEnumerator Start()
    {
        yield return null;

        LoadQuestionsByCategory();
        ShuffleQuestions();

        currentQuestionIndex = 0;
        PrepareCurrentQuestionAndShow();
    }
    public enum LevelChangeDirection
    {
        Down,
        Stay,
        Up
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

    private void BuildMotionQuestionPools(List<Question> allMotionQuestions)
    {
        motionQuestionsByRegion.Clear();

        foreach (string region in motionRegions)
        {
            List<Question> regionQuestions = allMotionQuestions
                .Where(q => !string.IsNullOrWhiteSpace(q.region) &&
                            q.GetNormalizedRegion().Equals(region, StringComparison.OrdinalIgnoreCase))
                .ToList();

            motionQuestionsByRegion[region] = regionQuestions;

            Debug.Log($"[POOL DEBUG] Region={region} | Total Questions={regionQuestions.Count}");

            int easyCount = regionQuestions.Count(q => q.IsLevel("easy"));
            int mediumCount = regionQuestions.Count(q => q.IsLevel("medium"));
            int hardCount = regionQuestions.Count(q => q.IsLevel("hard"));

            Debug.Log($"[POOL] Region={region} | Easy={easyCount} | Medium={mediumCount} | Hard={hardCount}");
        }
    }

    private void InitializeMotionRegionStates()
    {
        regionStates.Clear();

        foreach (string region in motionRegions)
        {
            RegionQuizState state = new RegionQuizState
            {
                regionName = region,
                askedCount = 0,
                currentLevel = "medium",
                askedQuestionIds = new List<string>()
            };

            regionStates[region] = state;
        }
    }

    private void BuildMotionRegionQueue()
    {
        motionRegionQueue.Clear();

        foreach (string region in motionRegions)
        {
            for (int i = 0; i < questionsPerRegion; i++)
            {
                motionRegionQueue.Add(region);
            }
        }

        motionRegionQueue = motionRegionQueue
            .OrderBy(x => UnityEngine.Random.value)
            .ToList();

        Debug.Log("[QUEUE] Motion region queue created:");
        foreach (string region in motionRegionQueue)
        {
            Debug.Log("[QUEUE ITEM] " + region);
        }
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

    public void OnMatchingQuestionAnswered(LevelChangeDirection direction)
    {
        if (NavigationState.CurrentQuizCategory != QuizCategory.MotionSystem)
            return;

        if (string.IsNullOrWhiteSpace(currentQuestionRegion) || !regionStates.ContainsKey(currentQuestionRegion))
            return;

        RegionQuizState state = regionStates[currentQuestionRegion];

        string oldLevel = state.currentLevel;

        switch (direction)
        {
            case LevelChangeDirection.Up:
                state.currentLevel = GetNextLevel(state.currentLevel, true);
                break;

            case LevelChangeDirection.Down:
                state.currentLevel = GetNextLevel(state.currentLevel, false);
                break;

            case LevelChangeDirection.Stay:
                // level değişmiyor
                break;
        }

        Debug.Log(
            $"[MATCHING LEVEL UPDATE] Region={currentQuestionRegion} | Direction={direction} | OldLevel={oldLevel} | NewLevel={state.currentLevel}"
        );
    }

    private string GetNextLevel(string currentLevel, bool answeredCorrect)
    {
        string normalized = string.IsNullOrWhiteSpace(currentLevel)
            ? "medium"
            : currentLevel.Trim().ToLower();

        if (answeredCorrect)
        {
            switch (normalized)
            {
                case "easy":
                    return "medium";
                case "medium":
                    return "hard";
                case "hard":
                    return "hard";
                default:
                    return "medium";
            }
        }
        else
        {
            switch (normalized)
            {
                case "hard":
                    return "medium";
                case "medium":
                    return "easy";
                case "easy":
                    return "easy";
                default:
                    return "medium";
            }
        }
    }

    void LoadQuestionsByCategory()
    {
        Debug.Log("[TEST] Entered LoadQuestionsByCategory");

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

        if (NavigationState.CurrentQuizCategory == QuizCategory.MotionSystem)
        {
            BuildMotionQuestionPools(questionList.questions);
            InitializeMotionRegionStates();
            BuildMotionRegionQueue();

            questionList.questions = new List<Question>();
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

        Debug.Log($"[QuizManager] Loaded questions count = {questionList.questions.Count}");
        foreach (Question q in questionList.questions)
        {
            Debug.Log(
                $"[Question Debug] id={q.id} | region={q.GetNormalizedRegion()} | topic={q.topic} | level(raw)={q.level} | level(normalized)={q.GetNormalizedLevel()}"
            );
        }
    }

    private List<string> GetLevelPriorityOrder(string targetLevel)
    {
        string normalized = string.IsNullOrWhiteSpace(targetLevel)
            ? "medium"
            : targetLevel.Trim().ToLower();

        switch (normalized)
        {
            case "easy":
                return new List<string> { "easy", "medium", "hard" };

            case "hard":
                return new List<string> { "hard", "medium", "easy" };

            case "medium":
            default:
                return new List<string> { "medium", "easy", "hard" };
        }
    }

    private Question GetNextMotionSystemQuestion()
    {
        if (motionRegionQueue.Count == 0)
            return null;

        string region = motionRegionQueue[0];
        motionRegionQueue.RemoveAt(0);

        currentQuestionRegion = region;

        RegionQuizState state = regionStates[region];

        string targetLevel = state.askedCount < 2 ? "medium" : state.currentLevel;

        List<string> levelPriority = GetLevelPriorityOrder(targetLevel);
        List<Question> availableQuestions = new List<Question>();
        string actualSelectedLevel = string.Empty;

        foreach (string level in levelPriority)
        {
            availableQuestions = motionQuestionsByRegion[region]
                .Where(q => q.IsLevel(level) && !state.askedQuestionIds.Contains(q.id))
                .OrderBy(q => UnityEngine.Random.value)
                .ToList();

            if (availableQuestions.Count > 0)
            {
                actualSelectedLevel = level;
                break;
            }
        }

        if (availableQuestions.Count == 0)
        {
            Debug.LogError($"[ADAPTIVE] No available question found for region {region}");
            return null;
        }

        Question selected = availableQuestions[0];

        state.askedCount++;
        state.askedQuestionIds.Add(selected.id);

        Debug.Log(
            $"[ADAPTIVE] Selected Question | region={region} | askedCount={state.askedCount} | targetLevel={targetLevel} | actualLevel={actualSelectedLevel} | selectedId={selected.id} | selectedLevel={selected.GetNormalizedLevel()}"
        );

        return selected;
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
        if (NavigationState.CurrentQuizCategory == QuizCategory.MotionSystem)
        {
            timeUpHandled = false;
            questionLocked = false;
            isTimerPaused = false;

            currentQuestion = GetNextMotionSystemQuestion();

            if (currentQuestion == null)
            {
                EndQuiz();
                return;
            }

            Question q = NormalizeForUI(currentQuestion);

            Debug.Log("===== CURRENT QUESTION DEBUG =====");
            Debug.Log("id: " + q.id);
            Debug.Log("doc_type: " + q.doc_type);
            Debug.Log("topic: " + q.topic);
            Debug.Log("region: " + q.region);
            Debug.Log("level: " + q.level);
            Debug.Log("normalized level: " + q.GetNormalizedLevel());
            Debug.Log("body: " + q.body);
            Debug.Log("question_text: " + q.question_text);
            Debug.Log("rationale: " + q.rationale);
            Debug.Log("A: " + q.A);
            Debug.Log("B: " + q.B);
            Debug.Log("C: " + q.C);
            Debug.Log("D: " + q.D);
            Debug.Log("answer_text: " + q.answer_text);
            Debug.Log("time_limit_sec: " + q.time_limit_sec);

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

            ui.ShowQuestion(q);
            return;
        }

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

        Question normalQuestion = NormalizeForUI(questionList.questions[currentQuestionIndex]);

        Debug.Log("===== CURRENT QUESTION DEBUG =====");
        Debug.Log("id: " + normalQuestion.id);
        Debug.Log("doc_type: " + normalQuestion.doc_type);
        Debug.Log("topic: " + normalQuestion.topic);
        Debug.Log("region: " + normalQuestion.region);
        Debug.Log("level: " + normalQuestion.level);
        Debug.Log("normalized level: " + normalQuestion.GetNormalizedLevel());
        Debug.Log("body: " + normalQuestion.body);
        Debug.Log("question_text: " + normalQuestion.question_text);
        Debug.Log("rationale: " + normalQuestion.rationale);
        Debug.Log("A: " + normalQuestion.A);
        Debug.Log("B: " + normalQuestion.B);
        Debug.Log("C: " + normalQuestion.C);
        Debug.Log("D: " + normalQuestion.D);
        Debug.Log("answer_text: " + normalQuestion.answer_text);
        Debug.Log("time_limit_sec: " + normalQuestion.time_limit_sec);

        isTimedQuestion = normalQuestion.time_limit_sec > 0;

        if (isTimedQuestion)
        {
            remainingTime = normalQuestion.time_limit_sec;
            ui.UpdateTimer(remainingTime);
        }
        else
        {
            ui.UpdateTimer(-1f);
        }

        ui.ShowQuestion(normalQuestion);
    }

    private void RecordChoiceQuestionResult(Question q, string selectedOption, bool isCorrect)
    {
        QuestionResult result = new QuestionResult
        {
            questionId = q.id,
            topic = q.topic,
            region = q.region,
            level = q.GetNormalizedLevel(),
            docType = q.doc_type,
            isCorrect = isCorrect,
            scoreRatio = isCorrect ? 1f : 0f,
            selectedAnswer = selectedOption,
            correctAnswer = q.answer_text
        };

        quizResults.Add(result);

        Debug.Log(
            $"[RESULT] Choice | id={result.questionId} | region={result.region} | level={result.level} | isCorrect={result.isCorrect}"
        );
    }

    public void RecordMatchingQuestionResult(Question q, int correctCount, int totalCount)
    {
        if (q == null || totalCount <= 0)
            return;

        float accuracy = (float)correctCount / totalCount;
        bool isCorrect = accuracy >= 1f;

        QuestionResult result = new QuestionResult
        {
            questionId = q.id,
            topic = q.topic,
            region = q.region,
            level = q.GetNormalizedLevel(),
            docType = q.doc_type,
            isCorrect = isCorrect,
            scoreRatio = accuracy,
            selectedAnswer = $"{correctCount}/{totalCount}",
            correctAnswer = $"{totalCount}/{totalCount}"
        };

        quizResults.Add(result);

        Debug.Log(
            $"[RESULT] Matching | id={result.questionId} | region={result.region} | level={result.level} | accuracy={result.scoreRatio}"
        );
    }

    private List<RegionAnalysisResult> BuildRegionAnalysis()
    {
        List<RegionAnalysisResult> analysisResults = new List<RegionAnalysisResult>();

        var groupedByRegion = quizResults
            .Where(r => !string.IsNullOrWhiteSpace(r.region))
            .GroupBy(r => r.region);

        foreach (var group in groupedByRegion)
        {
            int totalQuestions = group.Count();
            int correctCount = group.Count(x => x.isCorrect);
            int wrongCount = group.Count(x => !x.isCorrect);
            float averageScoreRatio = group.Average(x => x.scoreRatio);

            RegionAnalysisResult result = new RegionAnalysisResult
            {
                region = group.Key,
                totalQuestions = totalQuestions,
                correctCount = correctCount,
                wrongCount = wrongCount,
                averageScoreRatio = averageScoreRatio,
                status = GetRegionPerformanceStatus(wrongCount, totalQuestions)
            };

            analysisResults.Add(result);
        }

        analysisResults = analysisResults
            .OrderByDescending(r => r.wrongCount)
            .ThenBy(r => r.averageScoreRatio)
            .ToList();

        return analysisResults;
    }

    private void BuildFinalQuizAnalysis()
    {
        finalTotalQuestions = quizResults.Count;
        finalTotalCorrect = quizResults.Count(r => r.isCorrect);
        finalTotalWrong = quizResults.Count(r => !r.isCorrect);
        finalOverallAverageScore = quizResults.Count > 0 ? quizResults.Average(r => r.scoreRatio) : 0f;

        finalRegionAnalysis = BuildRegionAnalysis();
    }

    private void PrintQuizAnalysisToConsole()
    {
        Debug.Log("========== QUIZ ANALYSIS ==========");

        Debug.Log($"[ANALYSIS] Total Questions: {finalTotalQuestions}");
        Debug.Log($"[ANALYSIS] Total Correct: {finalTotalCorrect}");
        Debug.Log($"[ANALYSIS] Total Wrong: {finalTotalWrong}");
        Debug.Log($"[ANALYSIS] Overall Average Score Ratio: {finalOverallAverageScore:F2}");

        Debug.Log("========== REGION ANALYSIS ==========");

        foreach (RegionAnalysisResult region in finalRegionAnalysis)
        {
            Debug.Log(
                $"[REGION ANALYSIS] Region={region.region} | Total={region.totalQuestions} | Correct={region.correctCount} | Wrong={region.wrongCount} | AvgScore={region.averageScoreRatio:F2} | Status={region.status}"
            );
        }

        Debug.Log("====================================");
    }

    private RegionPerformanceStatus GetRegionPerformanceStatus(int wrongCount, int totalQuestions)
    {
        if (totalQuestions <= 0)
            return RegionPerformanceStatus.Green;

        if (wrongCount >= 4)
            return RegionPerformanceStatus.Red;

        if (wrongCount >= 2)
            return RegionPerformanceStatus.Yellow;

        return RegionPerformanceStatus.Green;
    }


    // mevcut eski yapı aşağıda aynen kalsın

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
            topic = string.IsNullOrWhiteSpace(raw.topic) ? string.Empty : raw.topic.Trim(),
            region = raw.GetNormalizedRegion(),
            concept_type = string.IsNullOrWhiteSpace(raw.concept_type) ? string.Empty : raw.concept_type.Trim(),
            level = raw.GetNormalizedLevel(),
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

        Question sourceQuestion = NavigationState.CurrentQuizCategory == QuizCategory.MotionSystem
            ? currentQuestion
            : questionList.questions[currentQuestionIndex];

        Question q = NormalizeForUI(sourceQuestion);

        if (!string.IsNullOrEmpty(q.doc_type) && q.doc_type.Equals("matching", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[QuizManager] SubmitAnswer called for matching question - evaluation TBD.");
            return;
        }

        isTimerPaused = true;
        questionLocked = true;

        bool isCorrect = selectedOption == q.answer_text;

        if (NavigationState.CurrentQuizCategory == QuizCategory.MotionSystem)
        {
            if (!string.IsNullOrWhiteSpace(currentQuestionRegion) && regionStates.ContainsKey(currentQuestionRegion))
            {
                RegionQuizState state = regionStates[currentQuestionRegion];

                string oldLevel = state.currentLevel;
                string questionLevel = q.GetNormalizedLevel();

                if (state.askedCount >= 2)
                {
                    state.currentLevel = GetNextLevel(state.currentLevel, isCorrect);
                }
                else
                {
                    state.currentLevel = GetNextLevel(questionLevel, isCorrect);
                }

                Debug.Log(
                    $"[LEVEL UPDATE] Region={currentQuestionRegion} | QuestionId={q.id} | QuestionLevel={questionLevel} | AnswerCorrect={isCorrect} | OldStateLevel={oldLevel} | NewStateLevel={state.currentLevel} | AskedCount={state.askedCount}"
                );
            }
        }

        RecordChoiceQuestionResult(q, selectedOption, isCorrect);

        ui.ShowAnswerResult(
            correctOption: q.answer_text,
            selectedOption: selectedOption,
            rationale: q.rationale
        );
    }

    public void NextQuestion()
    {
        if (quizFinished) return;

        if (NavigationState.CurrentQuizCategory != QuizCategory.MotionSystem)
        {
            currentQuestionIndex++;
        }

        PrepareCurrentQuestionAndShow();
    }

    void EndQuiz()
    {
        quizFinished = true;
        BuildFinalQuizAnalysis();
        PrintQuizAnalysisToConsole();

        ui.ShowQuizFinishedResults(
            finalTotalCorrect,
            finalTotalWrong,
            finalOverallAverageScore,
            finalRegionAnalysis
        );
    }
}