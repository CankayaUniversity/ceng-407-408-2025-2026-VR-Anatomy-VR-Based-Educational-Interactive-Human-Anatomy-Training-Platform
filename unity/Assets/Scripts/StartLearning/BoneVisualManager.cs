using UnityEngine;
using System.Collections.Generic;

public class BoneVisualManager : MonoBehaviour
{
    private MaterialPropertyBlock propBlock;
    // This is the property ID for the "Base Color" in URP Lit shaders
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake() => propBlock = new MaterialPropertyBlock();

    public void FocusBone(GameObject target, List<GameObject> allBones)
    {
        foreach (GameObject bone in allBones)
        {
            Renderer r = bone.GetComponent<Renderer>();
            if (r == null) continue;

            // 1. Get the current properties
            r.GetPropertyBlock(propBlock);

            // 2. Determine Alpha: 1.0 (Solid) for target, 0.2 (Ghost) for others
            // Note: We keep RGB as (1, 1, 1) so it doesn't change the bone color
            float alpha = (bone == target) ? 1.0f : 0.2f;
            propBlock.SetColor(BaseColorId, new Color(1, 1, 1, alpha));

            // 3. Apply the changes ONLY to this specific bone's renderer
            r.SetPropertyBlock(propBlock);
        }
    }
}