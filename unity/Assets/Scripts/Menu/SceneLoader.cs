using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Ana menü
    public void LoadMenu()
    {
        SceneManager.LoadScene("01_MenuDeneme");
    }

    public void SetModeLearn()
{
    NavigationState.CurrentEntryMode = EntryMode.Learn;
}

public void SetModeFreeExplore()
{
    NavigationState.CurrentEntryMode = EntryMode.FreeExplore;
}

    // Hareket Sistemi VR sahnesi
    public void LoadMotionSystemSubUnit(int subUnitInt)
{
    NavigationState.SelectedMotionSubUnit = (MotionSubUnit)subUnitInt;
    SceneManager.LoadScene("02_MotionSystem");
}

    public void LoadMotionSystemFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("02_MotionSystem");
    }

    // Dolaşım Sistemi VR sahnesi
    public void LoadCirculationSystem()
    {
        SceneManager.LoadScene("03_CirculationSystem");
    }

    public void LoadCirculationSystemFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("03_CirculationSystem");
    }

    // Quiz sahnesi
    public void LoadQuiz()
    {
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("04_Quiz");
    }

    // Yapay Zeka sahnesi
    public void LoadAIChat()
    {
        SceneManager.LoadScene("05_AIChat");
    }

    public void LoadAIChatFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("05_AIChat");
    }

    // Hakkında
    public void LoadAbout()
    {
        SceneManager.LoadScene("06_About");
    }
    public void LoadAboutFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("06_About");
    }

    // Ayarlar
    public void LoadSettings()
    {
        SceneManager.LoadScene("07_Settings");
    }

    public void LoadSettingsFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("07_Settings");
    }

    // Uygulamadan çıkmak için (PC build’inde)
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
