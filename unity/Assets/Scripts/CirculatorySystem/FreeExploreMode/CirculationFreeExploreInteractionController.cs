using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CirculationFreeExploreInteractionController : MonoBehaviour
{
    [Header("Display Root")]
    [SerializeField] private Transform displayRoot;

    private readonly List<XRGrabInteractable> _allGrabInteractables = new List<XRGrabInteractable>();

    private void Awake()
    {
        CacheInteractables();
    }

    public void CacheInteractables()
    {
        _allGrabInteractables.Clear();

        if (displayRoot == null)
        {
            Debug.LogWarning("[FreeExploreInteractionController] displayRoot is missing.");
            return;
        }

        XRGrabInteractable[] grabs = displayRoot.GetComponentsInChildren<XRGrabInteractable>(true);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null && !_allGrabInteractables.Contains(grabs[i]))
            {
                _allGrabInteractables.Add(grabs[i]);
            }
        }
    }

    public void DisableAllInteractions()
    {
        if (_allGrabInteractables.Count == 0)
            CacheInteractables();

        for (int i = 0; i < _allGrabInteractables.Count; i++)
        {
            XRGrabInteractable grab = _allGrabInteractables[i];
            if (grab == null) continue;
            grab.enabled = false;
        }
    }

    public void EnableOnly(List<GameObject> interactionTargets)
    {
        DisableAllInteractions();

        if (interactionTargets == null || interactionTargets.Count == 0)
            return;

        HashSet<XRGrabInteractable> allowed = CollectGrabInteractables(interactionTargets);

        foreach (XRGrabInteractable grab in allowed)
        {
            if (grab == null) continue;
            grab.enabled = true;
        }
    }

    private HashSet<XRGrabInteractable> CollectGrabInteractables(List<GameObject> roots)
    {
        HashSet<XRGrabInteractable> result = new HashSet<XRGrabInteractable>();

        for (int i = 0; i < roots.Count; i++)
        {
            GameObject go = roots[i];
            if (go == null) continue;

            XRGrabInteractable[] grabs = go.GetComponentsInChildren<XRGrabInteractable>(true);
            for (int j = 0; j < grabs.Length; j++)
            {
                if (grabs[j] != null)
                    result.Add(grabs[j]);
            }
        }

        return result;
    }
}