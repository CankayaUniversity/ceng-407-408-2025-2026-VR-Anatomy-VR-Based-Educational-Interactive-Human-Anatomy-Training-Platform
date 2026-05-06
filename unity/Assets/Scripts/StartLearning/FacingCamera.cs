using UnityEngine;

public class FacingCamera : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 2f;
    public float rotationDelay = 0.5f;

    private float currentDelayTimer = 0f;
    private Quaternion lastTargetRot;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        HandleDelayedRotation();
    }

    private void HandleDelayedRotation()
    {
        Vector3 dirToCam = mainCam.transform.position - transform.position;

        if (dirToCam != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(-dirToCam, Vector3.up);

            if (Quaternion.Angle(lastTargetRot, targetRot) > 1.0f)
            {
                currentDelayTimer = rotationDelay;
                lastTargetRot = targetRot;
            }

            if (currentDelayTimer > 0)
            {
                currentDelayTimer -= Time.deltaTime;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }

    public void ResetRotationTimer()
    {
        currentDelayTimer = 0;
    }
}