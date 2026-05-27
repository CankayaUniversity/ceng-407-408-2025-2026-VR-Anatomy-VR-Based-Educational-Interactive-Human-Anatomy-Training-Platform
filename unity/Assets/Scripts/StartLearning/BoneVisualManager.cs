using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BoneVisualManager : MonoBehaviour
{
    [Header("Material Settings")]
    public Material ghostMaterial;

    [Header("Exclusion Configuration")]
    [Tooltip("Renderers with this tag will be entirely ignored by material alterations.")]
    public string ignoreTag = "IgnoreVisuals";

    
    private struct TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private Dictionary<Renderer, Material> _originalMaterials = new Dictionary<Renderer, Material>();
    private Dictionary<Transform, TransformData> _originalTransforms = new Dictionary<Transform, TransformData>();

    public static BoneVisualManager Active;

    void Awake()
    {
        
        CacheOriginalMaterials();
        CacheOriginalTransforms();
    }

    void OnEnable()
    {
        Active = this;
    }

    private void CacheOriginalMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (r.CompareTag(ignoreTag)) continue;

            if (!_originalMaterials.ContainsKey(r))
            {
                _originalMaterials[r] = r.sharedMaterial;
            }
        }
    }

    private void CacheOriginalTransforms()
    {
        
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            // Do not cache the unit root container itself
            if (t == this.transform || t == null) continue;
            if (t.CompareTag(ignoreTag)) continue;

            if (!_originalTransforms.ContainsKey(t))
            {
                _originalTransforms[t] = new TransformData
                {
                    localPosition = t.localPosition,
                    localRotation = t.localRotation
                };
            }
        }
    }

   
    public void SnapBoneToInitialTransform(GameObject boneRoot)
    {
        if (boneRoot == null) return;


        Transform[] targetTransforms = boneRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in targetTransforms)
        {
            if (t == null) continue;

            
            if (_originalTransforms.ContainsKey(t))
            {
                TransformData original = _originalTransforms[t];

                //sometimes it flies away bc of physic engine
                Rigidbody rb = t.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                
                t.localPosition = original.localPosition;
                t.localRotation = original.localRotation;
            }
        }
    }

  
    public void SnapAllBonesToInitialTransforms()
    {
        foreach (var pair in _originalTransforms)
        {
            Transform t = pair.Key;
            TransformData original = pair.Value;

            if (t != null)
            {
                Rigidbody rb = t.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                t.localPosition = original.localPosition;
                t.localRotation = original.localRotation;
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

        
        foreach (GameObject bone in allBones)
        {
            if (bone == null) continue;
            XRGrabInteractable[] allGrabs = bone.GetComponentsInChildren<XRGrabInteractable>(true);
            foreach (var g in allGrabs) g.enabled = false;
        }

        
        XRGrabInteractable[] targetGrabs = targetBone.GetComponentsInChildren<XRGrabInteractable>(true);
        foreach (var g in targetGrabs)
        {
            g.enabled = true;
        }

        
        Renderer[] targetRenderers = targetBone.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in targetRenderers)
        {
            if (_originalMaterials.ContainsKey(r))
            {
                r.sharedMaterial = _originalMaterials[r];
            }
        }
    }

    public void ResetAllBones(List<GameObject> allBones)
    {
        foreach (var item in _originalMaterials)
        {
            Renderer r = item.Key;
            Material originalMat = item.Value;

            if (r != null)
            {
                r.sharedMaterial = originalMat;
            }
        }
    }
}