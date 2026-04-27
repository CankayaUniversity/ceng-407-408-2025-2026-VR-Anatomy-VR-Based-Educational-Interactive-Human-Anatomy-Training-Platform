using System.Collections.Generic;
using UnityEngine;

public class CirculationFreeExploreVisualController : MonoBehaviour
{
    [Header("Display Root")]
    [SerializeField] private Transform displayRoot;

    [Header("Override Materials")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material dimMaterial;

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new();

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
                Material[] originals = r.sharedMaterials;
                Material[] copy = new Material[originals.Length];

                for (int j = 0; j < originals.Length; j++)
                {
                    copy[j] = originals[j];
                }

                _originalMaterials.Add(r, copy);
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

    public void ApplyFocus(List<GameObject> interactionTargets, List<GameObject> dimTargets)
    {
        ResetVisualState();

        HashSet<Renderer> interactionRenderers = CollectRenderers(interactionTargets);
        HashSet<Renderer> dimRenderers = CollectRenderers(dimTargets);

        // Interaction olanlar highlight
        foreach (Renderer r in interactionRenderers)
        {
            if (r == null) continue;

            if (highlightMaterial != null)
                ApplyOverrideMaterial(r, highlightMaterial);
        }

        // Dim olanlar dim material
        foreach (Renderer r in dimRenderers)
        {
            if (r == null) continue;

            // Interaction her zaman kazanır
            if (interactionRenderers.Contains(r))
                continue;

            if (dimMaterial != null)
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