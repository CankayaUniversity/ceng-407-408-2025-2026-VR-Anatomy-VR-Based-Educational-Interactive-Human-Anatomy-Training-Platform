public static class NavigationState
{
    // Menu sahnesi açılınca hangi panel aktif edilecek?
    public static string ReturnMenuPanelName = "MainMenuPanel";

    // İstersen her seferinde ana menüye döndürmek için
    public static void ResetToMainMenuPanel()
    {
        ReturnMenuPanelName = "MainMenuPanel";
    }
}
