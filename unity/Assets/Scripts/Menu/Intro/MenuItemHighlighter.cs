using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Highlight, dim, and pulse effects for a single menu button.
/// Driven by MenuIntroManager during the intro sequence.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuItemHighlighter : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private float highlightScale = 1.12f;
    [SerializeField] private Color highlightTint = Color.white;
    [SerializeField] private float transitionDuration = 0.3f;

    [Header("Pulse")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseMinScale = 1.08f;
    [SerializeField] private float pulseMaxScale = 1.14f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("Dim")]
    [SerializeField] private float dimAlpha = 0.35f;

    private Vector3 _originalScale;
    private Color _originalColor;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private Coroutine _transition;
    private Coroutine _pulse;
    private bool _cached;

    private void Awake() => CacheOriginals();

    private void CacheOriginals()
    {
        if (_cached) return;
        _cached = true;

        _originalScale = transform.localScale;
        _image = GetComponent<Image>();
        if (_image != null) _originalColor = _image.color;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Highlight()
    {
        CacheOriginals();
        StopAll();
        _transition = StartCoroutine(HighlightSequence());
    }

    public void Unhighlight()
    {
        CacheOriginals();
        StopAll();
        _transition = StartCoroutine(TransitionTo(1f, _originalColor, 1f));
    }

    public void SetDimmed()
    {
        CacheOriginals();
        StopAll();
        _transition = StartCoroutine(TransitionTo(1f, _originalColor, dimAlpha));
    }

    public void ResetToNormal()
    {
        CacheOriginals();
        StopAll();
        transform.localScale = _originalScale;
        if (_image != null) _image.color = _originalColor;
        _canvasGroup.alpha = 1f;
    }

    private void StopAll()
    {
        if (_transition != null) { StopCoroutine(_transition); _transition = null; }
        if (_pulse != null) { StopCoroutine(_pulse); _pulse = null; }
    }

    private IEnumerator HighlightSequence()
    {
        yield return TransitionTo(highlightScale, highlightTint, 1f);

        if (enablePulse)
            _pulse = StartCoroutine(PulseLoop());
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
            if (_image != null) _image.color = Color.Lerp(fromColor, color, p);
            _canvasGroup.alpha = Mathf.Lerp(fromAlpha, alpha, p);

            yield return null;
        }

        transform.localScale = toScale;
        if (_image != null) _image.color = color;
        _canvasGroup.alpha = alpha;
    }

    private IEnumerator PulseLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float s = Mathf.Lerp(pulseMinScale, pulseMaxScale,
                (Mathf.Sin(t * Mathf.PI) + 1f) * 0.5f);
            transform.localScale = _originalScale * s;
            yield return null;
        }
    }
}
