public static class NavigationState
{
    // Menüde en son seçilen ünite/alt menü id (istersen kullanırsın)
    public static string lastSelectedUnitId;

    // Menüye geri dönünce hangi panel açılacak?
    public static string ReturnMenuPanelName;

    // Hangi moddan girildi? (Learn / FreeExplore)
    public static EntryMode CurrentEntryMode { get; set; } = EntryMode.Learn;

    // Hareket sistemi içinde hangi alt ünite seçildi?
    public static MotionSubUnit SelectedMotionSubUnit { get; set; } = MotionSubUnit.None;

    // ✅ QUIZ: Menüden hangi quiz kategorisi seçildi?
    public static QuizCategory CurrentQuizCategory { get; set; } = QuizCategory.AllQuestions;
    public static bool SkipMenuIntroOnce = false;

    // Sadece runtime state temizliği (menü geri dönüş bilgisini silmez)
    public static void ClearRuntimeOnly()
    {
        // lastSelectedUnitId aynı kalsın
        // ReturnMenuPanelName aynı kalsın
        SelectedMotionSubUnit = MotionSubUnit.None;

        // Quiz seçimini istersen koru istersen resetle.
        // Ben "korusun" diye dokunmuyorum.
        // CurrentQuizCategory = QuizCategory.AllQuestions;

        // Mode’u da istersen koru, istersen resetle:
        // CurrentEntryMode = EntryMode.Learn;
    }

    public static void ResetAll()
    {
        lastSelectedUnitId = null;
        ReturnMenuPanelName = null;
        CurrentEntryMode = EntryMode.Learn;
        SelectedMotionSubUnit = MotionSubUnit.None;

        // ✅ Quiz state reset
        CurrentQuizCategory = QuizCategory.AllQuestions;
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

    // Bunları senin menü seçeneklerine göre genişletebilirsin.
    // Şimdilik örnekler:
    HeadFaceBones = 1,
    Rib = 2,
    Spine = 3,
    UpperExtremityBones = 4,
    LowerExtremityBones = 5,
    UpperExtremityMuscles = 6,
    LowerExtremityMuscles = 7
}

// ✅ Quiz kategorileri (menüdeki butonlara birebir)
public enum QuizCategory
{
    BasicConcepts = 0,     // Temel Kavramlar
    MotionSystem = 1,      // Hareket Sistemi
    CirculationSystem = 2, // Dolaşım Sistemi
    AllQuestions = 3       // Bütün Sorular
}