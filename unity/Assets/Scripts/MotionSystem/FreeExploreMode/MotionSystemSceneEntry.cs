using UnityEngine;

public class MotionSystemSceneEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FreeExploreController freeExploreController;

    [Header("Startup")]
    [SerializeField] private bool autoStartOnSceneEnter = true;

    private void Start()
    {
        if (!autoStartOnSceneEnter)
        {
            Debug.Log("[MotionSystemSceneEntry] Auto start disabled for this scene.");
            return;
        }

        TryEnterFromNavigationState();
    }

    public void StartFromNavigationState()
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