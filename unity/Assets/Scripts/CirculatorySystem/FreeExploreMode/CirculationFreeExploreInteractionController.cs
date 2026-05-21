using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CirculationFreeExploreInteractionController : MonoBehaviour
{
    [Header("Interaction Root")]
    [Tooltip("Dolaşım model objelerinin ana root'u. Boş bırakılırsa bu objenin altı taranır.")]
    [SerializeField] private Transform interactionRoot;

    [Header("Optional Collider Control")]
    [Tooltip("Açıksa interaction target olmayan objelerin colliderlarını da kapatır. İlk testte kapalı kalsın.")]
    [SerializeField] private bool disableInactiveColliders = false;

    private readonly List<XRBaseInteractable> allInteractables = new();
    private readonly Dictionary<Collider, bool> originalColliderStates = new();

    private void Awake()
    {
        RefreshCache();
    }

    private void RefreshCache()
    {
        allInteractables.Clear();
        originalColliderStates.Clear();

        Transform root = interactionRoot != null ? interactionRoot : transform;

        XRBaseInteractable[] interactables = root.GetComponentsInChildren<XRBaseInteractable>(true);

        foreach (XRBaseInteractable interactable in interactables)
        {
            if (interactable == null)
                continue;

            allInteractables.Add(interactable);

            Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);

            foreach (Collider col in colliders)
            {
                if (col == null)
                    continue;

                if (!originalColliderStates.ContainsKey(col))
                    originalColliderStates.Add(col, col.enabled);
            }
        }
    }

    public void DisableAllInteractions()
    {
        RefreshCache();

        foreach (XRBaseInteractable interactable in allInteractables)
        {
            if (interactable == null)
                continue;

            interactable.enabled = false;
        }

        if (disableInactiveColliders)
        {
            foreach (KeyValuePair<Collider, bool> pair in originalColliderStates)
            {
                if (pair.Key != null)
                    pair.Key.enabled = false;
            }
        }
    }

    public void EnableOnly(List<GameObject> allowedRoots)
    {
        RefreshCache();

        HashSet<XRBaseInteractable> allowedInteractables = BuildAllowedInteractableSet(allowedRoots);

        foreach (XRBaseInteractable interactable in allInteractables)
        {
            if (interactable == null)
                continue;

            bool isAllowed = allowedInteractables.Contains(interactable);
            interactable.enabled = isAllowed;

            if (disableInactiveColliders)
            {
                Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);

                foreach (Collider col in colliders)
                {
                    if (col == null)
                        continue;

                    col.enabled = isAllowed;
                }
            }
        }

        Debug.Log("[InteractionController] Enabled interactables: " + allowedInteractables.Count);
    }

    private HashSet<XRBaseInteractable> BuildAllowedInteractableSet(List<GameObject> allowedRoots)
    {
        HashSet<XRBaseInteractable> result = new();

        if (allowedRoots == null)
            return result;

        foreach (GameObject root in allowedRoots)
        {
            if (root == null)
                continue;

            XRBaseInteractable[] interactables = root.GetComponentsInChildren<XRBaseInteractable>(true);

            foreach (XRBaseInteractable interactable in interactables)
            {
                if (interactable != null)
                    result.Add(interactable);
            }

            XRBaseInteractable parentInteractable = root.GetComponentInParent<XRBaseInteractable>();

            if (parentInteractable != null && IsUnderAllowedRoot(parentInteractable.gameObject, root))
                result.Add(parentInteractable);
        }

        return result;
    }

    private bool IsUnderAllowedRoot(GameObject interactableObject, GameObject allowedRoot)
    {
        if (interactableObject == null || allowedRoot == null)
            return false;

        if (interactableObject == allowedRoot)
            return true;

        if (interactableObject.transform.IsChildOf(allowedRoot.transform))
            return true;

        if (allowedRoot.transform.IsChildOf(interactableObject.transform))
            return true;

        return false;
    }
}