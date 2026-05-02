using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneToLoad; 
    [SerializeField] private string mode;       


    public void LoadUnit(int unitID)
    {
        UnitState.CurrentMode = mode;
        UnitState.SelectedUnitID = unitID;


        SceneManager.LoadScene(sceneToLoad);
        Debug.Log("SceneLoader IS UPP");
    }
}