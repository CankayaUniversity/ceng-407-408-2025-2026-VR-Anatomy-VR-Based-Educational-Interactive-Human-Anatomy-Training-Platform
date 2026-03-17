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
    static readonly Color BorderNorm  = new Color(0f, 0.40f, 0.55f, 0.18f);
    static readonly Color BorderHover = new Color(0f, 0.85f, 1f, 0.85f);
    static readonly Color BorderFlash = new Color(0.5f, 1f, 1f, 1f);

    static readonly Color FillNorm  = new Color(0.02f, 0.05f, 0.08f, 0.78f);
    static readonly Color FillHover = new Color(0.07f, 0.26f, 0.38f, 0.92f);
    static readonly Color FillFlash = new Color(0.15f, 0.45f, 0.55f, 1f);

    static readonly Color TextNorm  = new Color(0.80f, 0.94f, 1f, 1f);
    static readonly Color TextHover = Color.white;

    const float BorderWidth = 1.5f;
    const float HoverScale  = 1.08f;
    const float PressScale  = 0.92f;
    const float LerpSpeed   = 12f;

    Image borderImage;
    Image fillImage;
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

    void SetupLabel()
    {
        if (label == null) return;

        label.color = TextNorm;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10;
        label.fontSizeMax = 20;

        if (label.fontSharedMaterial == null) return;

        labelMatNorm = new Material(label.fontSharedMaterial);
        labelMatNorm.EnableKeyword("GLOW_ON");
        labelMatNorm.SetFloat("_GlowOffset", 0.30f);
        labelMatNorm.SetFloat("_GlowOuter", 0.15f);
        labelMatNorm.SetFloat("_GlowInner", 0.05f);
        labelMatNorm.SetFloat("_GlowPower", 0.35f);
        labelMatNorm.SetColor("_GlowColor", new Color(0f, 0.80f, 1f, 0.25f));

        labelMatHover = new Material(label.fontSharedMaterial);
        labelMatHover.EnableKeyword("GLOW_ON");
        labelMatHover.SetFloat("_GlowOffset", 0.40f);
        labelMatHover.SetFloat("_GlowOuter", 0.35f);
        labelMatHover.SetFloat("_GlowInner", 0.10f);
        labelMatHover.SetFloat("_GlowPower", 0.65f);
        labelMatHover.SetColor("_GlowColor", new Color(0f, 0.90f, 1f, 0.60f));

        label.fontMaterial = labelMatNorm;
    }

    void Update()
    {
        float s = targetScale;
        if (isHovered)
            s *= 1f + Mathf.Sin(Time.time * 3f) * 0.008f;

        rect.localScale = Vector3.Lerp(
            rect.localScale,
            baseScale * s,
            Time.deltaTime * LerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = HoverScale;
        Apply(BorderHover, FillHover, TextHover, labelMatHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
        Apply(BorderNorm, FillNorm, TextNorm, labelMatNorm);
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
        Apply(BorderFlash, FillFlash, TextHover, labelMatHover);

        yield return new WaitForSeconds(0.1f);

        bool h = isHovered;
        targetScale = h ? HoverScale : 1f;
        Apply(
            h ? BorderHover : BorderNorm,
            h ? FillHover   : FillNorm,
            h ? TextHover   : TextNorm,
            h ? labelMatHover : labelMatNorm);
    }

    void Apply(Color border, Color fill, Color text, Material mat)
    {
        if (borderImage != null) borderImage.color = border;
        if (fillImage   != null) fillImage.color   = fill;
        if (label != null)
        {
            label.color = text;
            if (mat != null) label.fontMaterial = mat;
        }
    }
}
