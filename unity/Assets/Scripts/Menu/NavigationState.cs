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

    // Sadece runtime state temizliği (menü geri dönüş bilgisini silmez)
    public static void ClearRuntimeOnly()
    {
        // lastSelectedUnitId aynı kalsın
        // ReturnMenuPanelName aynı kalsın
        SelectedMotionSubUnit = MotionSubUnit.None;
        // Mode’u da istersen koru, istersen resetle:
        // CurrentEntryMode = EntryMode.Learn;
    }

    public static void ResetAll()
    {
        lastSelectedUnitId = null;
        ReturnMenuPanelName = null;
        CurrentEntryMode = EntryMode.Learn;
        SelectedMotionSubUnit = MotionSubUnit.None;
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