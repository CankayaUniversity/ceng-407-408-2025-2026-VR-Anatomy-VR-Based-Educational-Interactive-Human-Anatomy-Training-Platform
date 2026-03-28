using UnityEngine;

public class CirculationSystemSceneEntry : MonoBehaviour
{
    [SerializeField] private CirculationFreeExploreController controller;

    private void Start()
    {
        if (NavigationState.CurrentEntryMode != EntryMode.FreeExplore)
            return;

        controller.StartSelectionBySubUnit(NavigationState.SelectedCirculationSubUnit);
    }
}