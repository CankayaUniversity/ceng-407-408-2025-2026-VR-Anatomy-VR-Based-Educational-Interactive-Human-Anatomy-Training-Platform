using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Soft highlight + pulse + full-box metallic overlay for a single menu button.
/// Driven by MenuIntroManager during the intro sequence.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuItemHighlighter : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private float highlightScale = 1.04f;
    [SerializeField] private Color highlightTint = new Color(0.82f, 0.92f, 1f, 1f);
    [SerializeField] private float tintStrength = 0.10f;
    [SerializeField] private float brightnessBoost = 1.08f;
    [SerializeField] private float transitionDuration = 0.20f;

    [Header("Pulse")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseMinScale = 1.02f;
    [SerializeField] private float pulseMaxScale = 1.05f;
    [SerializeField] private float pulseSpeed = 2.0f;

    [Header("Metallic Overlay")]
    [SerializeField] private bool enableMetallicOverlay = true;
    [SerializeField] private Color metallicOverlayColor = new Color(1f, 1f, 1f, 0.16f);

    [Header("Dim")]
    [SerializeField] private float dimAlpha = 0.35f;

    private Vector3 _originalScale;
    private Color _originalColor;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    private Image _metallicOverlayImage;
    private RectTransform _metallicOverlayRect;

    private Coroutine _transition;
    private Coroutine _pulse;
    private bool _cached;

    private void Awake()
    {
        CacheOriginals();
        ResetToNormal();
    }

    private void OnEnable()
    {
        CacheOriginals();
        ResetToNormal();
    }

    private void CacheOriginals()
    {
        if (_cached) return;
        _cached = true;

        _rectTransform = GetComponent<RectTransform>();
        _originalScale = transform.localScale;

        _image = GetComponent<Image>();
        _originalColor = _image != null ? _image.color : Color.white;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CreateMetallicOverlayIfNeeded();
    }

    private void CreateMetallicOverlayIfNeeded()
    {
        if (_metallicOverlayImage != null) return;

        GameObject overlayGO = new GameObject("MetallicOverlay", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(transform, false);
        overlayGO.layer = gameObject.layer;

        _metallicOverlayRect = overlayGO.GetComponent<RectTransform>();
        _metallicOverlayRect.anchorMin = Vector2.zero;
        _metallicOverlayRect.anchorMax = Vector2.one;
        _metallicOverlayRect.offsetMin = Vector2.zero;
        _metallicOverlayRect.offsetMax = Vector2.zero;
        _metallicOverlayRect.SetAsLastSibling();

        _metallicOverlayImage = overlayGO.GetComponent<Image>();
        _metallicOverlayImage.raycastTarget = false;

        // Ana butonun þekli neyse onu kopyala
        if (_image != null)
        {
            _metallicOverlayImage.sprite = _image.sprite;
            _metallicOverlayImage.type = _image.type;
            _metallicOverlayImage.preserveAspect = _image.preserveAspect;
            _metallicOverlayImage.fillCenter = _image.fillCenter;
            _metallicOverlayImage.pixelsPerUnitMultiplier = _image.pixelsPerUnitMultiplier;
            _metallicOverlayImage.material = _image.material;
        }

        _metallicOverlayImage.color = new Color(
            metallicOverlayColor.r,
            metallicOverlayColor.g,
            metallicOverlayColor.b,
            0f
        );
        if (_metallicOverlayRect != null)
        {
            _metallicOverlayRect.localScale = Vector3.one;
            _metallicOverlayRect.localRotation = Quaternion.identity;
        }
    }

    public void Highlight()
    {
        CacheOriginals();
        StopAllEffects();
        _transition = StartCoroutine(HighlightSequence());
    }

    public void Unhighlight()
    {
        CacheOriginals();
        StopAllEffects();
        _transition = StartCoroutine(UnhighlightSequence());
    }

    public void SetDimmed()
    {
        CacheOriginals();
        StopAllEffects();
        HideMetallicOverlayImmediate();
        _transition = StartCoroutine(TransitionTo(1f, _originalColor, dimAlpha));
    }

    public void ResetToNormal()
    {
        CacheOriginals();
        StopAllEffects();

        transform.localScale = _originalScale;

        if (_image != null)
            _image.color = _originalColor;

        _canvasGroup.alpha = 1f;
        HideMetallicOverlayImmediate();
    }

    private void StopAllEffects()
    {
        if (_transition != null)
        {
            StopCoroutine(_transition);
            _transition = null;
        }

        if (_pulse != null)
        {
            StopCoroutine(_pulse);
            _pulse = null;
        }
    }

    private IEnumerator HighlightSequence()
    {
        Color targetColor = ComputeSoftHighlightColor();
        yield return TransitionTo(highlightScale, targetColor, 1f);

        if (enablePulse)
            _pulse = StartCoroutine(PulseLoop());

        if (enableMetallicOverlay)
            ShowMetallicOverlay();
    }

    private IEnumerator UnhighlightSequence()
    {
        HideMetallicOverlayImmediate();
        yield return TransitionTo(1f, _originalColor, 1f);
    }

    private Color ComputeSoftHighlightColor()
    {
        Color mixed = Color.Lerp(_originalColor, highlightTint, tintStrength);
        mixed.r = Mathf.Clamp01(mixed.r * brightnessBoost);
        mixed.g = Mathf.Clamp01(mixed.g * brightnessBoost);
        mixed.b = Mathf.Clamp01(mixed.b * brightnessBoost);
        mixed.a = _originalColor.a;
        return mixed;
    }

    private IEnumerator TransitionTo(float scaleMul, Color color, float alpha)
    {
        Vector3 fromScale = transform.localScale;
        Vector3 toScale = _originalScale * scaleMul;

        Color fromColor = _image != null ? _image.color : Color.white;
        float fromAlpha = _canvasGroup.alpha;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / transitionDuration);

            transform.localScale = Vector3.Lerp(fromScale, toScale, p);

            if (_image != null)
                _image.color = Color.Lerp(fromColor, color, p);

            _canvasGroup.alpha = Mathf.Lerp(fromAlpha, alpha, p);

            yield return null;
        }

        transform.localScale = toScale;

        if (_image != null)
            _image.color = color;

        _canvasGroup.alpha = alpha;
    }

    private IEnumerator PulseLoop()
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;

            float s = Mathf.Lerp(
                pulseMinScale,
                pulseMaxScale,
                (Mathf.Sin(t * Mathf.PI) + 1f) * 0.5f
            );

            transform.localScale = _originalScale * s;
            yield return null;
        }
    }

    private void ShowMetallicOverlay()
    {
        if (_metallicOverlayImage == null) return;

        _metallicOverlayImage.color = metallicOverlayColor;
    }

    private void HideMetallicOverlayImmediate()
    {
        if (_metallicOverlayImage == null) return;

        _metallicOverlayImage.color = new Color(
            metallicOverlayColor.r,
            metallicOverlayColor.g,
            metallicOverlayColor.b,
            0f
        );
    }
}