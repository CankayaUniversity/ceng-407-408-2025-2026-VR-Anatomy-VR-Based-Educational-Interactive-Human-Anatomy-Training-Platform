using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Rendering;

public class FreeExploreReturnCompleteController : MonoBehaviour
{
    [Header("Scope")]
    [Tooltip("Kemiklerin/etkileşimli modellerin bulunduğu ana root. Genelde displayRoot ile aynı obje.")]
    [SerializeField] private Transform displayRoot;

    [Header("Return Behavior")]
    [SerializeField] private bool returnOnRelease = true;
    [SerializeField] private float returnDuration = 0.6f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Obje yerine dönerken tekrar tutulamasın.")]
    [SerializeField] private bool disableGrabWhileReturning = true;

    [Tooltip("Yerine döndükten sonra fizik yüzünden düşmesin/oynamasın diye Rigidbody kinematic kalsın.")]
    [SerializeField] private bool keepKinematicAfterReturn = true;

    [Header("Completed Visual")]
    [SerializeField] private bool markCompleted = true;

    [Tooltip("İncelenen kemiğin alacağı renk. Hafif yeşil/turkuaz önerilir.")]
    [SerializeField] private Color completedColor = new Color(0.35f, 1f, 0.55f, 1f);

    private class ObjectState
    {
        public XRGrabInteractable grab;
        public Transform transform;

        public Transform originalParent;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;

        public Rigidbody rb;
        public bool originalIsKinematic;
        public bool originalUseGravity;

        public Renderer[] renderers;
        public Material[][] originalSharedMaterials;

        public bool completed;
        public Coroutine returnRoutine;
    }

    private readonly Dictionary<XRGrabInteractable, ObjectState> _states = new();

    private void Start()
    {
        RebuildCache();
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
    }

    public void RebuildCache()
    {
        UnsubscribeAll();
        _states.Clear();

        if (displayRoot == null)
        {
            Debug.LogWarning("[FreeExploreReturnCompleteController] displayRoot boş. Inspector'dan atamalısın.", this);
            return;
        }

        XRGrabInteractable[] grabs = displayRoot.GetComponentsInChildren<XRGrabInteractable>(true);

        foreach (XRGrabInteractable grab in grabs)
        {
            if (grab == null || _states.ContainsKey(grab))
                continue;

            Transform t = grab.transform;
            Rigidbody rb = grab.GetComponent<Rigidbody>();

            ObjectState state = new ObjectState
            {
                grab = grab,
                transform = t,

                originalParent = t.parent,
                originalLocalPosition = t.localPosition,
                originalLocalRotation = t.localRotation,
                originalLocalScale = t.localScale,

                rb = rb,
                originalIsKinematic = rb != null && rb.isKinematic,
                originalUseGravity = rb != null && rb.useGravity,

                renderers = t.GetComponentsInChildren<Renderer>(true)
            };

            state.originalSharedMaterials = new Material[state.renderers.Length][];

            for (int i = 0; i < state.renderers.Length; i++)
            {
                Renderer renderer = state.renderers[i];

                if (renderer != null)
                    state.originalSharedMaterials[i] = renderer.sharedMaterials;
            }

            _states.Add(grab, state);

            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }

        Debug.Log($"[FreeExploreReturnCompleteController] Cached interactables: {_states.Count}", this);
    }

    public void ResetAllCompletedVisuals()
    {
        foreach (ObjectState state in _states.Values)
        {
            ClearCompletedVisual(state);
            state.completed = false;
        }
    }

