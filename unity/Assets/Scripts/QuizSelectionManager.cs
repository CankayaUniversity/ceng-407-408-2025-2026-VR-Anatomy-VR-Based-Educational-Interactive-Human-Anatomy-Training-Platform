using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class QuizSelectionManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject categorySelectionPanel;
    [SerializeField] private GameObject introPanel;

    [Header("Intro Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private QuizCategory selectedCategory;
    private bool countdownRunning;

    static readonly string IntroInfoText =
        "Bu test 30 sorudan olu\u015Fmaktad\u0131r.\n\n" +
        "\u00C7oktan se\u00E7meli sorular i\u00E7in 30 saniye s\u00FCreniz vard\u0131r.\n\n" +
        "Zorland\u0131\u011F\u0131n\u0131z sorularda \u0130pucu butonunu kullanabilirsiniz.\n\n" +
        "Haz\u0131r oldu\u011Funuzda Ba\u015Fla butonuna basarak teste ba\u015Flayabilirsiniz.";

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        WireQuizTopicButtons();

        if (introPanel != null)
        {
            introPanel.SetActive(false);
            SetupIntroPanel();
        }
    }

    private void SetupIntroPanel()
    {
        var introTextObj = introPanel.transform.Find("IntroText");
        if (introTextObj != null)
        {
            var tr = introTextObj.GetComponent<RectTransform>();
            if (tr != null)
            {
                tr.anchorMin = new Vector2(0.06f, 0.20f);
                tr.anchorMax = new Vector2(0.94f, 0.80f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = Vector2.zero;
            }

            var tmp = introTextObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = IntroInfoText;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.lineSpacing = 5f;
                tmp.paragraphSpacing = 10f;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12;
                tmp.fontSizeMax = 26;
            }
        }

        if (startButton != null)
        {
            var r = startButton.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = new Vector2(0.65f, 0.06f);
                r.anchorMax = new Vector2(0.88f, 0.16f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;
            }
            if (startButton.GetComponent<VRButtonEffect>() == null)
                startButton.gameObject.AddComponent<VRButtonEffect>();
        }

        if (backButton != null)
        {
            var r = backButton.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = new Vector2(0.12f, 0.06f);
                r.anchorMax = new Vector2(0.35f, 0.16f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;
            }
            if (backButton.GetComponent<VRButtonEffect>() == null)
                backButton.gameObject.AddComponent<VRButtonEffect>();
        }
    }

    private void WireQuizTopicButtons()
    {
        if (categorySelectionPanel == null) return;

        foreach (var btn in categorySelectionPanel.GetComponentsInChildren<Button>(true))
        {
            string objName = btn.gameObject.name;

            QuizCategory? category = null;

            if (objName.Contains("Hareket"))
                category = QuizCategory.MotionSystem;
            else if (objName.Contains("Dola"))
                category = QuizCategory.CirculationSystem;
            else if (objName.Contains("Temel"))
                category = QuizCategory.BasicConcepts;
            else if (objName.Contains("Soru") || objName.Contains("T\u00fcm"))
                category = QuizCategory.AllQuestions;

            if (!category.HasValue) continue;

            DisablePersistentListeners(btn);

            QuizCategory cat = category.Value;
            btn.onClick.AddListener(() => OnQuizTopicSelected(cat));
        }
    }

    private void DisablePersistentListeners(Button btn)
    {
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            btn.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
    }

    private void OnQuizTopicSelected(QuizCategory category)
    {
        selectedCategory = category;

        if (categorySelectionPanel != null)
            categorySelectionPanel.SetActive(false);

        if (introPanel != null)
            introPanel.SetActive(true);
    }

    private void OnStartClicked()
    {
        if (countdownRunning) return;
        countdownRunning = true;

        if (startButton != null) startButton.interactable = false;
        if (backButton != null) backButton.interactable = false;

        NavigationState.CurrentQuizCategory = selectedCategory;

        var countdown = gameObject.AddComponent<QuizCountdown>();
        countdown.OnCountdownComplete += () => SceneManager.LoadScene("04_Quiz");
        countdown.StartCountdown(introPanel.transform);
    }

    private void OnBackClicked()
    {
        if (countdownRunning) return;

        if (introPanel != null)
            introPanel.SetActive(false);

        if (categorySelectionPanel != null)
            categorySelectionPanel.SetActive(true);
    }
}
