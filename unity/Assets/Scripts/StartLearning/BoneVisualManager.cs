using UnityEngine;
using System.Collections.Generic;

public class BoneVisualManager : MonoBehaviour
{
    [Header("Material Settings")]
    public Material ghostMaterial;

    private Dictionary<Renderer, Material> _originalMaterials = new Dictionary<Renderer, Material>();

    void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null && !_originalMaterials.ContainsKey(r))
            {
                _originalMaterials[r] = r.sharedMaterial;
            }
        }
    }

    void OnEnable()
    {
        // Whenever this unit is turned ON, force it to Ghost mode
        // so we don't have lingering "Solid" materials from before.
        SetAllToGhost();
    }

    private void SetAllToGhost()
    {
        foreach (var r in _originalMaterials.Keys)
        {
            if (r != null) r.sharedMaterial = ghostMaterial;
        }
    }

    public void FocusBone(GameObject targetBone, List<GameObject> allBones)
    {
        SetAllToGhost();

        Renderer[] targetRenderers = targetBone.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in targetRenderers)
        {
            if (_originalMaterials.ContainsKey(r))
            {
                r.sharedMaterial = _originalMaterials[r];
            }
        }
    }
}