    public void ReturnAllToOriginImmediate()
    {
        foreach (ObjectState state in _states.Values)
        {
            if (state == null || state.transform == null)
                continue;

            if (state.returnRoutine != null)
            {
                StopCoroutine(state.returnRoutine);
                state.returnRoutine = null;
            }

            state.transform.SetParent(state.originalParent, true);
            state.transform.localPosition = state.originalLocalPosition;
            state.transform.localRotation = state.originalLocalRotation;
            state.transform.localScale = state.originalLocalScale;

            if (state.rb != null)
            {
                state.rb.linearVelocity = Vector3.zero;
                state.rb.angularVelocity = Vector3.zero;

                if (keepKinematicAfterReturn)
                {
                    state.rb.isKinematic = true;
                    state.rb.useGravity = false;
                }
                else
                {
                    state.rb.isKinematic = state.originalIsKinematic;
                    state.rb.useGravity = state.originalUseGravity;
                }
            }

            if (state.grab != null)
                state.grab.enabled = true;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        XRGrabInteractable grab = args.interactableObject as XRGrabInteractable;

        if (grab == null && args.interactableObject != null)
            grab = args.interactableObject.transform.GetComponentInParent<XRGrabInteractable>();

        if (grab == null)
            return;

        if (!_states.TryGetValue(grab, out ObjectState state))
            return;

        if (!returnOnRelease)
            return;

        if (state.returnRoutine != null)
            StopCoroutine(state.returnRoutine);

        state.returnRoutine = StartCoroutine(ReturnThenMarkCompleted(state));
    }

    private IEnumerator ReturnThenMarkCompleted(ObjectState state)
    {
        if (state == null || state.transform == null)
            yield break;

        Transform t = state.transform;

        if (disableGrabWhileReturning && state.grab != null)
            state.grab.enabled = false;

        if (state.rb != null)
        {
            state.rb.linearVelocity = Vector3.zero;
            state.rb.angularVelocity = Vector3.zero;
            state.rb.isKinematic = true;
            state.rb.useGravity = false;
        }

        // XR grab parent değiştirdiyse anatomik parent'a geri bağla ama dünya pozisyonunu koru.
        if (t.parent != state.originalParent)
            t.SetParent(state.originalParent, true);

        Vector3 startPos = t.localPosition;
        Quaternion startRot = t.localRotation;
        Vector3 startScale = t.localScale;

        float duration = Mathf.Max(0.01f, returnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float raw = Mathf.Clamp01(elapsed / duration);
            float eased = returnCurve != null ? returnCurve.Evaluate(raw) : raw;

            t.localPosition = Vector3.Lerp(startPos, state.originalLocalPosition, eased);
            t.localRotation = Quaternion.Slerp(startRot, state.originalLocalRotation, eased);
            t.localScale = Vector3.Lerp(startScale, state.originalLocalScale, eased);

            yield return null;
        }

        t.localPosition = state.originalLocalPosition;
        t.localRotation = state.originalLocalRotation;
        t.localScale = state.originalLocalScale;

        if (markCompleted)
        {
            ApplyCompletedVisual(state);
            state.completed = true;
        }

        if (state.rb != null)
        {
            state.rb.linearVelocity = Vector3.zero;
            state.rb.angularVelocity = Vector3.zero;

            if (keepKinematicAfterReturn)
            {
                state.rb.isKinematic = true;
                state.rb.useGravity = false;
            }
            else
            {
                state.rb.isKinematic = state.originalIsKinematic;
                state.rb.useGravity = state.originalUseGravity;
            }
        }

        if (disableGrabWhileReturning && state.grab != null)
            state.grab.enabled = true;

        state.returnRoutine = null;
    }

    private void ApplyCompletedVisual(ObjectState state)
{
    if (state == null || state.renderers == null)
        return;

    Debug.Log($"[FreeExploreReturnCompleteController] Applying completed visual to {state.transform.name}, renderer count={state.renderers.Length}");

    foreach (Renderer renderer in state.renderers)
    {
        if (renderer == null)
            continue;

        Material[] materials = renderer.materials;

        foreach (Material mat in materials)
        {
            if (mat == null)
                continue;

            MakeMaterialTransparent(mat);

            Color finalColor = completedColor;
            finalColor.a = completedColor.a;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", finalColor);

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", finalColor);

            // Parlamasın diye emission kapalı
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}

private void MakeMaterialTransparent(Material mat)
{
    if (mat == null)
        return;

    // Built-in Standard Shader için
    if (mat.HasProperty("_Mode"))
        mat.SetFloat("_Mode", 3f); // 3 = Transparent

    // URP Lit için Surface Type = Transparent
    if (mat.HasProperty("_Surface"))
        mat.SetFloat("_Surface", 1f); // 1 = Transparent

    // URP Lit için Blend Mode = Alpha
    if (mat.HasProperty("_Blend"))
        mat.SetFloat("_Blend", 0f); // 0 = Alpha

    if (mat.HasProperty("_AlphaClip"))
        mat.SetFloat("_AlphaClip", 0f);

    // Genel blend ayarları
    if (mat.HasProperty("_SrcBlend"))
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

    if (mat.HasProperty("_DstBlend"))
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

    if (mat.HasProperty("_ZWrite"))
        mat.SetFloat("_ZWrite", 0f);

    // Bazı URP sürümlerinde ayrı alpha blend property'leri olabiliyor
    if (mat.HasProperty("_AlphaSrcBlend"))
        mat.SetFloat("_AlphaSrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

    if (mat.HasProperty("_AlphaDstBlend"))
        mat.SetFloat("_AlphaDstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

    // Render tag/keyword ayarları
    mat.SetOverrideTag("RenderType", "Transparent");

    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    mat.EnableKeyword("_ALPHABLEND_ON");

    mat.DisableKeyword("_ALPHATEST_ON");
    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
}

    private void ClearCompletedVisual(ObjectState state)
    {
        if (state == null || state.renderers == null || state.originalSharedMaterials == null)
            return;

        for (int i = 0; i < state.renderers.Length; i++)
        {
            Renderer renderer = state.renderers[i];

            if (renderer == null)
                continue;

            if (i < state.originalSharedMaterials.Length && state.originalSharedMaterials[i] != null)
            {
                renderer.sharedMaterials = state.originalSharedMaterials[i];
            }
        }

        state.completed = false;
    }

    private void UnsubscribeAll()
{
    foreach (KeyValuePair<XRGrabInteractable, ObjectState> pair in _states)
    {
        if (pair.Key != null)
        {
            pair.Key.selectEntered.RemoveListener(OnSelectEntered);
            pair.Key.selectExited.RemoveListener(OnSelectExited);
        }
    }
}

    private void OnSelectEntered(SelectEnterEventArgs args)
{
    XRGrabInteractable grab = args.interactableObject as XRGrabInteractable;

    if (grab == null && args.interactableObject != null)
        grab = args.interactableObject.transform.GetComponentInParent<XRGrabInteractable>();

    if (grab == null)
        return;

    if (!_states.TryGetValue(grab, out ObjectState state))
        return;

    // Daha önce incelenip yeşilleşmişse, tekrar ele alınınca orijinal rengine dönsün.
    ClearCompletedVisual(state);
}
}