using UnityEngine;
using System.Collections.Generic;

public class UnitInitializer : MonoBehaviour
{
    [System.Serializable]
    public struct UnitMapping
    {
        public int unitID;
        public GameObject unitRoot;
        public LessonManager lessonManager;
    }

    [Header("Setup")]
    public List<UnitMapping> unitList;

    void Start()
    {
        Debug.LogError("[INITIALIZER] Start called. Checking List Size: " + unitList.Count);

        foreach (var mapping in unitList)
        {
            if (mapping.unitRoot != null)
                mapping.unitRoot.SetActive(false);

            if (mapping.lessonManager != null)
            {
                mapping.lessonManager.enabled = false;
                Debug.LogError("[INITIALIZER] Force-Disabled LessonManager for ID: " + mapping.unitID);
            }
            else
            {
                Debug.LogError("[INITIALIZER] WARNING: LessonManager slot is NULL for ID: " + mapping.unitID);
            }
        }

        int selectedID = AnatomyState.SelectedAnatomyUnitID;
        Debug.LogError("[INITIALIZER] Looking for Selected ID: " + selectedID);

        foreach (var mapping in unitList)
        {
            if (mapping.unitID == selectedID)
            {
                if (mapping.unitRoot != null)
                {
                    mapping.unitRoot.SetActive(true);
                    Debug.LogError("[INITIALIZER] Found and Activated 3D model for Unit: " + selectedID);
                }
                return;
            }
        }
        Debug.LogError("[INITIALIZER] CRITICAL: Could not find a match for ID " + selectedID + " in the list!");
    }
}