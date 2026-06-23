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
        Debug.Log("[SceneLoader] Mode set edildi: Learn");
    }

    //public void LoadMotionSystemLearnUnit(string sceneName, int unitInt)
    //{
    //    LoadMotionSystemLearnUnit(sceneName, unitInt, LessonUIReader.LessonSection.Auto, "Unknown");
    //}

    //public void LoadMotionSystemLearnUnit(
    //    string sceneName,
    //    int unitInt,
    //    LessonUIReader.LessonSection lessonSection,
    //    string pressedSectionName)
    //{
    //    AnatomyState.SelectedAnatomyUnitID = unitInt;
    //    AnatomyState.SelectedLessonSection = lessonSection;
    //    NavigationState.CurrentEntryMode = EntryMode.Learn;
    //    NavigationState.SelectedMotionSubUnit = MotionSubUnit.None;
    //    NavigationState.SelectedCirculationSubUnit = CirculationSubUnit.None;

    //    Debug.Log(
    //        $"[SceneLoader] Learning unit yükleniyor | Basılan bölüm='{pressedSectionName}' | AnatomyUnitID={unitInt} | " +
    //        $"MotionSubUnit={NavigationState.SelectedMotionSubUnit} | CirculationSubUnit={NavigationState.SelectedCirculationSubUnit} | LessonSection={lessonSection}");

    //    SceneManager.LoadScene(sceneName);
    //}//

    public void SetModeFreeExplore()
    {
        NavigationState.CurrentEntryMode = EntryMode.FreeExplore;
        Debug.Log("[SceneLoader] Mode set edildi: FreeExplore");
    }

    public void LoadMotionSystemSubUnit(int subUnitInt)
    {
        MotionSubUnit selectedSubUnit = (MotionSubUnit)subUnitInt;
        NavigationState.SelectedMotionSubUnit = selectedSubUnit;
        NavigationState.SelectedCirculationSubUnit = CirculationSubUnit.None;
        AnatomyState.SelectedAnatomyUnitID = -1;
       // AnatomyState.SelectedLessonSection = ResolveLessonSection(selectedSubUnit);

        Debug.Log(
            $"[SceneLoader] Motion subunit yükleniyor | MotionSubUnit={selectedSubUnit} ({subUnitInt}) | " +
            $"CirculationSubUnit={NavigationState.SelectedCirculationSubUnit} | AnatomyUnitID={AnatomyState.SelectedAnatomyUnitID} | " 
            /*+$"LessonSection={AnatomyState.SelectedLessonSection}"*/);

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
        CirculationSubUnit selectedSubUnit = (CirculationSubUnit)subUnitInt;
        NavigationState.SelectedCirculationSubUnit = selectedSubUnit;
        NavigationState.SelectedMotionSubUnit = MotionSubUnit.None;
        AnatomyState.SelectedAnatomyUnitID = -1;
        //AnatomyState.SelectedLessonSection = ResolveLessonSection(selectedSubUnit);

        Debug.Log(
            $"[SceneLoader] Circulation subunit yükleniyor | CirculationSubUnit={selectedSubUnit} ({subUnitInt}) | " +
            $"MotionSubUnit={NavigationState.SelectedMotionSubUnit} | AnatomyUnitID={AnatomyState.SelectedAnatomyUnitID} | " /*+
            $"LessonSection={AnatomyState.SelectedLessonSection}"*/);

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

    //private static LessonUIReader.LessonSection ResolveLessonSection(MotionSubUnit subUnit)
    //{
    //    switch (subUnit)
    //    {
    //        case MotionSubUnit.HeadFaceBones:
    //            return LessonUIReader.LessonSection.HeadAndFaceBones;
    //        case MotionSubUnit.Rib:
    //        case MotionSubUnit.Spine:
    //            return LessonUIReader.LessonSection.TrunkBones;
    //        case MotionSubUnit.UpperExtremityBones:
    //            return LessonUIReader.LessonSection.UpperExtremityBones;
    //        case MotionSubUnit.LowerExtremityBones:
    //            return LessonUIReader.LessonSection.LowerExtremityBones;
    //        case MotionSubUnit.UpperExtremityMuscles:
    //        case MotionSubUnit.LowerExtremityMuscles:
    //            return LessonUIReader.LessonSection.SkeletalMuscles;
    //        default:
    //            return LessonUIReader.LessonSection.Auto;
    //    }
    //}

    //private static LessonUIReader.LessonSection ResolveLessonSection(CirculationSubUnit subUnit)
    //{
    //    switch (subUnit)
    //    {
    //        case CirculationSubUnit.HeartInnerStructure:
    //        case CirculationSubUnit.HeartOuterStructure:
    //            return LessonUIReader.LessonSection.HeartStructure;
    //        case CirculationSubUnit.UpperExtremityArteries:
    //        case CirculationSubUnit.AbdominalAortaBranches:
    //        case CirculationSubUnit.LowerExtremityArteries:
    //        case CirculationSubUnit.PalpableArteries:
    //        case CirculationSubUnit.UpperExtremityVeins:
    //        case CirculationSubUnit.LowerExtremityVeins:
    //        case CirculationSubUnit.SystemicCirculation:
    //        case CirculationSubUnit.PulmonaryCirculation:
    //            return LessonUIReader.LessonSection.Vessels;
    //        default:
    //            return LessonUIReader.LessonSection.Auto;
    //    }
    //}
}