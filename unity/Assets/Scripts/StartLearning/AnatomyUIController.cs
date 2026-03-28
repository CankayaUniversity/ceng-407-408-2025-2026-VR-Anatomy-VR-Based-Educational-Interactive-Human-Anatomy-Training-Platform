using UnityEngine;

public class AnatomyUIController : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetBone;
    public Transform leftUIAnchor;

    [Header("Positioning & Rotation")]
    public Vector3 manualOffset = new Vector3(0.25f, 0.25f, 0f);
    public float smoothSpeed = 5f;
    public float rotationSpeed = 2f;
    public float verticalStartOffset = 0.3f;

    [Header("Latency Settings")]
    [Tooltip("Seconds to wait before the UI starts rotating toward you")]
    public float rotationDelay = 0.5f;
    private float currentDelayTimer = 0f;
    private Quaternion lastTargetRot;

    [Header("Line Settings")]
    public int curveResolution = 10;
    public float lineFlexibility = 5f;
    public float sagAmount = 0.05f;

    private LineRenderer line;
    private Camera mainCam;
    private Vector3 elasticMidPoint;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        mainCam = Camera.main;
        line.positionCount = curveResolution;

        if (targetBone != null) SnapToTarget();
    }

    void LateUpdate()
    {
        if (targetBone == null) return;

        // 1. POSITIONING: Smooth float to target
        Vector3 desiredPos = targetBone.position + manualOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);

        // 2. DELAYED ROTATION
        HandleDelayedRotation();

        // 3. CURVE
        UpdateCurve();
    }

    private void HandleDelayedRotation()
    {
        // Direction from UI to Camera (all axes included now for crouching/looking up)
        Vector3 dirToCam = mainCam.transform.position - transform.position;

        if (dirToCam != Vector3.zero)
        {
            // The ideal rotation to face the user (No head-roll/tilt allowed)
            Quaternion targetRot = Quaternion.LookRotation(-dirToCam, Vector3.up);

            // Check if the camera's ideal rotation has changed significantly
            if (Quaternion.Angle(lastTargetRot, targetRot) > 1.0f)
            {
                // Reset timer if we detect new movement
                currentDelayTimer = rotationDelay;
                lastTargetRot = targetRot;
            }

            // Countdown the latency
            if (currentDelayTimer > 0)
            {
                currentDelayTimer -= Time.deltaTime;
            }
            else
            {
                // Once timer hits zero, start the smooth catch-up
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }

    public void SetNewTarget(Transform newBone)
    {
        targetBone = newBone;

        Vector3 startPos = targetBone.position + manualOffset + (Vector3.down * verticalStartOffset);
        transform.position = startPos;

        elasticMidPoint = startPos;
        currentDelayTimer = 0; // Rotate immediately on new target selection
    }

    private void UpdateCurve()
    {
        Vector3 targetMid = Vector3.Lerp(targetBone.position, leftUIAnchor.position, 0.5f) + Vector3.down * sagAmount;
        elasticMidPoint = Vector3.Lerp(elasticMidPoint, targetMid, Time.deltaTime * lineFlexibility);

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            line.SetPosition(i, GetBezierPoint(t, targetBone.position, elasticMidPoint, leftUIAnchor.position));
        }
    }

    private Vector3 GetBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }

    private void SnapToTarget()
    {
        transform.position = targetBone.position + manualOffset;
        elasticMidPoint = transform.position;

        // Face camera immediately on snap
        Vector3 dir = mainCam.transform.position - transform.position;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
    }
}