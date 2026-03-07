using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuizUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text timerText;
    public Transform answerContainer;
    public Button answerButtonPrefab;
    public Button nextButton;
    public RationalePopupUI rationalePopup;

    [Header("Manager")]
    public QuizManager quizManager;

    private List<AnswerButtonUI> spawnedButtons = new List<AnswerButtonUI>();

    private void Start()
    {
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    public void ShowQuestion(Question q)
    {
        // Soru metni için önce body, yoksa helper metod
        if (!string.IsNullOrWhiteSpace(q.body))
            questionText.text = q.body;
        else
            questionText.text = q.GetQuestionText();

        nextButton.gameObject.SetActive(false);

        rationalePopup.Hide();
        ClearButtons();

        CreateButton("A", q.A);
        CreateButton("B", q.B);
        CreateButton("C", q.C);
        CreateButton("D", q.D);
    }

    void CreateButton(string key, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Button btn = Instantiate(answerButtonPrefab, answerContainer);
        AnswerButtonUI buttonUI = btn.GetComponent<AnswerButtonUI>();

        if (buttonUI == null)
        {
            Debug.LogError("[QuizUIController] AnswerButtonUI component not found on answerButtonPrefab.");
            return;
        }

        buttonUI.Setup(key, text, this);
        spawnedButtons.Add(buttonUI);
    }

    public void OnAnswerSelected(string optionKey)
    {
        quizManager.SubmitAnswer(optionKey);
    }

    public void ShowAnswerResult(string correctOption, string selectedOption, string rationale)
    {
        bool isCorrect = selectedOption == correctOption;

        foreach (var btn in spawnedButtons)
        {
            btn.Disable();

            if (btn.OptionKey == correctOption)
                btn.SetCorrect();
            else if (btn.OptionKey == selectedOption)
                btn.SetWrong();
        }

        if (!isCorrect && !string.IsNullOrEmpty(rationale))
        {
            rationalePopup.Show(rationale);
        }

        nextButton.gameObject.SetActive(true);
    }

    public void ShowTimeUpResult(string message)
    {
        questionText.text = message;
        rationalePopup.Hide();
        ClearButtons();
        nextButton.gameObject.SetActive(true);
    }

    private void OnNextButtonPressed()
    {
        quizManager.NextQuestion();
    }

    public void UpdateTimer(float time)
    {
        if (time < 0f)
        {
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            return;
        }

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        timerText.text = Mathf.CeilToInt(time).ToString();
    }

    public void ShowQuizFinished()
    {
        questionText.text = "Quiz tamamlandı!";
        ClearButtons();
        nextButton.gameObject.SetActive(false);
        rationalePopup.Hide();
    }

    void ClearButtons()
    {
        foreach (Transform child in answerContainer)
            Destroy(child.gameObject);

        spawnedButtons.Clear();
    }
}