using UnityEngine;

public class MenuRestoreOnStart : MonoBehaviour
{
    [Header("Drag all menu root panels here (MainMenuPanel, OgremeyeBaslaPanel, ... )")]
    [SerializeField] private GameObject[] allPanels;

    [Header("Fallback panel if name not found")]
    [SerializeField] private GameObject mainMenuPanel;

    private void Start()
    {
        RestorePanel();
    }

    private void RestorePanel()
    {
        foreach (var p in allPanels)
        {
            if (p != null)
                p.SetActive(false);
        }

        string target = NavigationState.ReturnMenuPanelName;

        if (!string.IsNullOrEmpty(target))
        {
            foreach (var p in allPanels)
            {
                if (p != null && p.name == target)
                {
                    Debug.Log($"[MenuRestore] wanted='{target}'");
                    p.SetActive(true);
                    return;
                }
            }
        }

        if (mainMenuPanel != null)
        {
            Debug.LogWarning($"[MenuRestore] Panel not found: '{target}'. Falling back to MainMenu.");
            mainMenuPanel.SetActive(true);
        }
    }
}