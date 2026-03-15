using System.Collections.Generic;
using UnityEngine;

public class FreeExploreDisplayVisibilityController : MonoBehaviour
{
    [Header("Display Root")]
    [SerializeField] private Transform displayRoot;

    [Header("Top-level visibility roots under display root")]
    [Tooltip("Boş bırakılırsa displayRoot altındaki ilk seviye çocuklar otomatik root kabul edilir.")]
    [SerializeField] private List<GameObject> visibilityRoots = new List<GameObject>();

    private readonly List<GameObject> _resolvedVisibilityRoots = new List<GameObject>();

    private void Awake()
    {
        RebuildVisibilityRoots();
    }

    public void RebuildVisibilityRoots()
    {
        _resolvedVisibilityRoots.Clear();

        if (displayRoot == null)
        {
            Debug.LogWarning("[FreeExploreDisplayVisibilityController] displayRoot is missing.");
            return;
        }

        if (visibilityRoots != null && visibilityRoots.Count > 0)
        {
            for (int i = 0; i < visibilityRoots.Count; i++)
            {
                GameObject go = visibilityRoots[i];
                if (go != null && !_resolvedVisibilityRoots.Contains(go))
                {
                    _resolvedVisibilityRoots.Add(go);
                }
            }
        }
        else
        {
            for (int i = 0; i < displayRoot.childCount; i++)
            {
                Transform child = displayRoot.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    _resolvedVisibilityRoots.Add(child.gameObject);
                }
            }
        }
    }

    public void ShowAll()
    {
        EnsureRootsReady();

        for (int i = 0; i < _resolvedVisibilityRoots.Count; i++)
        {
            if (_resolvedVisibilityRoots[i] != null)
            {
                SetBranchActive(_resolvedVisibilityRoots[i].transform, true);
            }
        }
    }

    public void HideAll()
    {
        EnsureRootsReady();

        for (int i = 0; i < _resolvedVisibilityRoots.Count; i++)
        {
            if (_resolvedVisibilityRoots[i] != null)
            {
                SetBranchActive(_resolvedVisibilityRoots[i].transform, false);
            }
        }
    }

    public void ShowOnly(List<GameObject> contextObjects)
    {
        HideAll();

        if (contextObjects == null || contextObjects.Count == 0)
        {
            Debug.LogWarning("[FreeExploreDisplayVisibilityController] ShowOnly called with empty contextObjects.");
            return;
        }

        for (int i = 0; i < contextObjects.Count; i++)
        {
            GameObject context = contextObjects[i];
            if (context == null)
                continue;

            // 1) Parent zincirini displayRoot'a kadar aç
            ActivateWithAncestorsUntilDisplayRoot(context.transform);

            // 2) Context objesinin TÜM alt ağacını zorla aç
            SetBranchActive(context.transform, true);
        }
    }

    private void EnsureRootsReady()
    {
        if (_resolvedVisibilityRoots.Count == 0)
        {
            RebuildVisibilityRoots();
        }
    }

    private void ActivateWithAncestorsUntilDisplayRoot(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (displayRoot != null && current == displayRoot)
                break;

            current = current.parent;
        }
    }

    private void SetBranchActive(Transform root, bool isActive)
    {
        if (root == null)
            return;

        root.gameObject.SetActive(isActive);

        for (int i = 0; i < root.childCount; i++)
        {
            SetBranchActive(root.GetChild(i), isActive);
        }
    }
}