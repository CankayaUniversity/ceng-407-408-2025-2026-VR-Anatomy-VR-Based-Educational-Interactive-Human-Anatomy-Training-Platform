using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        // 1. Disable ALL grab components in the entire unit first
        foreach (GameObject bone in allBones)
        {
            if (bone == null) continue;
            // Get all grabables in children as well to ensure total lockdown
            XRGrabInteractable[] allGrabs = bone.GetComponentsInChildren<XRGrabInteractable>(true);
            foreach (var g in allGrabs) g.enabled = false;
        }

        // 2. Enable ONLY the grab components belonging to the targetBone hierarchy
        XRGrabInteractable[] targetGrabs = targetBone.GetComponentsInChildren<XRGrabInteractable>(true);
        foreach (var g in targetGrabs)
        {
            g.enabled = true;
        }

        // 3. Handle Visuals
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