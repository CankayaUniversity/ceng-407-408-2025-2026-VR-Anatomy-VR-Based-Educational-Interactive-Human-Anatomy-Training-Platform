using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculationFreeExploreController : MonoBehaviour
{
    [System.Serializable]
    public class SequenceDefinition
    {
        public CirculationSubUnit subUnitValue;

        public List<GameObject> contextObjects = new();
        public List<GameObject> focusTargets = new();
        public List<GameObject> dimTargets = new();
        public List<GameObject> interactionTargets = new();

        public float overviewDurationOverride = 10f;
    }

    [SerializeField] private List<SequenceDefinition> sequenceDefinitions = new();

    [SerializeField] private FreeExploreDisplayVisibilityController visibilityController;
    [SerializeField] private FreeExploreVisualController visualController;
    [SerializeField] private FreeExploreRotationController rotationController;
    [SerializeField] private FreeExploreInteractionController interactionController;

    [SerializeField] private float defaultOverviewDuration = 10f;

    private Coroutine _activeSequence;

    public void StartSelectionBySubUnit(CirculationSubUnit subUnit)
    {
        SequenceDefinition definition = sequenceDefinitions.Find(x => x.subUnitValue == subUnit);

        if (definition == null)
        {
            Debug.LogWarning($"[CirculationFreeExploreController] No sequence found for {subUnit}");
            return;
        }

        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(RunSequence(definition));
    }

    private IEnumerator RunSequence(SequenceDefinition def)
    {
        if (visibilityController != null)
            visibilityController.HideAll();

        if (visualController != null)
            visualController.ResetVisualState();

        if (interactionController != null)
            interactionController.DisableAllInteractions();

        if (visibilityController != null)
            visibilityController.ShowOnly(def.contextObjects);

        if (rotationController != null)
            rotationController.EnableRotation();

        float wait = def.overviewDurationOverride > 0f
            ? def.overviewDurationOverride
            : defaultOverviewDuration;

        yield return new WaitForSeconds(wait);

        if (rotationController != null)
            rotationController.DisableRotation();

        if (visibilityController != null)
            visibilityController.ShowOnly(def.contextObjects);

        if (visualController != null)
            visualController.ApplyFocus(def.focusTargets, def.dimTargets);

        if (interactionController != null)
            interactionController.EnableOnly(def.interactionTargets);
    }
}