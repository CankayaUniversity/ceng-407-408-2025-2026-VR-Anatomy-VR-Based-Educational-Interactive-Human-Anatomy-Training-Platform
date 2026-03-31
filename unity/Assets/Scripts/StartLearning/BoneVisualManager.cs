using UnityEngine;
using System.Collections.Generic;

public class BoneVisualManager : MonoBehaviour
{
    [Header("Material Templates")]
    public Material solidMaterial;       // Drag your 'Mat_Bone_Solid' (Opaque) here
    public Material transparentMaterial; // Drag your 'Mat_Bone_Transparent' here

    public void FocusBone(GameObject target, List<GameObject> allBones)
    {
        foreach (GameObject bone in allBones)
        {
            Renderer r = bone.GetComponent<Renderer>();
            if (r == null) continue;

            // Swap materials: target gets solid, others get transparent
            if (bone == target)
            {
                r.material = solidMaterial;
            }
            else
            {
                r.material = transparentMaterial;
            }
        }
    }
}