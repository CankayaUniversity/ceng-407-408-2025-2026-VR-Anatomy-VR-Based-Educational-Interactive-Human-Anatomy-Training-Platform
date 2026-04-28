using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMenu()
    {
        SceneManager.LoadScene("01_Menu");
    }

    public void SetModeLearn()
    {
        NavigationState.CurrentEntryMode = EntryMode.Learn;
    }

    public void SetModeFreeExplore()
    {
        NavigationState.CurrentEntryMode = EntryMode.FreeExplore;
    }

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

    public void LoadCirculationSystem()
    {
        SceneManager.LoadScene("03_CirculationSystem");
    }

    public void LoadCirculationSystemSubUnit(int subUnitInt)
{
    NavigationState.SelectedCirculationSubUnit = (CirculationSubUnit)subUnitInt;
    SceneManager.LoadScene("03_CirculationSystem");
}

    public void LoadCirculationSystemFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("03_CirculationSystem");
    }

    // ----------------------------
    // QUIZ - Genel
    // ----------------------------
    public void LoadQuiz()
    {
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("04_Quiz");
    }

    // ----------------------------
    // QUIZ - Kategori seçerek yükleme
    // ----------------------------
    public void LoadBasicConceptsQuiz()
    {
        NavigationState.CurrentQuizCategory = QuizCategory.BasicConcepts;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadMotionSystemQuiz()
    {
        NavigationState.CurrentQuizCategory = QuizCategory.MotionSystem;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadCirculationSystemQuiz()
    {
        NavigationState.CurrentQuizCategory = QuizCategory.CirculationSystem;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadAllQuestionsQuiz()
    {
        NavigationState.CurrentQuizCategory = QuizCategory.AllQuestions;
        SceneManager.LoadScene("04_Quiz");
    }

    // ----------------------------
    // ✅ PANELDEN QUIZ'E GİRİŞ İÇİN YENİLER
    // ----------------------------
    public void LoadBasicConceptsQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        NavigationState.CurrentQuizCategory = QuizCategory.BasicConcepts;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadMotionSystemQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        NavigationState.CurrentQuizCategory = QuizCategory.MotionSystem;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadCirculationSystemQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        NavigationState.CurrentQuizCategory = QuizCategory.CirculationSystem;
        SceneManager.LoadScene("04_Quiz");
    }

    public void LoadAllQuestionsQuizFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        NavigationState.CurrentQuizCategory = QuizCategory.AllQuestions;
        SceneManager.LoadScene("04_Quiz");
    }

    // ----------------------------
    // Diğer sahneler
    // ----------------------------
    public void LoadAIChat()
    {
        SceneManager.LoadScene("05_AIChat");
    }

    public void LoadAIChatFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("05_AIChat");
    }

    public void LoadAbout()
    {
        SceneManager.LoadScene("06_About");
    }

    public void LoadAboutFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("06_About");
    }

    public void LoadSettings()
    {
        SceneManager.LoadScene("07_Settings");
    }

    public void LoadSettingsFromMenuPanel(string currentMenuPanelName)
    {
        NavigationState.ReturnMenuPanelName = currentMenuPanelName;
        SceneManager.LoadScene("07_Settings");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}