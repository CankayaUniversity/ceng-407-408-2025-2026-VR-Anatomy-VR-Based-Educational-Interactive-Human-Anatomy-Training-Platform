using System.Collections.Generic;
using UnityEngine;

public class CirculationFreeExploreVisualController : MonoBehaviour
{
    [Header("Display Root")]
    [SerializeField] private Transform displayRoot;

    [Header("Override Materials")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material dimMaterial;

    [Header("Hover Focus Material")]
    [Tooltip("Işın bir damara değdiğinde, aynı alt ünitedeki diğer damarlar bu materyalle hafif şeffaflaşır.")]
    [SerializeField] private Material hoverOtherMaterial;

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new();

    private List<GameObject> _currentInteractionTargets = new();
    private List<GameObject> _currentDimTargets = new();

    private GameObject _currentHoverTarget;

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
            if (r == null)
                continue;

            if (_originalMaterials.ContainsKey(r))
                continue;

            Material[] originals = r.sharedMaterials;
            Material[] copy = new Material[originals.Length];

            for (int j = 0; j < originals.Length; j++)
            {
                copy[j] = originals[j];
            }

            _originalMaterials.Add(r, copy);
        }
    }

    public void ResetVisualState()
    {
        if (_originalMaterials.Count == 0)
            CacheOriginalMaterials();

        _currentHoverTarget = null;
        _currentInteractionTargets.Clear();
        _currentDimTargets.Clear();

        RestoreOriginalMaterials();
    }

    public void ApplyFocus(List<GameObject> interactionTargets, List<GameObject> dimTargets)
    {
        if (_originalMaterials.Count == 0)
            CacheOriginalMaterials();

        _currentHoverTarget = null;

        _currentInteractionTargets = interactionTargets != null
            ? new List<GameObject>(interactionTargets)
            : new List<GameObject>();

        _currentDimTargets = dimTargets != null
            ? new List<GameObject>(dimTargets)
            : new List<GameObject>();

        ApplyCurrentVisualState();
    }

    public void SetHoverTarget(GameObject hoverTarget)
    {
        GameObject resolvedTarget = ResolveIdentityRoot(hoverTarget);

        if (_currentHoverTarget == resolvedTarget)
            return;

        _currentHoverTarget = resolvedTarget;
        ApplyCurrentVisualState();
    }

    public void ClearHoverTarget()
    {
        if (_currentHoverTarget == null)
            return;

        _currentHoverTarget = null;
        ApplyCurrentVisualState();
    }

    private void ApplyCurrentVisualState()
    {
        RestoreOriginalMaterials();

        HashSet<Renderer> interactionRenderers = CollectRenderers(_currentInteractionTargets);
        HashSet<Renderer> dimRenderers = CollectRenderers(_currentDimTargets);
        HashSet<Renderer> hoverRenderers = CollectRenderersFromSingle(_currentHoverTarget);

        // 1) Alakasız / context dışı yapılar şeffaf kalsın.
        foreach (Renderer r in dimRenderers)
        {
            if (r == null)
                continue;

            if (interactionRenderers.Contains(r))
                continue;

            if (dimMaterial != null)
                ApplyOverrideMaterial(r, dimMaterial);
        }

        // 2) Hover yokken seçili alt ünite damarları orijinal kalsın.
        if (_currentHoverTarget == null)
        {
            // İstersen eski highlight davranışını korumak için highlightMaterial kullanabilirsin.
            // Ama senin istediğin "orijinal kalsın" olduğu için burada bilerek hiçbir şey yapmıyoruz.
            return;
        }

        // 3) Hover varken: seçili alt ünitedeki diğer damarlar hafif şeffaflaşsın.
        foreach (Renderer r in interactionRenderers)
        {
            if (r == null)
                continue;

            if (hoverRenderers.Contains(r))
                continue;

            if (hoverOtherMaterial != null)
                ApplyOverrideMaterial(r, hoverOtherMaterial);
        }

        // 4) Hover edilen damar orijinal materyaliyle kalsın.
        foreach (Renderer r in hoverRenderers)
        {
            if (r == null)
                continue;

            RestoreOriginalMaterial(r);
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in _originalMaterials)
        {
            if (pair.Key == null)
                continue;

            pair.Key.sharedMaterials = pair.Value;
        }
    }

    private void RestoreOriginalMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (_originalMaterials.TryGetValue(renderer, out Material[] original))
        {
            renderer.sharedMaterials = original;
        }
    }

    private GameObject ResolveIdentityRoot(GameObject target)
    {
        if (target == null)
            return null;

        VeinIdentity identity = target.GetComponent<VeinIdentity>();

        if (identity == null)
            identity = target.GetComponentInParent<VeinIdentity>();

        if (identity == null)
            identity = target.GetComponentInChildren<VeinIdentity>();

        return identity != null ? identity.gameObject : target;
    }

    private HashSet<Renderer> CollectRenderers(List<GameObject> roots)
    {
        HashSet<Renderer> result = new HashSet<Renderer>();

        if (roots == null)
            return result;

        for (int i = 0; i < roots.Count; i++)
        {
            GameObject go = roots[i];
            if (go == null)
                continue;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);

            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j] != null)
                    result.Add(renderers[j]);
            }
        }

        return result;
    }

    private HashSet<Renderer> CollectRenderersFromSingle(GameObject root)
    {
        HashSet<Renderer> result = new HashSet<Renderer>();

        if (root == null)
            return result;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                result.Add(renderers[i]);
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