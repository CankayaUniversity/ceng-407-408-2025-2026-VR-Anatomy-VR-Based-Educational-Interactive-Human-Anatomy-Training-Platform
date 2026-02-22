public static class NavigationState
{
    public static string ReturnMenuPanelName = "MainMenuPanel";

    // ✅ Yeni: Menüden hangi modla girildi?
    public static EntryMode CurrentEntryMode = EntryMode.None;

    // Motion System sahnesinde hangi alt ünite açılacak?
    public static MotionSubUnit SelectedMotionSubUnit = MotionSubUnit.None;

    public static void ResetToMainMenuPanel()
    {
        ReturnMenuPanelName = "MainMenuPanel";
    }

    public static void ResetMotionSelection()
    {
        SelectedMotionSubUnit = MotionSubUnit.None;
    }

    // ✅ İstersen menüye dönünce her şeyi temizle
    public static void ResetAll()
    {
        ReturnMenuPanelName = "MainMenuPanel";
        CurrentEntryMode = EntryMode.None;
        SelectedMotionSubUnit = MotionSubUnit.None;
    }
}

public enum EntryMode
{
    None = 0,
    Learn = 1,
    FreeExplore = 2
}

public enum MotionSubUnit
{
    None = 0,
    BonesoftheHeadandFace = 1,
    Rib = 2,
    Spine = 3,
    UpperExtremityBones = 4,
    LowerExtremityBones = 5,
    UpperExtremityMuscles = 6,
    LowerExtremityMuscles = 7
}