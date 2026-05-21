using UnityEngine;

public class CirculationSystemSubUnitButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Params")]
    [SerializeField] private int subUnitInt;

    [SerializeField] private string returnMenuPanelName;

    [Header("Options")]
    [SerializeField] private bool setReturnPanel = false;

    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader referansı yok! CirculationSystemSubUnitButton üzerinde SceneLoader atamalısın.");
            return;
        }

        if (setReturnPanel && !string.IsNullOrEmpty(returnMenuPanelName))
        {
            NavigationState.ReturnMenuPanelName = returnMenuPanelName;
        }

        CirculationSubUnit selectedSubUnit = (CirculationSubUnit)subUnitInt;
        LessonUIReader.LessonSection lessonSection = ResolveLessonSection(selectedSubUnit);
        Debug.Log(
            $"[CirculationSystemSubUnitButton] Basılan bölüm='{gameObject.name}' | CirculationSubUnit={selectedSubUnit} ({subUnitInt}) | LessonSection={lessonSection}",
            this);

        sceneLoader.LoadCirculationSystemSubUnit(subUnitInt);
    }

    private static LessonUIReader.LessonSection ResolveLessonSection(CirculationSubUnit subUnit)
    {
        switch (subUnit)
        {
            case CirculationSubUnit.HeartInnerStructure:
            case CirculationSubUnit.HeartOuterStructure:
                return LessonUIReader.LessonSection.HeartStructure;
            case CirculationSubUnit.UpperExtremityArteries:
            case CirculationSubUnit.AbdominalAortaBranches:
            case CirculationSubUnit.LowerExtremityArteries:
            case CirculationSubUnit.PalpableArteries:
            case CirculationSubUnit.UpperExtremityVeins:
            case CirculationSubUnit.LowerExtremityVeins:
            case CirculationSubUnit.SystemicCirculation:
            case CirculationSubUnit.PulmonaryCirculation:
                return LessonUIReader.LessonSection.Vessels;
            default:
                return LessonUIReader.LessonSection.Auto;
        }
    }
}