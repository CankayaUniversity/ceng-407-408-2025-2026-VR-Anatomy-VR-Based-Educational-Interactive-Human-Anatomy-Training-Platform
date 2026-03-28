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

    private static string GetIntroInfoText(QuizCategory category)
    {
        const string bullet = "<indent=0%><color=#00D4FF>></color>  <indent=5%>";

        switch (category)
        {
            case QuizCategory.BasicConcepts:
                return
                    bullet + "Bu test 10 sorudan olu\u015Fmaktad\u0131r.\n" +
                    bullet + "Her soru i\u00E7in 30 saniyeniz vard\u0131r.\n" +
                    bullet + "Haz\u0131r oldu\u011Funuzda Ba\u015Fla butonuna basarak teste ba\u015Flayabilirsiniz.";

            case QuizCategory.AllQuestions:
                return
                    bullet + "Bu test 45 sorudan olu\u015Fmaktad\u0131r.\n" +
                    bullet + "\u00C7oktan se\u00E7meli sorular i\u00E7in 30 saniye s\u00FCreniz vard\u0131r.\n" +
                    bullet + "Zorland\u0131\u011F\u0131n\u0131z sorularda \u0130pucu butonunu kullanabilirsiniz.\n" +
                    bullet + "Haz\u0131r oldu\u011Funuzda Ba\u015Fla butonuna basarak teste ba\u015Flayabilirsiniz.";

            default: // MotionSystem & CirculationSystem
                return
                    bullet + "Bu test 30 sorudan olu\u015Fmaktad\u0131r.\n" +
                    bullet + "\u00C7oktan se\u00E7meli sorular i\u00E7in 30 saniye s\u00FCreniz vard\u0131r.\n" +
                    bullet + "Zorland\u0131\u011F\u0131n\u0131z sorularda \u0130pucu butonunu kullanabilirsiniz.\n" +
                    bullet + "Haz\u0131r oldu\u011Funuzda Ba\u015Fla butonuna basarak teste ba\u015Flayabilirsiniz.";
        }
    }

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
                tr.anchorMin = new Vector2(0.08f, 0.22f);
                tr.anchorMax = new Vector2(0.92f, 0.82f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = Vector2.zero;
            }

            var tmp = introTextObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = GetIntroInfoText(QuizCategory.MotionSystem);
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.lineSpacing = 2f;
                tmp.paragraphSpacing = 0f;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 8;
                tmp.fontSizeMax = 12;
            }
        }

        // --- Başla butonu (sağ alt, küçük) ---
        if (startButton != null)
        {
            var r = startButton.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = new Vector2(0.70f, 0.09f);
                r.anchorMax = new Vector2(0.82f, 0.16f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;
            }

            // Buton yazı boyutunu küçült
            var btnText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.enableAutoSizing = true;
                btnText.fontSizeMin = 8;
                btnText.fontSizeMax = 12;
            }

            if (startButton.GetComponent<VRButtonEffect>() == null)
                startButton.gameObject.AddComponent<VRButtonEffect>();
        }

        // --- Geri butonu (sol alt, küçük) ---
        if (backButton != null)
        {
            var r = backButton.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = new Vector2(0.08f, 0.09f);
                r.anchorMax = new Vector2(0.20f, 0.16f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;
            }

            // Buton yazı boyutunu küçült
            var btnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.enableAutoSizing = true;
                btnText.fontSizeMin = 8;
                btnText.fontSizeMax = 12;
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

        // Intro metnini seçilen kategoriye göre güncelle
        UpdateIntroText(category);

        if (categorySelectionPanel != null)
            categorySelectionPanel.SetActive(false);

        if (introPanel != null)
            introPanel.SetActive(true);

        // Hiçbir butonun otomatik seçili görünmemesi için selection'ı temizle
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
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

    private void UpdateIntroText(QuizCategory category)
    {
        if (introPanel == null) return;

        var introTextObj = introPanel.transform.Find("IntroText");
        if (introTextObj == null) return;

        var tmp = introTextObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = GetIntroInfoText(category);
    }
}
