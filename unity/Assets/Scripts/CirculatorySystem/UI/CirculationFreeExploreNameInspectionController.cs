using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CirculationFreeExploreNameInspectionController : MonoBehaviour
{
    
    [Header("XR Interactors - Grab yapan interactor'lar")]
    [SerializeField] private XRBaseInteractor rightInteractor;
    [SerializeField] private XRBaseInteractor leftInteractor;

    [Header("Ray Origins - Işının çıkacağı noktalar")]
    [SerializeField] private Transform rightRayOrigin;
    [SerializeField] private Transform leftRayOrigin;

    [Header("Ray Settings")]
    [SerializeField] private float maxDistance = 5f;

    [Tooltip("Damarlar ince olduğu için Raycast yerine SphereCast kullanıyoruz. 0.02 - 0.05 arası iyi çalışır.")]
    [SerializeField] private float sphereRadius = 0.035f;

    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleAction;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private VeinNamePresenter namePresenter;

    [Header("Visual Feedback")]
    [SerializeField] private CirculationFreeExploreVisualController visualController;

    private readonly HashSet<GameObject> allowedInspectionTargets = new();

    [SerializeField] private bool restrictInspectionToAllowedTargets = true;

    [Header("Optional Ray Visual")]
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private LineRenderer leftLineRenderer;

    [Header("Intro / Welcome Panel Block")]
    [SerializeField] private GameObject introPanelRoot;
    [SerializeField] private bool hideInspectionWhileIntroIsOpen = true;

    [Header("Start State")]
    [SerializeField] private bool startPanelClosed = true;

    [Header("Behavior")]
    [SerializeField] private float clearDelay = 0.15f;

    [SerializeField] private bool startNamePanelClosed = true;

    private bool namePanelVisible;
    private GameObject currentTarget;
    private float lastValidTargetTime;

    private void OnEnable()
    {
        namePanelVisible = !startNamePanelClosed;

        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnTogglePressed;
        }

        if (panelRoot != null)
            panelRoot.SetActive(namePanelVisible);
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed -= OnTogglePressed;
            toggleAction.action.Disable();
        }
    }

    private void Update()
    {
        if (ShouldBlockInspection())
        {
            SetRayVisualsActive(false);
            ClearInspectionState();
            return;
        }

        SetRayVisualsActive(true);

        // 1) Öncelik: elde tutulan obje
        GameObject heldTarget = GetCurrentSelectedObject();
        GameObject resolvedHeldTarget = ResolveTarget(heldTarget);

        if (resolvedHeldTarget != null && IsAllowedInspectionTarget(resolvedHeldTarget))
        {
            ShowTargetName(resolvedHeldTarget);
            return;
        }

        // 2) Grab yoksa: ışının değdiği obje
        if (TryGetBestRayTarget(out GameObject rayTarget))
        {
            ShowTargetName(rayTarget);
            return;
        }

        // 3) Hedef yoksa kısa gecikmeden sonra temizle
        if (currentTarget != null && Time.time - lastValidTargetTime > clearDelay)
        {
            ClearInspectionState();
        }
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        namePanelVisible = !namePanelVisible;

        if (panelRoot != null)
            panelRoot.SetActive(namePanelVisible);

        if (!namePanelVisible && namePresenter != null)
            namePresenter.ShowName(null);
    }

    private void ShowTargetName(GameObject target)
    {
        if (target == null)
            return;

        currentTarget = target;
        lastValidTargetTime = Time.time;

        if (namePanelVisible && namePresenter != null)
            namePresenter.ShowName(target);

        if (visualController != null)
            visualController.SetHoverTarget(target);
    }

    private void ClearInspectionState()
    {
        currentTarget = null;

        if (namePresenter != null)
            namePresenter.ShowName(null);

        if (visualController != null)
            visualController.ClearHoverTarget();
    }

    private GameObject GetCurrentSelectedObject()
    {
        GameObject leftObj = GetSelectedObjectFromInteractor(leftInteractor);
        if (leftObj != null)
            return leftObj;

        GameObject rightObj = GetSelectedObjectFromInteractor(rightInteractor);
        if (rightObj != null)
            return rightObj;

        return null;
    }

    private GameObject GetSelectedObjectFromInteractor(XRBaseInteractor interactor)
    {
        if (interactor == null)
            return null;

        var interactables = interactor.interactablesSelected;

        if (interactables == null || interactables.Count == 0)
            return null;

        var first = interactables[0];

        Component component = first as Component;
        return component != null ? component.gameObject : null;
    }

    private bool TryGetBestRayTarget(out GameObject target)
    {
        target = null;

        bool rightHasHit = TryCastFromOrigin(rightRayOrigin, rightLineRenderer, out GameObject rightTarget, out float rightDistance);
        bool leftHasHit = TryCastFromOrigin(leftRayOrigin, leftLineRenderer, out GameObject leftTarget, out float leftDistance);

        if (rightHasHit && leftHasHit)
        {
            target = rightDistance <= leftDistance ? rightTarget : leftTarget;
            return target != null;
        }

        if (rightHasHit)
        {
            target = rightTarget;
            return target != null;
        }

        if (leftHasHit)
        {
            target = leftTarget;
            return target != null;
        }

        return false;
    }

    private bool TryCastFromOrigin(Transform origin, LineRenderer lineRenderer, out GameObject target, out float distance)
    {
        target = null;
        distance = maxDistance;

        if (origin == null)
            return false;

        Vector3 start = origin.position;
        Vector3 direction = origin.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            sphereRadius,
            direction,
            maxDistance,
            targetLayerMask,
            QueryTriggerInteraction.Collide
        );

        float bestDistance = maxDistance;
        GameObject bestTarget = null;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            GameObject candidate = ResolveTarget(hit.collider.gameObject);

            if (candidate == null)
                continue;

            if (!IsAllowedInspectionTarget(candidate))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
        {
            target = bestTarget;
            distance = bestDistance;
        }

        UpdateLineRenderer(lineRenderer, start, start + direction * distance);

        return target != null;
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

    private void UpdateLineRenderer(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        // Width grafiğiyle uğraşmamak için kalınlığı koddan sabitliyoruz.
        lineRenderer.widthMultiplier = 0.005f;
        lineRenderer.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private bool ShouldBlockInspection()
    {
        return hideInspectionWhileIntroIsOpen &&
            introPanelRoot != null &&
            introPanelRoot.activeInHierarchy;
    }

    private void SetRayVisualsActive(bool active)
    {
        if (rightLineRenderer != null)
            rightLineRenderer.enabled = active;

        if (leftLineRenderer != null)
            leftLineRenderer.enabled = active;
    }

    public void SetAllowedInspectionTargets(List<GameObject> targets)
    {
        allowedInspectionTargets.Clear();

        if (targets == null)
            return;

        foreach (GameObject root in targets)
        {
            if (root == null)
                continue;

            // Root objeyi ekle
            allowedInspectionTargets.Add(root);

            // Root altında VeinIdentity varsa onları da ekle
            VeinIdentity[] identities = root.GetComponentsInChildren<VeinIdentity>(true);

            foreach (VeinIdentity identity in identities)
            {
                if (identity != null)
                    allowedInspectionTargets.Add(identity.gameObject);
            }
        }

        Debug.Log("[RayName] Allowed inspection target count: " + allowedInspectionTargets.Count);
    }

    public void ClearAllowedInspectionTargets()
    {
        allowedInspectionTargets.Clear();
        ClearInspectionState();
    }

    private bool IsAllowedInspectionTarget(GameObject target)
    {
        if (!restrictInspectionToAllowedTargets)
            return true;

        if (target == null)
            return false;

        if (allowedInspectionTargets.Count == 0)
            return false;

        if (allowedInspectionTargets.Contains(target))
            return true;

        foreach (GameObject allowed in allowedInspectionTargets)
        {
            if (allowed == null)
                continue;

            if (target.transform.IsChildOf(allowed.transform))
                return true;

            if (allowed.transform.IsChildOf(target.transform))
                return true;
        }

        return false;
    }
}