using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ModularMenuButton : MonoBehaviour
{
    [Header("Navigation Target Setup")]
    [Tooltip("The unique numerical identifier matching your scene's UnitInitializer mapping list.")]
    [SerializeField] private int unitID;

    [Tooltip("Type the exact name of your main VR Anatomy learning scene here.")]
    [SerializeField] private string targetSceneName = "StartLearningDemo";

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError($"[MENU_BUTTON] Missing standard UI Button component on object: {gameObject.name}");
        }
    }

    private void OnButtonClicked()
    {
        // 1. Write the target mapping ID directly onto your static mailbox variable
        AnatomyState.SelectedAnatomyUnitID = unitID;
        Debug.Log($"[MENU_BUTTON] Saved selection state. Assigned Unit ID #{unitID} to AnatomyState.");

        // 2. Clear out any hanging audio before switching scenes
        if (TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
        }

        // 3. Load your decoupled scene layout safely
        Debug.Log($"[MENU_BUTTON] Initializing async scene swap framework to: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
}