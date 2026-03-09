using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuizUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text timerText;
    public GameObject timerRoot;
    public Transform answerContainer;
    public Button answerButtonPrefab;
    public Button nextButton;
    public RationalePopupUI rationalePopup;

    [Header("Question Type Panels")]
    public GameObject multipleChoicePanel;
    public GameObject matchingPanel;

    [Header("Matching UI")]
    public Transform leftColumn;
    public Transform rightColumn;
    public MatchingItemUI leftItemPrefab;
    public MatchingItemUI rightItemPrefab;

    [Header("Manager")]
    public QuizManager quizManager;

    private List<AnswerButtonUI> spawnedButtons = new List<AnswerButtonUI>();
    private List<MatchingItemUI> spawnedLeftItems = new List<MatchingItemUI>();
    private List<MatchingItemUI> spawnedRightItems = new List<MatchingItemUI>();

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
        ClearMatchingItems();

        if (q.IsMatching())
        {
            if (timerRoot != null)
                timerRoot.SetActive(false);

            if (multipleChoicePanel != null)
                multipleChoicePanel.SetActive(false);

            if (matchingPanel != null)
                matchingPanel.SetActive(true);

            ShowMatchingQuestion(q);

            Debug.Log("Matching soru gösteriliyor: " + q.GetQuestionText());
        }
        else
        {
            if (timerRoot != null)
                timerRoot.SetActive(true);

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

        if (leftColumn == null || rightColumn == null)
        {
            Debug.LogWarning("LeftColumn veya RightColumn atanmadı.");
            return;
        }

        if (leftItemPrefab == null || rightItemPrefab == null)
        {
            Debug.LogWarning("Matching item prefabları atanmadı.");
            return;
        }

        for (int i = 0; i < leftItems.Count; i++)
        {
            MatchingItemUI leftItem = Instantiate(leftItemPrefab, leftColumn);
            leftItem.Setup(leftItems[i]);
            spawnedLeftItems.Add(leftItem);
        }

        for (int i = 0; i < rightItems.Count; i++)
        {
            MatchingItemUI rightItem = Instantiate(rightItemPrefab, rightColumn);
            rightItem.Setup(rightItems[i]);
            spawnedRightItems.Add(rightItem);
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
        ClearMatchingItems();

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
            if (timerRoot != null)
                timerRoot.SetActive(false);
            return;
        }

        if (timerRoot != null)
            timerRoot.SetActive(true);

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(time).ToString();
    }

    public void ShowQuizFinished()
    {
        questionText.text = "Quiz tamamlandı!";

        ClearButtons();
        ClearMatchingItems();

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

    void ClearMatchingItems()
    {
        if (leftColumn != null)
        {
            foreach (Transform child in leftColumn)
                Destroy(child.gameObject);
        }

        if (rightColumn != null)
        {
            foreach (Transform child in rightColumn)
                Destroy(child.gameObject);
        }

        spawnedLeftItems.Clear();
        spawnedRightItems.Clear();
    }
}