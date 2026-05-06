using UnityEngine;

public class GenericUnitButton : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string targetSceneName;
    [SerializeField] private int unitID;
    [SerializeField] private LessonUIReader.LessonSection lessonSection = LessonUIReader.LessonSection.Auto;

    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader is missing on the button!");
            return;
        }

        LessonUIReader.LessonSection resolvedSection = ResolveLessonSection();
        Debug.Log(
            $"[GenericUnitButton] Basılan bölüm='{gameObject.name}' | AnatomyUnitID={unitID} | LessonSection={resolvedSection}",
            this);

        sceneLoader.LoadMotionSystemLearnUnit(targetSceneName, unitID, resolvedSection, gameObject.name);
    }

    private LessonUIReader.LessonSection ResolveLessonSection()
    {
        if (lessonSection != LessonUIReader.LessonSection.Auto)
            return lessonSection;

        string normalizedName = gameObject.name.Replace(" ", "").ToLowerInvariant();

        if (normalizedName.Contains("head") || normalizedName.Contains("face"))
            return LessonUIReader.LessonSection.HeadAndFaceBones;

        if (normalizedName.Contains("trunk") || normalizedName.Contains("govde") || normalizedName.Contains("gövde"))
            return LessonUIReader.LessonSection.TrunkBones;

        if (normalizedName.Contains("upper") && normalizedName.Contains("bone"))
            return LessonUIReader.LessonSection.UpperExtremityBones;

        if (normalizedName.Contains("lower") && normalizedName.Contains("bone"))
            return LessonUIReader.LessonSection.LowerExtremityBones;

        if (normalizedName.Contains("muscle") || normalizedName.Contains("kas"))
            return LessonUIReader.LessonSection.SkeletalMuscles;

        return LessonUIReader.LessonSection.Auto;
    }
}