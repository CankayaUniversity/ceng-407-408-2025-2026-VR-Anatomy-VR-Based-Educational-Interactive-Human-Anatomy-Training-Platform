using UnityEngine;

public class CirculationSystemSubUnitButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Params")]
    [SerializeField] private int subUnitInt;

    [SerializeField] private string returnMenuPanelName;

    [Header("Options")]
    [SerializeField] private bool setReturnPanel = false;

    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader referansı yok! CirculationSystemSubUnitButton üzerinde SceneLoader atamalısın.");
            return;
        }

        if (setReturnPanel && !string.IsNullOrEmpty(returnMenuPanelName))
        {
            NavigationState.ReturnMenuPanelName = returnMenuPanelName;
        }

        sceneLoader.LoadCirculationSystemSubUnit(subUnitInt);
    }
}