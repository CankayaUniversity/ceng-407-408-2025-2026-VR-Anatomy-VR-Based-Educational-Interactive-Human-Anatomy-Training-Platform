using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineToTarget : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetBone;
    public Transform leftUIAnchor;

    [Header("Positioning")]
    public Vector3 manualOffset = new Vector3(0.25f, 0.25f, 0f);
    public float smoothSpeed = 5f;
    public float verticalStartOffset = 0.3f;

    [Header("Line Settings")]
    public int curveResolution = 10;
    public float lineFlexibility = 5f;
    public float sagAmount = 0.05f;

    private LineRenderer line;
    private Vector3 elasticMidPoint;

    // SUBSCRIPTION: Connect the phone line when the object is active
    void OnEnable()
    {
        LessonManager.OnBoneChanged += SetNewTarget;
    }

    // SUBSCRIPTION: Disconnect the phone line to prevent memory leaks/errors
    void OnDisable()
    {
        LessonManager.OnBoneChanged -= SetNewTarget;
    }

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = curveResolution;

        if (targetBone != null) SnapToTarget();
    }

    void LateUpdate()
    {
        // Only run the line rendering if we have a target
        if (targetBone == null) return;

        Vector3 desiredPos = targetBone.position + manualOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);

        UpdateCurve();
    }

    // This method is now the "listener" that gets called by the event
    public void SetNewTarget(Transform newBone)
    {
        targetBone = newBone;
        Vector3 startPos = targetBone.position + manualOffset + (Vector3.down * verticalStartOffset);
        transform.position = startPos;
        elasticMidPoint = startPos;
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
    }
}