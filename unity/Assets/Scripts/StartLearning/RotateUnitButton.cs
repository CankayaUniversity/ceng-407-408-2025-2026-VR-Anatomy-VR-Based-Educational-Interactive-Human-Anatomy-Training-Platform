using UnityEngine;

public class RotateUnitButton : MonoBehaviour
{
    [Tooltip("Set to 1 for Turning Right, set to -1 for Turning Left")]
    public float turningDirection = 1f;

    public void SendOnPointerDown()
    {
        if (BoneVisualManager.Active != null)
        {
            RotateUnit activeRotator = BoneVisualManager.Active.GetComponent<RotateUnit>();
            if (activeRotator != null)
            {
                activeRotator.StartRotating(turningDirection);
            }
        }
    }

    public void SendOnPointerUp()
    {
        if (BoneVisualManager.Active != null)
        {
            RotateUnit activeRotator = BoneVisualManager.Active.GetComponent<RotateUnit>();
            if (activeRotator != null)
            {
                activeRotator.StopRotating();
            }
        }
    }
}