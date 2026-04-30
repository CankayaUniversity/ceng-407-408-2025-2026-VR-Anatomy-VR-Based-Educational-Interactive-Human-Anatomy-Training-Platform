using UnityEngine;
using System.Collections.Generic;

public class SkeletonInitializer : MonoBehaviour
{
    [System.Serializable]
    public struct UnitMapping
    {
        public int unitID;         // The ID from button 
        public GameObject unitRoot; 
    }

    [Header("Setup")]
    public List<UnitMapping> unitList;

    void Start()
    {
        
        foreach (var mapping in unitList)
        {
            if (mapping.unitRoot != null)
                mapping.unitRoot.SetActive(false);
        }

       

        int selectedID = AnatomyState.SelectedAnatomyUnitID;

       
        foreach (var mapping in unitList)
        {
            if (mapping.unitID == selectedID)
            {
                if (mapping.unitRoot != null)
                    mapping.unitRoot.SetActive(true);
                return; 
            }
        }

        Debug.LogWarning("No unit found for ID: " + selectedID);
   
    }
}