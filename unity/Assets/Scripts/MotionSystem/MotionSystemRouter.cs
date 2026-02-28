using UnityEngine;


public class MotionSystemRouter : MonoBehaviour
{
    [Header("Mode Roots")]
    [SerializeField] private GameObject learnModeRoot;
    [SerializeField] private GameObject freeExploreModeRoot;

    [System.Serializable]
    public class SubUnitGroup
    {
        public MotionSubUnit subUnit;
        public GameObject root; // o alt üniteye ait parent
    }

    [Header("Subunit Roots")]
    [SerializeField] private SubUnitGroup[] groups;

    [Header("If no subunit selected, show all?")]
    [SerializeField] private bool showAllWhenNone = true;

    private void Start()
    {
        Debug.Log($"[Router] Mode={NavigationState.CurrentEntryMode} | SubUnit={NavigationState.SelectedMotionSubUnit} ({(int)NavigationState.SelectedMotionSubUnit})");

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
    var selected = NavigationState.SelectedMotionSubUnit;

    Debug.Log($"[Router] ApplySubUnit -> Selected={selected}");

    if (groups == null || groups.Length == 0)
    {
        Debug.LogWarning("[Router] groups EMPTY!");
        return;
    }

    bool found = false;

    if (selected == MotionSubUnit.None)
    {
        foreach (var g in groups)
        {
            if (g.root == null)
            {
                Debug.LogWarning($"[Router] Root NULL for {g.subUnit}");
                continue;
            }
            g.root.SetActive(showAllWhenNone);
        }
        return;
    }

    foreach (var g in groups)
    {
        if (g.root == null)
        {
            Debug.LogWarning($"[Router] Root NULL for {g.subUnit}");
            continue;
        }

        bool active = (g.subUnit == selected);
        if (active) found = true;

        Debug.Log($"[Router]   {g.subUnit} -> {(active ? "ON" : "off")} | root={g.root.name} | parentActive={g.root.transform.parent?.gameObject.activeInHierarchy}");
        g.root.SetActive(active);
    }

    if (!found)
        Debug.LogError($"[Router] Selected subunit ({selected}) NOT FOUND in groups list!");
    }
    private void OnEnable()
    {
    ApplyMode();
    ApplySubUnit();
    }
}