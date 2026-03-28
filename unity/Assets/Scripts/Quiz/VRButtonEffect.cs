using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class VRButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    static readonly Color BorderNorm   = new Color(0.35f, 0.80f, 1f, 0.18f);
    static readonly Color BorderHover  = new Color(0.60f, 0.92f, 1f, 0.65f);
    static readonly Color BorderPress  = new Color(0.85f, 0.98f, 1f, 0.90f);
    static readonly Color BorderDisable= new Color(0.45f, 0.50f, 0.56f, 0.12f);

    static readonly Color FillNorm     = new Color(0.03f, 0.07f, 0.12f, 0.70f);
    static readonly Color FillHover    = new Color(0.05f, 0.12f, 0.19f, 0.86f);
    static readonly Color FillPress    = new Color(0.08f, 0.17f, 0.26f, 0.95f);
    static readonly Color FillDisable  = new Color(0.05f, 0.06f, 0.08f, 0.42f);

    static readonly Color HighlightNorm   = new Color(0.85f, 0.95f, 1f, 0.025f);
    static readonly Color HighlightHover  = new Color(0.75f, 0.93f, 1f, 0.12f);
    static readonly Color HighlightPress  = new Color(0.90f, 0.98f, 1f, 0.18f);
    static readonly Color HighlightDisable= new Color(1f, 1f, 1f, 0.01f);

    static readonly Color TextNorm    = new Color(0.88f, 0.95f, 1f, 0.96f);
    static readonly Color TextHover   = Color.white;
    static readonly Color TextDisable = new Color(0.72f, 0.76f, 0.82f, 0.55f);

    const float BorderWidth = 1.2f;
    const float HoverScale = 1.04f;
    const float PressScale = 0.965f;
    const float HoverLift = 3f;

    const float ScaleLerpSpeed = 14f;
    const float PosLerpSpeed = 10f;
    const float PulseSpeed = 2.2f;
    const float PulseRange = 0.05f;
    const float HoverGlowPulseSpeed = 3.2f;

    Button button;
    Image borderImage;
    Image fillImage;
    Image highlightImage;
    TextMeshProUGUI label;
    RectTransform rect;

    Material labelMatNorm;
    Material labelMatHover;

    Vector3 baseScale;
    Vector2 baseAnchoredPos;

    float targetScale = 1f;
    Vector2 targetAnchoredPos;

    bool isHovered;
    bool isPressed;
    Coroutine pressRoutine;

    void Awake()
    {
        button = GetComponent<Button>();
        borderImage = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        label = GetComponentInChildren<TextMeshProUGUI>(true);

        if (rect != null)
        {
            baseScale = rect.localScale;
            baseAnchoredPos = rect.anchoredPosition;
            targetAnchoredPos = baseAnchoredPos;
        }

        if (button != null)
        {
            button.transition = Selectable.Transition.None;

            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
        }

        if (borderImage != null)
            borderImage.color = BorderNorm;

        var oldOutline = GetComponent<Outline>();
        if (oldOutline != null)
            Destroy(oldOutline);

        CreateOrResetFill();
        CreateOrResetHighlight();
        SetupLabel();
        ResetStateImmediate();
    }

    void OnEnable()
    {
        ResetStateImmediate();

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        bool interactable = button == null || button.interactable;

        if (rect != null)
        {
            float scale = targetScale;

            if (interactable && isHovered && !isPressed)
                scale *= 1f + Mathf.Sin(Time.time * 3f) * 0.003f;

            rect.localScale = Vector3.Lerp(
                rect.localScale,
                baseScale * scale,
                Time.deltaTime * ScaleLerpSpeed
            );

            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                targetAnchoredPos,
                Time.deltaTime * PosLerpSpeed
            );
        }

        if (!interactable)
        {
            Apply(BorderDisable, FillDisable, HighlightDisable, TextDisable, labelMatNorm);
            return;
        }

        if (!isHovered && !isPressed && borderImage != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed);
            Color c = BorderNorm;
            c.a = Mathf.Lerp(BorderNorm.a - PulseRange, BorderNorm.a + PulseRange, pulse);
            borderImage.color = c;
        }

        if (isHovered && !isPressed && highlightImage != null)
        {
            float glow = 0.5f + 0.5f * Mathf.Sin(Time.time * HoverGlowPulseSpeed);
            Color c = HighlightHover;
            c.a = Mathf.Lerp(0.08f, 0.16f, glow);
            highlightImage.color = c;
        }

        if (label != null && label.fontMaterial != null)
        {
            try
            {
                float dilate = (isHovered || isPressed)
                    ? Mathf.Sin(Time.time * 5f) * 0.015f
                    : 0f;
                label.fontMaterial.SetFloat("_FaceDilate", dilate);
            }
            catch { }
        }
    }

    void CreateOrResetFill()
    {
        Transform existing = transform.Find("ButtonFill");
        GameObject obj;

        if (existing != null) obj = existing.gameObject;
        else
        {
            obj = new GameObject("ButtonFill");
            obj.transform.SetParent(transform, false);
        }

        obj.transform.SetAsFirstSibling();

        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(BorderWidth, BorderWidth);
        rt.offsetMax = new Vector2(-BorderWidth, -BorderWidth);

        fillImage = obj.GetComponent<Image>();
        if (fillImage == null) fillImage = obj.AddComponent<Image>();

        fillImage.color = FillNorm;
        fillImage.raycastTarget = false;
    }

    void CreateOrResetHighlight()
    {
        Transform existing = transform.Find("ButtonHighlight");
        GameObject obj;

        if (existing != null) obj = existing.gameObject;
        else
        {
            obj = new GameObject("ButtonHighlight");
            obj.transform.SetParent(transform, false);
        }

        obj.transform.SetSiblingIndex(1);

        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(BorderWidth + 1f, 0f);
        rt.offsetMax = new Vector2(-BorderWidth - 1f, -BorderWidth - 1f);

        highlightImage = obj.GetComponent<Image>();
        if (highlightImage == null) highlightImage = obj.AddComponent<Image>();

        highlightImage.color = HighlightNorm;
        highlightImage.raycastTarget = false;
    }

    void SetupLabel()
    {
        if (label == null) return;

        label.color = TextNorm;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8;
        label.fontSizeMax = 16;

        if (label.fontSharedMaterial == null) return;

        labelMatNorm = new Material(label.fontSharedMaterial);
        labelMatNorm.EnableKeyword("GLOW_ON");
        labelMatNorm.SetFloat("_GlowOffset", 0.16f);
        labelMatNorm.SetFloat("_GlowOuter", 0.10f);
        labelMatNorm.SetFloat("_GlowInner", 0.03f);
        labelMatNorm.SetFloat("_GlowPower", 0.22f);
        labelMatNorm.SetColor("_GlowColor", new Color(0.55f, 0.86f, 1f, 0.14f));

        labelMatHover = new Material(label.fontSharedMaterial);
        labelMatHover.EnableKeyword("GLOW_ON");
        labelMatHover.SetFloat("_GlowOffset", 0.24f);
        labelMatHover.SetFloat("_GlowOuter", 0.18f);
        labelMatHover.SetFloat("_GlowInner", 0.05f);
        labelMatHover.SetFloat("_GlowPower", 0.38f);
        labelMatHover.SetColor("_GlowColor", new Color(0.78f, 0.96f, 1f, 0.28f));

        label.fontMaterial = labelMatNorm;
    }

    void ResetStateImmediate()
    {
        isHovered = false;
        isPressed = false;
        targetScale = 1f;
        targetAnchoredPos = baseAnchoredPos;

        if (rect != null)
        {
            rect.localScale = baseScale;
            rect.anchoredPosition = baseAnchoredPos;
        }

        bool interactable = button == null || button.interactable;

        if (!interactable)
            Apply(BorderDisable, FillDisable, HighlightDisable, TextDisable, labelMatNorm);
        else
            Apply(BorderNorm, FillNorm, HighlightNorm, TextNorm, labelMatNorm);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        isHovered = true;
        isPressed = false;
        targetScale = HoverScale;
        targetAnchoredPos = baseAnchoredPos + new Vector2(0f, HoverLift);

        Apply(BorderHover, FillHover, HighlightHover, TextHover, labelMatHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        isHovered = false;
        isPressed = false;
        targetScale = 1f;
        targetAnchoredPos = baseAnchoredPos;

        Apply(BorderNorm, FillNorm, HighlightNorm, TextNorm, labelMatNorm);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        if (pressRoutine != null)
            StopCoroutine(pressRoutine);

        pressRoutine = StartCoroutine(PressFlash());
    }

    public void OnPointerUp(PointerEventData eventData) { }

    public void OnSelect(BaseEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (button != null && !button.interactable)
        {
            Apply(BorderDisable, FillDisable, HighlightDisable, TextDisable, labelMatNorm);
            return;
        }

        isHovered = false;
        isPressed = false;
        targetScale = 1f;
        targetAnchoredPos = baseAnchoredPos;

        Apply(BorderNorm, FillNorm, HighlightNorm, TextNorm, labelMatNorm);
    }

    IEnumerator PressFlash()
    {
        isPressed = true;
        targetScale = PressScale;
        targetAnchoredPos = baseAnchoredPos + new Vector2(0f, HoverLift * 0.35f);

        Apply(BorderPress, FillPress, HighlightPress, TextHover, labelMatHover);

        yield return new WaitForSeconds(0.10f);

        isPressed = false;

        bool hovered = isHovered;
        targetScale = hovered ? HoverScale : 1f;
        targetAnchoredPos = hovered
            ? baseAnchoredPos + new Vector2(0f, HoverLift)
            : baseAnchoredPos;

        Apply(
            hovered ? BorderHover : BorderNorm,
            hovered ? FillHover : FillNorm,
            hovered ? HighlightHover : HighlightNorm,
            hovered ? TextHover : TextNorm,
            hovered ? labelMatHover : labelMatNorm
        );

        pressRoutine = null;
    }

    void Apply(Color border, Color fill, Color highlight, Color text, Material textMat)
    {
        if (borderImage != null) borderImage.color = border;
        if (fillImage != null) fillImage.color = fill;
        if (highlightImage != null) highlightImage.color = highlight;

        if (label != null)
        {
            label.color = text;
            if (textMat != null)
                label.fontMaterial = textMat;
        }
    }
}