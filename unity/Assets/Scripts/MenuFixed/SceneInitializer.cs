using UnityEngine;
using System.Collections.Generic;

public class SceneInitializer : MonoBehaviour
{
    [SerializeField] private List<GameObject> anatomyUnits;

    void Start()
    {
        InitializeScene();
    }

    void InitializeScene()
    {
       
        foreach (GameObject unit in anatomyUnits)
        {
            unit.SetActive(false);
        }

        int id = UnitState.SelectedUnitID;

        if (id >= 0 && id < anatomyUnits.Count)
        {
            anatomyUnits[id].SetActive(true);
            Debug.Log($"Loaded {UnitState.CurrentMode} mode for Unit ID: {id}");
        }
        else
        {
            Debug.LogError("Invalid Unit ID selected!");
        }
    }
}