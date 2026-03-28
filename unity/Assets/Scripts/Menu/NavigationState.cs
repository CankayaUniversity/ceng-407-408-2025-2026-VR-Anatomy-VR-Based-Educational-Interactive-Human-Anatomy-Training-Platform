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

    // Quiz kategorisi
    public static QuizCategory CurrentQuizCategory { get; set; } = QuizCategory.AllQuestions;

    // Menü introsunu bir kere atlamak için
    public static bool SkipMenuIntroOnce = false;

    // Sadece runtime state temizliği
    public static void ClearRuntimeOnly()
    {
        // lastSelectedUnitId aynı kalsın
        // ReturnMenuPanelName aynı kalsın

        SelectedMotionSubUnit = MotionSubUnit.None;
        SelectedCirculationSubUnit = CirculationSubUnit.None;

        // İstersen quiz ve mode korunur
        // CurrentQuizCategory = QuizCategory.AllQuestions;
        // CurrentEntryMode = EntryMode.Learn;
    }

    public static void ResetAll()
    {
        lastSelectedUnitId = null;
        ReturnMenuPanelName = null;
        CurrentEntryMode = EntryMode.Learn;

        SelectedMotionSubUnit = MotionSubUnit.None;
        SelectedCirculationSubUnit = CirculationSubUnit.None;

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

    // 2.1 Kalbin Yapısı
    HeartInnerStructure = 1,     // 2.1.1
    HeartOuterStructure = 2,     // 2.1.2

    // 2.2 Damarlar > 2.2.1 Atardamarlar
    UpperExtremityArteries = 3,          // 2.2.1.1
    AbdominalAortaBranches = 4,          // 2.2.1.2
    LowerExtremityArteries = 5,          // 2.2.1.3
    PalpableArteries = 6,                // 2.2.1.4

    // 2.2.2 Toplardamarlar
    UpperExtremityVeins = 7,             // 2.2.2.1
    LowerExtremityVeins = 8,             // 2.2.2.2

    // 2.3 Dolaşım Çeşitleri
    SystemicCirculation = 9,             // 2.3.1 Büyük Dolaşım
    PulmonaryCirculation = 10            // 2.3.2 Küçük Dolaşım
}

public enum QuizCategory
{
    BasicConcepts = 0,
    MotionSystem = 1,
    CirculationSystem = 2,
    AllQuestions = 3
}