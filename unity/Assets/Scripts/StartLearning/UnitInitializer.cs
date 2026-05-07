using UnityEngine;
using System.Collections.Generic;

public class SkeletonInitializer : MonoBehaviour
{
    [System.Serializable]
    public struct UnitMapping
    {
        public int unitID;
        public GameObject unitRoot;
    }

    [Header("Setup")]
    public List<UnitMapping> unitList;

    // UI Panel variables removed because they are now handled by UnitIntroManager and ReviewManager

    void Start()
    {
        // 1. Disable all unit models first to ensure a clean state
        foreach (var mapping in unitList)
        {
            if (mapping.unitRoot != null)
                mapping.unitRoot.SetActive(false);
        }

        // 2. Get the ID selected from the main menu
        int selectedID = AnatomyState.SelectedAnatomyUnitID;

        // 3. Enable only the root object for the selected unit
        foreach (var mapping in unitList)
        {
            if (mapping.unitID == selectedID)
            {
                if (mapping.unitRoot != null)
                {
                    mapping.unitRoot.SetActive(true);
                    Debug.Log($"[SkeletonInitializer] Activated 3D Unit ID: {selectedID}");
                }
                return;
            }
        }

        Debug.LogWarning("No unit found for ID: " + selectedID);
    }
}