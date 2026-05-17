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

        foreach (var mapping in unitList)
        {
            if (mapping.unitRoot != null)
                mapping.unitRoot.SetActive(false);

            if (mapping.lessonManager != null)
            {
                mapping.lessonManager.enabled = false;
                
            }
            else
            {
                Debug.LogError("[INITIALIZER] WARNING: LessonManager slot is NULL for ID: " + mapping.unitID);
            }
        }

        int selectedID = AnatomyState.SelectedAnatomyUnitID;
        

        foreach (var mapping in unitList)
        {
            if (mapping.unitID == selectedID)
            {
                if (mapping.unitRoot != null)
                {
                    mapping.unitRoot.SetActive(true);
                    
                }
                return;
            }
        }
        Debug.LogError("[INITIALIZER] CRITICAL: Could not find a match for ID " + selectedID + " in the list!");
    }
}