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
        // 1) Hepsini kapat
        foreach (var p in allPanels)
        {
            if (p != null) p.SetActive(false);
        }

        // 2) İstenen paneli aç
        string target = NavigationState.ReturnMenuPanelName;

        if (!string.IsNullOrEmpty(target))
        {
            foreach (var p in allPanels)
            {
                if (p != null && p.name == target)
                {
                    Debug.Log($"[MenuRestore] wanted='{NavigationState.ReturnMenuPanelName}'");
                    p.SetActive(true);
                    return;
                }
            }
        }

        // 3) Bulamazsa main menu
        if (mainMenuPanel != null)
        {
            Debug.LogWarning($"[MenuRestore] Panel not found: '{NavigationState.ReturnMenuPanelName}'. Falling back to MainMenu.");
            mainMenuPanel.SetActive(true);
        }
        
    }
}
