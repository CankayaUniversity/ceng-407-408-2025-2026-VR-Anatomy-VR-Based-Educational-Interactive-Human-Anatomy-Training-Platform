using UnityEngine;

public class QuizManager : MonoBehaviour
{
    [Header("Config")]
    public float totalQuizTime = 120f;

    [Header("References")]
    public QuizUIController ui;

    private QuestionList questionList;
    private int currentQuestionIndex = 0;
    private float remainingTime;
    private bool quizFinished = false;

    void Start()
    {
        remainingTime = totalQuizTime;
        LoadQuestions();
        ShowCurrentQuestion();
    }

    void Update()
    {
        if (quizFinished)
            return;

        remainingTime -= Time.deltaTime;
        ui.UpdateTimer(remainingTime);

        if (remainingTime <= 0f)
            EndQuiz();
    }

    void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("JsonFiles/Quiz/quiz_data");
        questionList = JsonUtility.FromJson<QuestionList>(jsonFile.text);
    }

    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= questionList.questions.Count)
        {
            EndQuiz();
            return;
        }

        ui.ShowQuestion(questionList.questions[currentQuestionIndex]);
    }

    public void SubmitAnswer(string selectedOption)
    {
        Question q = questionList.questions[currentQuestionIndex];

        ui.ShowAnswerResult(
            correctOption: q.answer,
            selectedOption: selectedOption,
            rationale: q.rationale
        );
    }

    public void NextQuestion()
    {
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    void EndQuiz()
    {
        quizFinished = true;
        ui.ShowQuizFinished();
    }
}
