using UnityEngine;

public class MotionSystemSubUnitButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Params")]
    [SerializeField] private int subUnitInt;

    // İstersen ReturnPanel da burada dursun (opsiyonel)
    [SerializeField] private string returnMenuPanelName;

    [Header("Options")]
    [SerializeField] private bool setReturnPanel = false;

    // Button OnClick burayı çağıracak (parametresiz!)
    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader referansı yok! MotionSystemSubUnitButton üzerinde SceneLoader atamalısın.");
            return;
        }

        if (setReturnPanel && !string.IsNullOrEmpty(returnMenuPanelName))
        {
            NavigationState.ReturnMenuPanelName = returnMenuPanelName;
        }

        MotionSubUnit selectedSubUnit = (MotionSubUnit)subUnitInt;
        LessonUIReader.LessonSection lessonSection = ResolveLessonSection(selectedSubUnit);
        Debug.Log(
            $"[MotionSystemSubUnitButton] Basılan bölüm='{gameObject.name}' | MotionSubUnit={selectedSubUnit} ({subUnitInt}) | LessonSection={lessonSection}",
            this);

        sceneLoader.LoadMotionSystemSubUnit(subUnitInt);
    }

    private static LessonUIReader.LessonSection ResolveLessonSection(MotionSubUnit subUnit)
    {
        switch (subUnit)
        {
            case MotionSubUnit.HeadFaceBones:
                return LessonUIReader.LessonSection.HeadAndFaceBones;
            case MotionSubUnit.Rib:
            case MotionSubUnit.Spine:
                return LessonUIReader.LessonSection.TrunkBones;
            case MotionSubUnit.UpperExtremityBones:
                return LessonUIReader.LessonSection.UpperExtremityBones;
            case MotionSubUnit.LowerExtremityBones:
                return LessonUIReader.LessonSection.LowerExtremityBones;
            case MotionSubUnit.UpperExtremityMuscles:
            case MotionSubUnit.LowerExtremityMuscles:
                return LessonUIReader.LessonSection.SkeletalMuscles;
            default:
                return LessonUIReader.LessonSection.Auto;
        }
    }
}