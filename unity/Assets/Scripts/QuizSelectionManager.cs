using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizSelectionManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject categorySelectionPanel;
    [SerializeField] private GameObject introPanel;

    [Header("Intro Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private QuizCategory selectedCategory;

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        WireQuizTopicButtons();

        if (introPanel != null)
            introPanel.SetActive(false);
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
        NavigationState.CurrentQuizCategory = selectedCategory;
        SceneManager.LoadScene("04_Quiz");
    }

    private void OnBackClicked()
    {
        if (introPanel != null)
            introPanel.SetActive(false);

        if (categorySelectionPanel != null)
            categorySelectionPanel.SetActive(true);
    }
}
