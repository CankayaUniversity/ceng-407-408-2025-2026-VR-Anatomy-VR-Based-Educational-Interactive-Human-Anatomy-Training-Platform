using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
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
    public Button hintButton;
    public HintPopupUI hintPopup;

    [Header("Question Type Panels")]
    public GameObject multipleChoicePanel;
    public GameObject matchingPanel;

    [Header("Matching UI")]
    public Transform leftColumn;
    public Transform rightColumn;
    public MatchingItemUI leftItemPrefab;
    public MatchingItemUI rightItemPrefab;
    public Button confirmButton;

    [Header("Hint Settings")]
    public float hintDelay = 7f;

    [Header("Manager")]
    public QuizManager quizManager;

    private List<AnswerButtonUI> spawnedButtons = new List<AnswerButtonUI>();
    private List<MatchingItemUI> spawnedLeftItems = new List<MatchingItemUI>();
    private List<MatchingItemUI> spawnedRightItems = new List<MatchingItemUI>();

    // Matching state
    private Dictionary<int, int> playerMatches = new Dictionary<int, int>();
    private Dictionary<int, int> correctMatches = new Dictionary<int, int>();
    private bool matchingSubmitted = false;

    // Hint state
    private Coroutine hintCoroutine;
    private Question currentQuestion;
    private bool hintShownForCurrentQuestion = false;

    private void Start()
    {
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonPressed);

        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(false);
            hintButton.onClick.RemoveAllListeners();
            hintButton.onClick.AddListener(OnHintButtonPressed);
        }

        if (hintPopup != null)
        {
            hintPopup.Hide();
        }

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmMatchingPressed);
        }
    }

    public void ShowQuestion(Question q)
    {
        currentQuestion = q;
        ResetHintUI();

        if (!string.IsNullOrWhiteSpace(q.body))
            questionText.text = q.body;
        else
            questionText.text = q.GetQuestionText();

        nextButton.gameObject.SetActive(false);
        rationalePopup.Hide();

        ClearButtons();
        ClearMatchingItems();
        ResetMatchingState();

        if (q.IsMatching())
        {
            if (timerRoot != null)
                timerRoot.SetActive(false);

            if (multipleChoicePanel != null)
                multipleChoicePanel.SetActive(false);

            if (matchingPanel != null)
                matchingPanel.SetActive(true);

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
                confirmButton.interactable = false;
            }

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

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
                confirmButton.interactable = false;
            }

            ShowChoiceQuestion(q);
        }

        TryStartHintTimer(q);
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

        LoadCorrectMatches(q);

        for (int i = 0; i < leftItems.Count; i++)
        {
            MatchingItemUI leftItem = Instantiate(leftItemPrefab, leftColumn);
            leftItem.Setup(leftItems[i], i, true, this);
            spawnedLeftItems.Add(leftItem);
        }

        for (int i = 0; i < rightItems.Count; i++)
        {
            MatchingItemUI rightItem = Instantiate(rightItemPrefab, rightColumn);
            rightItem.Setup(rightItems[i], i, false, this);
            spawnedRightItems.Add(rightItem);
        }

        RefreshMatchingVisuals();
        UpdateConfirmButtonState();
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
        ResetHintUI();
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
        ResetHintUI();

        questionText.text = message;
        rationalePopup.Hide();

        ClearButtons();
        ClearMatchingItems();

        if (multipleChoicePanel != null)
            multipleChoicePanel.SetActive(false);

        if (matchingPanel != null)
            matchingPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

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
        ResetHintUI();

        questionText.text = "Quiz tamamlandı!";

        ClearButtons();
        ClearMatchingItems();

        if (multipleChoicePanel != null)
            multipleChoicePanel.SetActive(false);

        if (matchingPanel != null)
            matchingPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

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

    void ResetMatchingState()
    {
        playerMatches.Clear();
        correctMatches.Clear();
        matchingSubmitted = false;
    }

    void LoadCorrectMatches(Question q)
    {
        correctMatches.Clear();

        List<MatchPair> pairs = q.GetMatchingPairs();

        if (pairs == null || pairs.Count == 0)
        {
            Debug.LogWarning("Matching cevap çiftleri bulunamadı.");
            return;
        }

        for (int i = 0; i < pairs.Count; i++)
        {
            MatchPair pair = pairs[i];
            correctMatches[pair.leftIndex] = pair.rightIndex;
        }
    }

    public void RegisterMatch(int leftIndex, int rightIndex)
    {
        if (matchingSubmitted)
            return;

        List<int> keysToRemove = new List<int>();

        foreach (var pair in playerMatches)
        {
            if (pair.Value == rightIndex && pair.Key != leftIndex)
                keysToRemove.Add(pair.Key);
        }

        foreach (int key in keysToRemove)
            playerMatches.Remove(key);

        playerMatches[leftIndex] = rightIndex;

        Debug.Log($"Eşleşme kaydedildi: Left {leftIndex} -> Right {rightIndex}");
        Debug.Log($"Toplam eşleşme sayısı: {playerMatches.Count}");

        RefreshMatchingVisuals();
        UpdateConfirmButtonState();
    }

    void RefreshMatchingVisuals()
    {
        foreach (var item in spawnedLeftItems)
            item.ResetVisual();

        foreach (var item in spawnedRightItems)
            item.ResetVisual();

        foreach (var pair in playerMatches)
        {
            int leftIndex = pair.Key;
            int rightIndex = pair.Value;

            if (leftIndex >= 0 && leftIndex < spawnedLeftItems.Count)
                spawnedLeftItems[leftIndex].SetMatched(true);

            if (rightIndex >= 0 && rightIndex < spawnedRightItems.Count)
                spawnedRightItems[rightIndex].SetMatched(true);
        }
    }

    void UpdateConfirmButtonState()
    {
        if (confirmButton == null)
            return;

        confirmButton.interactable =
            spawnedLeftItems.Count > 0 &&
            playerMatches.Count == spawnedLeftItems.Count;
    }

    void OnConfirmMatchingPressed()
    {
        ResetHintUI();

        if (matchingSubmitted)
            return;

        if (playerMatches.Count != spawnedLeftItems.Count)
            return;

        matchingSubmitted = true;

        foreach (var pair in playerMatches)
        {
            int leftIndex = pair.Key;
            int rightIndex = pair.Value;

            bool isCorrect = false;

            if (correctMatches.ContainsKey(leftIndex))
                isCorrect = (correctMatches[leftIndex] == rightIndex);

            if (leftIndex >= 0 && leftIndex < spawnedLeftItems.Count)
            {
                if (isCorrect) spawnedLeftItems[leftIndex].SetCorrect();
                else spawnedLeftItems[leftIndex].SetWrong();
            }

            if (rightIndex >= 0 && rightIndex < spawnedRightItems.Count)
            {
                if (isCorrect) spawnedRightItems[rightIndex].SetCorrect();
                else spawnedRightItems[rightIndex].SetWrong();
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = false;

        nextButton.gameObject.SetActive(true);
    }

    void ResetHintUI()
    {
        hintShownForCurrentQuestion = false;

        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        if (hintButton != null)
            hintButton.gameObject.SetActive(false);

        if (hintPopup != null)
            hintPopup.Hide();
    }

    void TryStartHintTimer(Question q)
    {
        if (q == null)
            return;

        if (string.IsNullOrWhiteSpace(q.hint))
            return;

        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(ShowHintButtonAfterDelay());
    }

    IEnumerator ShowHintButtonAfterDelay()
    {
        yield return new WaitForSeconds(hintDelay);

        hintShownForCurrentQuestion = true;

        if (hintButton != null)
            hintButton.gameObject.SetActive(true);

        hintCoroutine = null;
    }

    void OnHintButtonPressed()
    {
        if (currentQuestion == null)
            return;

        if (string.IsNullOrWhiteSpace(currentQuestion.hint))
            return;

        if (hintPopup == null)
            return;

        if (hintPopup.IsOpen())
            hintPopup.Hide();
        else
            hintPopup.Show(currentQuestion.hint);
    }

    public bool IsMatchingSubmitted()
    {
        return matchingSubmitted;
    }
}