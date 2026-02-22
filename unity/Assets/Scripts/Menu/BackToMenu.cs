using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BackToMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private string menuSceneName = "01_MenuDeneme";

    [Header("Input (Optional)")]
    [Tooltip("If set, this Input Action triggers back-to-menu (recommended for VR controllers).")]
    [SerializeField] private InputActionReference backToMenuAction;

    [Header("Keyboard fallback (works in simulator/PC)")]
    [SerializeField] private Key backKey = Key.M;

    private void OnEnable()
    {
        if (backToMenuAction != null)
        {
            backToMenuAction.action.performed += OnBackAction;
            backToMenuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (backToMenuAction != null)
        {
            backToMenuAction.action.performed -= OnBackAction;
            backToMenuAction.action.Disable();
        }
    }

    private void Update()
    {
        // Keyboard fallback
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
    NavigationState.ResetAll();
    SceneManager.LoadScene(menuSceneName);
}
}
