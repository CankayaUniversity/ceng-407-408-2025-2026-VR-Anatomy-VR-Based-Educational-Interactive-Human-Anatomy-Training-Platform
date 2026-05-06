public static class AnatomyState
{
    public static int SelectedAnatomyUnitID = -1;
    public static LessonUIReader.LessonSection SelectedLessonSection = LessonUIReader.LessonSection.Auto;

    public static void ClearLessonSelection()
    {
        SelectedAnatomyUnitID = -1;
        SelectedLessonSection = LessonUIReader.LessonSection.Auto;
    }
}
