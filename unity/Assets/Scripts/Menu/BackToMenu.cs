using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BackToMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private string menuSceneName = "01_Menu";

    [Header("Input (Optional)")]
    [SerializeField] private InputActionReference backToMenuAction;

    [Header("Keyboard fallback (simulator/PC)")]
    [SerializeField] private Key backKey = Key.M;

    private void OnEnable()
    {
        if (backToMenuAction?.action != null)
        {
            backToMenuAction.action.performed += OnBackAction;
            backToMenuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (backToMenuAction?.action != null)
        {
            backToMenuAction.action.performed -= OnBackAction;
            backToMenuAction.action.Disable();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[backKey].wasPressedThisFrame)
        {
            GoBackToMenu();
        }
    }

    private void OnBackAction(InputAction.CallbackContext ctx)
    {
        GoBackToMenu();
    }

    public void GoBackToMenu()
    {
        Debug.Log("[BackToMenu] Returning to menu and keeping target panel.");
        NavigationState.SkipMenuIntroOnce = true;
        NavigationState.ClearRuntimeOnly();
        SceneManager.LoadScene(menuSceneName);
    }
}