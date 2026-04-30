using UnityEngine;

public class GenericUnitButton : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string targetSceneName;
    [SerializeField] private int unitID;

    public void Click()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader is missing on the button!");
            return;
        }

        sceneLoader.LoadMotionSystemLearnUnit(targetSceneName, unitID);
    }
}