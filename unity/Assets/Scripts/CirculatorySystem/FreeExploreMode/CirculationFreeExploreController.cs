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

    [SerializeField] private CirculationFreeExploreDisplayVisibilityController visibilityController;
    [SerializeField] private CirculationFreeExploreVisualController visualController;
    [SerializeField] private FreeExploreRotationController rotationController;
    [SerializeField] private CirculationFreeExploreInteractionController interactionController;

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

    // OVERVIEW: sadece context göster
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

    List<GameObject> visibleObjects = BuildVisibleSet(def);

    if (visibilityController != null)
        visibilityController.ShowOnly(visibleObjects);

    if (visualController != null)
        visualController.ApplyFocus(def.interactionTargets, def.dimTargets);

    if (interactionController != null)
        interactionController.EnableOnly(def.interactionTargets);
}

    private List<GameObject> BuildVisibleSet(SequenceDefinition def)
    {
        List<GameObject> result = new();

        AddRangeUnique(result, def.contextObjects);
        AddRangeUnique(result, def.focusTargets);
        AddRangeUnique(result, def.dimTargets);

        return result;
    }

    private void AddRangeUnique(List<GameObject> target, List<GameObject> source)
    {
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            GameObject go = source[i];
            if (go == null) continue;
            if (!target.Contains(go))
                target.Add(go);
        }
    }
}