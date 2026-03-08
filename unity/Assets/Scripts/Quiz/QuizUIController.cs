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

    [Header("Question Type Panels")]
    public GameObject multipleChoicePanel;
    public GameObject matchingPanel;

    [Header("Matching UI")]
    public Transform matchingContainer;
    public MatchingRowUI matchingRowPrefab;

    [Header("Manager")]
    public QuizManager quizManager;

    private List<AnswerButtonUI> spawnedButtons = new List<AnswerButtonUI>();
    private List<MatchingRowUI> spawnedMatchingRows = new List<MatchingRowUI>();

    private void Start()
    {
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    public void ShowQuestion(Question q)
    {
        if (!string.IsNullOrWhiteSpace(q.body))
            questionText.text = q.body;
        else
            questionText.text = q.GetQuestionText();

        nextButton.gameObject.SetActive(false);
        rationalePopup.Hide();

        ClearButtons();
        ClearMatchingRows();

        if (q.IsMatching())
        {
            if (multipleChoicePanel != null)
                multipleChoicePanel.SetActive(false);

            if (matchingPanel != null)
                matchingPanel.SetActive(true);

            ShowMatchingQuestion(q);

            Debug.Log("Matching soru gösteriliyor: " + q.GetQuestionText());
        }
        else
        {
            if (matchingPanel != null)
                matchingPanel.SetActive(false);

            if (multipleChoicePanel != null)
                multipleChoicePanel.SetActive(true);

            ShowChoiceQuestion(q);
        }
    }

    void ShowChoiceQuestion(Question q)
    {
        List<string> options = q.GetOptions();

        if (options != null && options.Count > 0)
        {
            for (int i = 0; i < options.Count; i++)
            {
                string optionKey = ((char)('A' + i)).ToString();
                CreateButton(optionKey, options[i]);
            }

            return;
        }

        CreateButton("A", q.A);
        CreateButton("B", q.B);
        CreateButton("C", q.C);
        CreateButton("D", q.D);
    }

    void ShowMatchingQuestion(Question q)
    {
        List<string> leftItems = q.GetMatchingLeft();
        List<string> rightItems = q.GetMatchingRight();

        if (leftItems == null || rightItems == null || leftItems.Count == 0 || rightItems.Count == 0)
        {
            Debug.LogWarning("Matching verisi boş.");
            return;
        }

        if (matchingContainer == null)
        {
            Debug.LogWarning("matchingContainer atanmadı.");
            return;
        }

        if (matchingRowPrefab == null)
        {
            Debug.LogWarning("matchingRowPrefab atanmadı.");
            return;
        }

        List<string> dropdownOptions = new List<string>(rightItems);

        for (int i = 0; i < leftItems.Count; i++)
        {
            MatchingRowUI row = Instantiate(matchingRowPrefab, matchingContainer);
            row.Setup(leftItems[i], dropdownOptions);
            spawnedMatchingRows.Add(row);
        }
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
        ClearMatchingRows();

        if (multipleChoicePanel != null)
            multipleChoicePanel.SetActive(false);

        if (matchingPanel != null)
            matchingPanel.SetActive(false);

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
        ClearMatchingRows();

        if (multipleChoicePanel != null)
            multipleChoicePanel.SetActive(false);

        if (matchingPanel != null)
            matchingPanel.SetActive(false);

        nextButton.gameObject.SetActive(false);
        rationalePopup.Hide();
    }

    void ClearButtons()
    {
        foreach (Transform child in answerContainer)
            Destroy(child.gameObject);

        spawnedButtons.Clear();
    }

    void ClearMatchingRows()
    {
        if (matchingContainer == null)
            return;

        foreach (Transform child in matchingContainer)
            Destroy(child.gameObject);

        spawnedMatchingRows.Clear();
    }
}