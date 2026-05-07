using UnityEngine;

public class CirculationRayNameHover : MonoBehaviour
{
    [Header("Ray Origins")]
    [SerializeField] private Transform rightRayOrigin;
    [SerializeField] private Transform leftRayOrigin;

    [Header("Ray Settings")]
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float sphereRadius = 0.025f;
    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Header("UI")]
    [SerializeField] private VeinNamePresenter namePresenter;

    [Header("Optional Ray Visuals")]
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private LineRenderer leftLineRenderer;

    [Header("Behavior")]
    [SerializeField] private string emptyText = "";
    [SerializeField] private float clearDelay = 0.15f;

    private GameObject _currentTarget;
    private float _lastHitTime;

    private void Update()
    {
        RaycastHit bestHit;
        bool hasHit = TryGetBestHit(out bestHit);

        if (hasHit)
        {
            GameObject target = ResolveTarget(bestHit.collider.gameObject);

            if (target != null)
            {
                _currentTarget = target;
                _lastHitTime = Time.time;

                if (namePresenter != null)
                    namePresenter.ShowName(_currentTarget);
            }
        }
        else
        {
            if (_currentTarget != null && Time.time - _lastHitTime > clearDelay)
            {
                _currentTarget = null;

                if (namePresenter != null)
                    namePresenter.ShowName(null);
            }
        }
    }

    private bool TryGetBestHit(out RaycastHit bestHit)
    {
        bool rightHit = CastFromOrigin(rightRayOrigin, rightLineRenderer, out RaycastHit hitRight);
        bool leftHit = CastFromOrigin(leftRayOrigin, leftLineRenderer, out RaycastHit hitLeft);

        if (rightHit && leftHit)
        {
            bestHit = hitRight.distance <= hitLeft.distance ? hitRight : hitLeft;
            return true;
        }

        if (rightHit)
        {
            bestHit = hitRight;
            return true;
        }

        if (leftHit)
        {
            bestHit = hitLeft;
            return true;
        }

        bestHit = default;
        return false;
    }

    private bool CastFromOrigin(Transform origin, LineRenderer lineRenderer, out RaycastHit hit)
    {
        if (origin == null)
        {
            hit = default;
            return false;
        }

        Vector3 start = origin.position;
        Vector3 direction = origin.forward;

        bool hasHit = Physics.SphereCast(
            start,
            sphereRadius,
            direction,
            out hit,
            maxDistance,
            targetLayerMask,
            QueryTriggerInteraction.Collide
        );

        float lineDistance = hasHit ? hit.distance : maxDistance;
        UpdateLine(lineRenderer, start, start + direction * lineDistance);

        return hasHit && ResolveTarget(hit.collider.gameObject) != null;
    }

    private GameObject ResolveTarget(GameObject hitObject)
    {
        if (hitObject == null)
            return null;

        VeinIdentity identity = hitObject.GetComponent<VeinIdentity>();

        if (identity == null)
            identity = hitObject.GetComponentInParent<VeinIdentity>();

        if (identity == null)
            identity = hitObject.GetComponentInChildren<VeinIdentity>();

        return identity != null ? identity.gameObject : null;
    }

    private void UpdateLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}