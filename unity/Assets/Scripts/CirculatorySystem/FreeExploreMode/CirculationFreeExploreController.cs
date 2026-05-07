using System.Collections.Generic;
using UnityEngine;
using System.Collections;


public class CirculationFreeExploreController : MonoBehaviour
{
    [System.Serializable]
    public class SequenceDefinition
    {
        public CirculationSubUnit subUnitValue;

        public List<GameObject> contextObjects = new List<GameObject>();
        public List<GameObject> focusTargets = new List<GameObject>();
        public List<GameObject> dimTargets = new List<GameObject>();
        public List<GameObject> interactionTargets = new List<GameObject>();

        public float overviewDurationOverride = 10f;
    }

    [Header("Sequence Definitions")]
    [SerializeField] private List<SequenceDefinition> sequenceDefinitions = new List<SequenceDefinition>();

    [Header("Controllers")]
    [SerializeField] private CirculationFreeExploreDisplayVisibilityController visibilityController;
    [SerializeField] private CirculationFreeExploreVisualController visualController;
    [SerializeField] private FreeExploreRotationController rotationController;
    [SerializeField] private CirculationFreeExploreInteractionController interactionController;

    [Tooltip("Ray ile isim gösterme/hover sistemini yöneten controller.")]
    [SerializeField] private CirculationFreeExploreNameInspectionController nameInspectionController;

    [Header("Overview")]
    [SerializeField] private float defaultOverviewDuration = 10f;

    private Coroutine activeSequence;

    public void StartSelectionBySubUnit(CirculationSubUnit subUnit)
    {
        SequenceDefinition definition = sequenceDefinitions.Find(x => x.subUnitValue == subUnit);

        if (definition == null)
        {
            Debug.LogWarning("[CirculationFreeExploreController] No sequence found for " + subUnit);
            return;
        }

        if (activeSequence != null)
            StopCoroutine(activeSequence);

        activeSequence = StartCoroutine(RunSequence(definition));
    }

    private IEnumerator RunSequence(SequenceDefinition def)
    {
        // 1) Önce eski state'i temizle.
        if (visibilityController != null)
            visibilityController.HideAll();

        if (visualController != null)
            visualController.ResetVisualState();

        if (interactionController != null)
            interactionController.DisableAllInteractions();

        if (nameInspectionController != null)
            nameInspectionController.ClearAllowedInspectionTargets();

        // 2) Overview aşaması: tüm context objeler görünsün.
        // Bu aşamada ray ile isim/hover hedefi yok.
        if (visibilityController != null)
            visibilityController.ShowOnly(def.contextObjects);

        if (rotationController != null)
            rotationController.EnableRotation();

        float wait = def.overviewDurationOverride > 0f
            ? def.overviewDurationOverride
            : defaultOverviewDuration;

        yield return new WaitForSeconds(wait);

        // 3) Overview bitti.
        if (rotationController != null)
            rotationController.DisableRotation();

        List<GameObject> visibleObjects = BuildVisibleSet(def);

        if (visibilityController != null)
            visibilityController.ShowOnly(visibleObjects);

        if (visualController != null)
            visualController.ApplyFocus(def.interactionTargets, def.dimTargets);

        if (interactionController != null)
            interactionController.EnableOnly(def.interactionTargets);

        // 4) En kritik kısım:
        // Ray artık sadece seçilen alt ünitenin interactionTargets listesindeki objeleri algılar.
        // Dim target collider'ları çarpsa bile isim/hover almaz.
        if (nameInspectionController != null)
            nameInspectionController.SetAllowedInspectionTargets(def.interactionTargets);
    }

    private List<GameObject> BuildVisibleSet(SequenceDefinition def)
    {
        List<GameObject> result = new List<GameObject>();

        AddRangeUnique(result, def.contextObjects);
        AddRangeUnique(result, def.focusTargets);
        AddRangeUnique(result, def.dimTargets);

        return result;
    }

    private void AddRangeUnique(List<GameObject> target, List<GameObject> source)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            GameObject go = source[i];

            if (go == null)
                continue;

            if (!target.Contains(go))
                target.Add(go);
        }
    }
}