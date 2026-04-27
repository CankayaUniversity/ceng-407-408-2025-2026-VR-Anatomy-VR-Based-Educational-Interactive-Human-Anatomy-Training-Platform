using UnityEngine;

public class CirculationSystemRouter : MonoBehaviour
{
    [Header("Mode Roots")]
    [SerializeField] private GameObject learnModeRoot;
    [SerializeField] private GameObject freeExploreModeRoot;

    [System.Serializable]
    public class SubUnitGroup
    {
        public CirculationSubUnit subUnit;
        public GameObject root;
    }

    [Header("Subunit Roots")]
    [SerializeField] private SubUnitGroup[] groups;

    [Header("If no subunit selected, show all?")]
    [SerializeField] private bool showAllWhenNone = true;

    private void Start()
    {
        Debug.Log($"[CirculationRouter] Mode={NavigationState.CurrentEntryMode} | SubUnit={NavigationState.SelectedCirculationSubUnit}");

        ApplyMode();
        ApplySubUnit();
    }

    private void ApplyMode()
    {
        bool isLearn = NavigationState.CurrentEntryMode == EntryMode.Learn;
        bool isFree = NavigationState.CurrentEntryMode == EntryMode.FreeExplore;

        if (learnModeRoot != null) learnModeRoot.SetActive(isLearn);
        if (freeExploreModeRoot != null) freeExploreModeRoot.SetActive(isFree);
    }

    private void ApplySubUnit()
    {
        var selected = NavigationState.SelectedCirculationSubUnit;

        if (groups == null || groups.Length == 0)
        {
            Debug.LogWarning("[CirculationRouter] groups EMPTY!");
            return;
        }

        if (selected == CirculationSubUnit.None)
        {
            foreach (var g in groups)
                if (g.root != null)
                    g.root.SetActive(showAllWhenNone);

            return;
        }

        foreach (var g in groups)
        {
            if (g.root == null) continue;
            g.root.SetActive(g.subUnit == selected);
        }
    }

    private void OnEnable()
    {
        ApplyMode();
        ApplySubUnit();
    }
}