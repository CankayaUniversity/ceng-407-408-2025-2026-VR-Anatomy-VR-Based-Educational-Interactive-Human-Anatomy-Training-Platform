using UnityEngine;

public class MotionSystemSceneEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FreeExploreController freeExploreController;

    private void Start()
    {
        TryEnterFromNavigationState();
    }

    private void TryEnterFromNavigationState()
    {
        if (freeExploreController == null)
        {
            Debug.LogWarning("[MotionSystemSceneEntry] FreeExploreController reference is missing.");
            return;
        }

        if (NavigationState.CurrentEntryMode != EntryMode.FreeExplore)
        {
            Debug.Log($"[MotionSystemSceneEntry] EntryMode is {NavigationState.CurrentEntryMode}, FreeExplore akışı başlatılmadı.");
            return;
        }

        int selectedSubUnitValue = (int)NavigationState.SelectedMotionSubUnit;

        Debug.Log($"[MotionSystemSceneEntry] FreeExplore start -> SubUnit={selectedSubUnitValue}, Enum={NavigationState.SelectedMotionSubUnit}");
        freeExploreController.StartSelectionBySubUnit((MotionSubUnit)selectedSubUnitValue);
    }
}