using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeExploreController : MonoBehaviour
{
    [System.Serializable]
    public class SequenceDefinition
    {
        public MotionSubUnit subUnitValue;
        public string displayName;

        [Header("Overview visibility context")]
        public List<GameObject> contextObjects = new List<GameObject>();

        [Header("Focus visuals")]
        public List<GameObject> focusTargets = new List<GameObject>();
        public List<GameObject> dimTargets = new List<GameObject>();

        [Header("Focus interactions")]
        [Tooltip("Focus başladıktan sonra sadece bu listedeki objelerin altındaki XRGrabInteractable'lar aktif edilir.")]
        public List<GameObject> interactionTargets = new List<GameObject>();

        [Header("Optional")]
        public float overviewDurationOverride = 10f;
    }

    [Header("Definitions")]
    [SerializeField] private List<SequenceDefinition> sequenceDefinitions = new List<SequenceDefinition>();

    [Header("Controllers")]
    [SerializeField] private FreeExploreDisplayVisibilityController visibilityController;
    [SerializeField] private FreeExploreVisualController visualController;
    [SerializeField] private FreeExploreRotationController rotationController;
    [SerializeField] private FreeExploreInteractionController interactionController;

    [Header("Default timings")]
    [SerializeField] private float defaultOverviewDuration = 10f;

    [Header("Startup Safety")]
    [Tooltip("Scene load sonrası geç çalışan scriptler overview görünürlüğünü ezmesin diye 1 frame sonra görünürlük tekrar uygulanır.")]
    [SerializeField] private bool reapplyOverviewAfterStartup = true;

    private Coroutine _activeSequence;

    public void StartSelectionBySubUnit(MotionSubUnit subUnit)
    {
        SequenceDefinition definition = FindDefinition(subUnit);
        if (definition == null)
        {
            Debug.LogWarning($"[FreeExploreController] No sequence definition found for subUnit: {subUnit}");
            return;
        }

        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }

        _activeSequence = StartCoroutine(RunSequence(definition));
    }

    private SequenceDefinition FindDefinition(MotionSubUnit subUnit)
    {
        for (int i = 0; i < sequenceDefinitions.Count; i++)
        {
            if (sequenceDefinitions[i] != null && sequenceDefinitions[i].subUnitValue == subUnit)
                return sequenceDefinitions[i];
        }

        return null;
    }

    private IEnumerator RunSequence(SequenceDefinition definition)
    {
        ResetSequenceState();

        // Scene startup race condition fix
        yield return null;

        ShowOverview(definition);

        if (reapplyOverviewAfterStartup)
        {
            yield return null;
            ShowOverview(definition);
        }

        SetRotationEnabled(true);

        float waitDuration = definition.overviewDurationOverride > 0f
            ? definition.overviewDurationOverride
            : defaultOverviewDuration;

        yield return new WaitForSeconds(waitDuration);

        SetRotationEnabled(false);
        ShowFocus(definition);

        _activeSequence = null;
    }

    private void ResetSequenceState()
    {
        SetRotationEnabled(false);

        if (visibilityController != null)
        {
            visibilityController.RebuildVisibilityRoots();
            visibilityController.ShowAll();
        }

        if (visualController != null)
        {
            visualController.ResetVisualState();
        }

        if (interactionController != null)
        {
            interactionController.CacheInteractables();
            interactionController.DisableAllInteractions();
        }
    }

    private void ShowOverview(SequenceDefinition definition)
    {
        if (visibilityController != null)
        {
            visibilityController.ShowOnly(definition.contextObjects);
        }

        if (visualController != null)
        {
            // Overview sırasında dim / highlight yok
            visualController.ResetVisualState();
        }

        if (interactionController != null)
        {
            // Overview sırasında hiçbir şey tutulamasın
            interactionController.DisableAllInteractions();
        }
    }

    private void ShowFocus(SequenceDefinition definition)
    {
        if (visibilityController != null)
        {
            // Focus'ta da context görünür kalsın
            visibilityController.ShowOnly(definition.contextObjects);
        }

        if (visualController != null)
        {
            visualController.ApplyFocus(definition.focusTargets, definition.dimTargets);
        }

        if (interactionController != null)
        {
            // Sadece doğru objeler tutulabilir olsun
            interactionController.EnableOnly(definition.interactionTargets);
        }
    }

    private void SetRotationEnabled(bool isEnabled)
    {
        if (rotationController == null)
            return;

        if (isEnabled)
            rotationController.EnableRotation();
        else
            rotationController.DisableRotation();
    }
}