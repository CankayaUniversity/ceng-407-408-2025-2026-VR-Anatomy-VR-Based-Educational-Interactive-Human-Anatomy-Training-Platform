using System.Collections.Generic;
using UnityEngine;

public class FreeExploreVisualController : MonoBehaviour
{
    [Header("Display Root")]
    [SerializeField] private Transform displayRoot;

    [Header("Override Materials")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material dimMaterial;

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

    private void Awake()
    {
        CacheOriginalMaterials();
    }

    private void CacheOriginalMaterials()
    {
        _originalMaterials.Clear();

        if (displayRoot == null)
            return;

        Renderer[] renderers = displayRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            if (!_originalMaterials.ContainsKey(r))
            {
                _originalMaterials.Add(r, r.sharedMaterials);
            }
        }
    }

    public void ResetVisualState()
    {
        if (_originalMaterials.Count == 0)
            CacheOriginalMaterials();

        foreach (KeyValuePair<Renderer, Material[]> pair in _originalMaterials)
        {
            if (pair.Key == null) continue;
            pair.Key.sharedMaterials = pair.Value;
        }
    }

    public void ApplyFocus(List<GameObject> focusTargets, List<GameObject> dimTargets)
    {
        ResetVisualState();

        HashSet<Renderer> focusRenderers = CollectRenderers(focusTargets);
        HashSet<Renderer> dimRenderers = CollectRenderers(dimTargets);

        // Focus renderers always win over dim renderers
        foreach (Renderer r in focusRenderers)
        {
            if (r == null) continue;
            ApplyOverrideMaterial(r, highlightMaterial);
        }

        foreach (Renderer r in dimRenderers)
        {
            if (r == null) continue;
            if (focusRenderers.Contains(r)) continue;
            ApplyOverrideMaterial(r, dimMaterial);
        }
    }

    private HashSet<Renderer> CollectRenderers(List<GameObject> roots)
    {
        HashSet<Renderer> result = new HashSet<Renderer>();

        if (roots == null)
            return result;

        for (int i = 0; i < roots.Count; i++)
        {
            GameObject go = roots[i];
            if (go == null) continue;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j] != null)
                    result.Add(renderers[j]);
            }
        }

        return result;
    }

    private void ApplyOverrideMaterial(Renderer renderer, Material overrideMaterial)
    {
        if (renderer == null || overrideMaterial == null)
            return;

        Material[] current = renderer.sharedMaterials;
        Material[] overridden = new Material[current.Length];

        for (int i = 0; i < overridden.Length; i++)
        {
            overridden[i] = overrideMaterial;
        }

        renderer.sharedMaterials = overridden;
    }
}