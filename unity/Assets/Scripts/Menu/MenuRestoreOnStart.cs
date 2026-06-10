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

        // Eğer geri dönülecek özel bir panel adı varsa onu aç
        if (!string.IsNullOrEmpty(target))
        {
            foreach (var p in allPanels)
            {
                if (p != null && p.name == target)
                {
                    Debug.Log($"[MenuRestore] Restored panel: '{target}'");

                    p.SetActive(true);
                    NavigationState.ReturnMenuPanelName = "";
                    return;
                }
            }

            // Target boş değil ama listede bulunamadıysa bu gerçekten warning olabilir
            Debug.LogWarning($"[MenuRestore] Panel not found: '{target}'. Falling back to MainMenu.");
        }
        else
        {
            // Target zaten boşsa bu normal başlangıçtır, warning basmaya gerek yok
            Debug.Log("[MenuRestore] No return panel set. Opening MainMenu.");
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[MenuRestore] MainMenuPanel is not assigned.");
        }

        NavigationState.ReturnMenuPanelName = "";
    }
}