using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class VRButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    // ── Border colours ──
    static readonly Color BorderNorm  = new Color(0f, 0.55f, 0.75f, 0.35f);
    static readonly Color BorderHover = new Color(0f, 0.90f, 1f, 0.95f);
    static readonly Color BorderFlash = new Color(0.6f, 1f, 1f, 1f);

    // ── Inner fill ──
    static readonly Color FillNorm  = new Color(0.01f, 0.06f, 0.12f, 0.70f);
    static readonly Color FillHover = new Color(0.04f, 0.18f, 0.30f, 0.88f);
    static readonly Color FillFlash = new Color(0.10f, 0.35f, 0.50f, 0.95f);

    // ── Top highlight gradient layer ──
    static readonly Color HighlightNorm  = new Color(0.15f, 0.65f, 0.85f, 0.06f);
    static readonly Color HighlightHover = new Color(0.15f, 0.75f, 1f, 0.18f);

    // ── Text ──
    static readonly Color TextNorm  = new Color(0.80f, 0.94f, 1f, 1f);
    static readonly Color TextHover = Color.white;

    const float BorderWidth  = 1.2f;
    const float HoverScale   = 1.06f;
    const float PressScale   = 0.94f;
    const float LerpSpeed    = 14f;
    const float PulseSpeed   = 2.5f;
    const float PulseRange   = 0.12f;

    Image borderImage;
    Image fillImage;
    Image highlightImage;
    TextMeshProUGUI label;
    Material labelMatNorm;
    Material labelMatHover;
    RectTransform rect;
    Vector3 baseScale;
    float targetScale = 1f;
    bool isHovered;

    void Awake()
    {
        borderImage = GetComponent<Image>();
        rect  = GetComponent<RectTransform>();
        label = GetComponentInChildren<TextMeshProUGUI>();
        baseScale = rect.localScale;

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.transition = Selectable.Transition.None;

        if (borderImage != null)
            borderImage.color = BorderNorm;

        var oldOutline = GetComponent<Outline>();
        if (oldOutline != null)
            Destroy(oldOutline);

        CreateFill();
        CreateHighlight();
        SetupLabel();
    }

    void CreateFill()
    {
        var fillObj = new GameObject("ButtonFill");
        fillObj.transform.SetParent(transform, false);
        fillObj.transform.SetAsFirstSibling();

        var fr = fillObj.AddComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(BorderWidth, BorderWidth);
        fr.offsetMax = new Vector2(-BorderWidth, -BorderWidth);

        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = FillNorm;
        fillImage.raycastTarget = false;
    }

    void CreateHighlight()
    {
        var hlObj = new GameObject("ButtonHighlight");
        hlObj.transform.SetParent(transform, false);
        hlObj.transform.SetSiblingIndex(1);

        var hr = hlObj.AddComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 0.5f);
        hr.anchorMax = Vector2.one;
        hr.offsetMin = new Vector2(BorderWidth + 1f, 0f);
        hr.offsetMax = new Vector2(-BorderWidth - 1f, -BorderWidth - 1f);

        highlightImage = hlObj.AddComponent<Image>();
        highlightImage.color = HighlightNorm;
        highlightImage.raycastTarget = false;
    }

    void SetupLabel()
    {
        if (label == null) return;

        label.color = TextNorm;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8;
        label.fontSizeMax = 14;

        if (label.fontSharedMaterial == null) return;

        labelMatNorm = new Material(label.fontSharedMaterial);
        labelMatNorm.EnableKeyword("GLOW_ON");
        labelMatNorm.SetFloat("_GlowOffset", 0.35f);
        labelMatNorm.SetFloat("_GlowOuter", 0.20f);
        labelMatNorm.SetFloat("_GlowInner", 0.08f);
        labelMatNorm.SetFloat("_GlowPower", 0.45f);
        labelMatNorm.SetColor("_GlowColor", new Color(0f, 0.75f, 1f, 0.30f));

        labelMatHover = new Material(label.fontSharedMaterial);
        labelMatHover.EnableKeyword("GLOW_ON");
        labelMatHover.SetFloat("_GlowOffset", 0.45f);
        labelMatHover.SetFloat("_GlowOuter", 0.45f);
        labelMatHover.SetFloat("_GlowInner", 0.15f);
        labelMatHover.SetFloat("_GlowPower", 0.80f);
        labelMatHover.SetColor("_GlowColor", new Color(0f, 0.92f, 1f, 0.70f));

        label.fontMaterial = labelMatNorm;
    }

    void Update()
    {
        // Smooth scale interpolation
        float s = targetScale;
        if (isHovered)
            s *= 1f + Mathf.Sin(Time.time * 3.5f) * 0.006f;

        rect.localScale = Vector3.Lerp(
            rect.localScale,
            baseScale * s,
            Time.deltaTime * LerpSpeed);

        // Subtle border pulse when idle (breathing effect)
        if (!isHovered && borderImage != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed);
            Color c = BorderNorm;
            c.a = Mathf.Lerp(BorderNorm.a - PulseRange, BorderNorm.a + PulseRange, pulse);
            borderImage.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = HoverScale;
        Apply(BorderHover, FillHover, HighlightHover, TextHover, labelMatHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
        Apply(BorderNorm, FillNorm, HighlightNorm, TextNorm, labelMatNorm);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(PressFlash());
    }

    public void OnPointerUp(PointerEventData eventData) { }

    IEnumerator PressFlash()
    {
        targetScale = PressScale;
        Apply(BorderFlash, FillFlash, HighlightHover, TextHover, labelMatHover);

        yield return new WaitForSeconds(0.12f);

        bool h = isHovered;
        targetScale = h ? HoverScale : 1f;
        Apply(
            h ? BorderHover  : BorderNorm,
            h ? FillHover    : FillNorm,
            h ? HighlightHover : HighlightNorm,
            h ? TextHover    : TextNorm,
            h ? labelMatHover : labelMatNorm);
    }

    void Apply(Color border, Color fill, Color highlight, Color text, Material mat)
    {
        if (borderImage    != null) borderImage.color    = border;
        if (fillImage      != null) fillImage.color      = fill;
        if (highlightImage != null) highlightImage.color = highlight;
        if (label != null)
        {
            label.color = text;
            if (mat != null) label.fontMaterial = mat;
        }
    }
}
