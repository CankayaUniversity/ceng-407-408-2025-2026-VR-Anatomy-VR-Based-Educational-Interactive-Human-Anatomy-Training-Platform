using UnityEngine;

public class CirculationSystemSceneEntry : MonoBehaviour
{
    [SerializeField] private CirculationFreeExploreController controller;
    [SerializeField] private bool autoStart = false;

    private bool _started;

    private void Start()
    {
        if (!autoStart)
            return;

        BeginSequence();
    }

    public void BeginSequence()
    {
        if (_started)
            return;

        if (NavigationState.CurrentEntryMode != EntryMode.FreeExplore)
            return;

        _started = true;
        controller.StartSelectionBySubUnit(NavigationState.SelectedCirculationSubUnit);
    }
}