public static class NavigationState
{
    // Menüde en son seçilen ünite/alt menü id
    public static string lastSelectedUnitId;

    // Menüye geri dönünce hangi panel açılacak?
    public static string ReturnMenuPanelName;

    // Hangi moddan girildi? (Learn / FreeExplore)
    public static EntryMode CurrentEntryMode { get; set; } = EntryMode.Learn;

    // Hareket sistemi içinde hangi alt ünite seçildi?
    public static MotionSubUnit SelectedMotionSubUnit { get; set; } = MotionSubUnit.None;

    // Dolaşım sistemi içinde hangi alt ünite seçildi?
    public static CirculationSubUnit SelectedCirculationSubUnit { get; set; } = CirculationSubUnit.None;

    // QUIZ: Menüden hangi quiz kategorisi seçildi?
    public static QuizCategory CurrentQuizCategory { get; set; } = QuizCategory.AllQuestions;

    // Menü intro bir kez atlasın mı?
    public static bool SkipMenuIntroOnce = false;

    // Sadece runtime state temizliği (menü geri dönüş panelini silmez)
    public static void ClearRuntimeOnly()
    {
        SelectedMotionSubUnit = MotionSubUnit.None;
        SelectedCirculationSubUnit = CirculationSubUnit.None;
    }

    public static void ResetAll()
    {
        lastSelectedUnitId = null;
        ReturnMenuPanelName = null;
        CurrentEntryMode = EntryMode.Learn;
        SelectedMotionSubUnit = MotionSubUnit.None;
        CurrentQuizCategory = QuizCategory.AllQuestions;
        SkipMenuIntroOnce = false;
    }
}

public enum EntryMode
{
    Learn = 0,
    FreeExplore = 1
}

public enum MotionSubUnit
{
    None = 0,
    HeadFaceBones = 1,
    Rib = 2,
    Spine = 3,
    UpperExtremityBones = 4,
    LowerExtremityBones = 5,
    UpperExtremityMuscles = 6,
    LowerExtremityMuscles = 7
}

public enum CirculationSubUnit
{
    None = 0,
    HeartInnerStructure = 1,
    HeartOuterStructure = 2,
    UpperExtremityArteries = 3,
    AbdominalAortaBranches = 4,
    LowerExtremityArteries = 5,
    PalpableArteries = 6,
    UpperExtremityVeins = 7,
    LowerExtremityVeins = 8,
    SystemicCirculation = 9,
    PulmonaryCirculation = 10
}

public enum QuizCategory
{
    BasicConcepts = 0,
    MotionSystem = 1,
    CirculationSystem = 2,
    AllQuestions = 3
}