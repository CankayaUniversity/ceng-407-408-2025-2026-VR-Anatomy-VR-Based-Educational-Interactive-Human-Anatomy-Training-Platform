using UnityEngine;

public class RotateUnitButton_Inspection : MonoBehaviour
{
    [Header("Direction Configuration")]
    public float turningDirection = 1f;

    private RotateUnit_Inspection _activeRotator;

    public void SendOnPointerDown()
    {
        // Dynamically find all rotators in the scene
        RotateUnit_Inspection[] allRotators = FindObjectsByType<RotateUnit_Inspection>(FindObjectsSortMode.None);

        foreach (RotateUnit_Inspection rotator in allRotators)
        {
            // Only capture the one that is currently active and open in the hierarchy
            if (rotator.gameObject.activeInHierarchy)
            {
                _activeRotator = rotator;
                _activeRotator.StartRotating(turningDirection);
                break;
            }
        }

        if (_activeRotator == null)
        {
            Debug.LogWarning("[Inspection Button] Could not find any active RotateUnit_Inspection targets in the scene layout!");
        }
    }

    public void SendOnPointerUp()
    {
        // Tell the captured rotator to stop turning
        if (_activeRotator != null)
        {
            _activeRotator.StopRotating();
            _activeRotator = null;
        }
    }
}