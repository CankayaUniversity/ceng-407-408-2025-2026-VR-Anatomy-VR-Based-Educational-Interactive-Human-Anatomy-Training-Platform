using UnityEngine;

public class MotionSystemSubUnitButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Params")]
    [SerializeField] private int subUnitInt;

    // İstersen ReturnPanel da burada dursun (opsiyonel)
    [SerializeField] private string returnMenuPanelName;

    [Header("Options")]
    [SerializeField] private bool setReturnPanel = false;

    // Button OnClick burayı çağıracak (parametresiz!)
    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader referansı yok! MotionSystemSubUnitButton üzerinde SceneLoader atamalısın.");
            return;
        }

        if (setReturnPanel && !string.IsNullOrEmpty(returnMenuPanelName))
        {
            NavigationState.ReturnMenuPanelName = returnMenuPanelName;
        }

        sceneLoader.LoadMotionSystemSubUnit(subUnitInt);
    }
}