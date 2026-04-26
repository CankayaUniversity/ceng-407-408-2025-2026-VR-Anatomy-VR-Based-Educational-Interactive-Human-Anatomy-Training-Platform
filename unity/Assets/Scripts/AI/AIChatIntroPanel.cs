using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Yapay Zeka ile Konuş sahnesinin girişinde, Quiz intro paneliyle (01_Menu sahnesindeki
// QuizIntroPanel) aynı tasarım ve akışta çalışan bilgilendirme ekranını runtime'da kurar.
//
// Stil birebir Quiz'den alındı:
//   • Panel arka planı: Sci-Fi UI atlas "window" sprite'ı, 9-slice, beyaz tint
//   • Metin: TMP, rengi (0.8, 0.94, 1, 1), sol-üst hizalı, > cyan bullet, auto-size
//   • Butonlar: yarı saydam, rounded ve cyan glow hover/press durumları
//   • Anchor düzeni: IntroText (0.08-0.92, 0.22-0.82),
//                    BackButton (0.09-0.31, 0.07-0.19),
//                    StartButton/"Devam Et" (0.69-0.91, 0.07-0.19)
public class AIChatIntroPanel : MonoBehaviour
{
    private const string PanelName = "AIChatIntroPanel";
    private const string MenuSceneName = "01_Menu";

    private static readonly string[] BodyLines =
    {
        "Bu bölümde anatomi hakkında istediğin soruları sorabilirsin.",
        "Sorunu yazabilir veya \"Konuş\" butonuna basarak sesli olarak iletebilirsin.",
        "Konuşmanın sonunda \"cevapla\" diyerek sorunu otomatik gönderebilir ya da \"Sor\" butonuyla manuel olarak gönderebilirsin.",
        "\"Dinle\" ile cevabı dinleyebilir, \"Cevabı Gör\" ile yazılı olarak inceleyebilirsin."
    };

    // Quiz intro panelindeki renk değerleri.
    private static readonly Color TextColor       = new Color(0.8f, 0.94f, 1f, 1f);
    private static readonly Color ButtonFillColor = new Color(0.03f, 0.28f, 0.40f, 0.46f);
    private static readonly Color ButtonHoverColor = new Color(0.05f, 0.35f, 0.49f, 0.56f);
    private static readonly Color ButtonPressedColor = new Color(0.02f, 0.24f, 0.34f, 0.52f);
    private static readonly Color ButtonGlowColor = new Color(0f, 0.82f, 1f, 0.16f);
    private static readonly Color ButtonGlowHoverColor = new Color(0f, 0.88f, 1f, 0.28f);
    private static readonly Color ButtonGlowPressedColor = new Color(0f, 0.72f, 0.95f, 0.12f);
    private static readonly Color ButtonTopTintColor = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ButtonBottomTintColor = new Color(0f, 0f, 0f, 0.08f);
    private static readonly Color PanelFallbackColor = new Color(0.04f, 0.22f, 0.32f, 0.85f);

    private GameObject _panelRoot;
    private readonly List<GameObject> _hiddenDuringIntro = new List<GameObject>();
    private bool _hasShown;
    private Action _onContinueCallback;

    private Sprite _panelSprite;
    private TMP_FontAsset _fontAsset;
    private Transform _titleTransform;
    private Vector3 _titleOriginalPosition;
    private Vector3 _titleOriginalScale;
    private bool _titleCentered;
    private Sprite _roundedButtonSprite;

    public void Show(
        Canvas canvas,
        Sprite panelSprite,
        TMP_FontAsset fontAsset,
        GameObject titleObject,
        IEnumerable<GameObject> hideWhileIntro,
        Action onContinue = null)
    {
        if (_hasShown || canvas == null) return;

        _panelSprite = panelSprite;
        _fontAsset = fontAsset;
        _onContinueCallback = onContinue;
        CenterTitle(titleObject);

        if (hideWhileIntro != null)
        {
            foreach (var go in hideWhileIntro)
            {
                if (go == null) continue;
                _hiddenDuringIntro.Add(go);
                go.SetActive(false);
            }
        }

        BuildPanel(canvas);
        _hasShown = true;
    }

    #region Panel Construction

    private void BuildPanel(Canvas canvas)
    {
        _panelRoot = CreateRect(PanelName, canvas.transform);
        var rt = (RectTransform)_panelRoot.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = ResolvePanelSize(canvas);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.SetAsLastSibling();

        _panelRoot.AddComponent<CanvasRenderer>();
        var panelImg = _panelRoot.AddComponent<Image>();
        if (_panelSprite != null)
        {
            panelImg.sprite = _panelSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.fillCenter = true;
            panelImg.pixelsPerUnitMultiplier = 1f;
            panelImg.color = Color.white;
        }
        else
        {
            panelImg.color = PanelFallbackColor;
        }
        panelImg.raycastTarget = true;

        BuildIntroText(rt);
        BuildBackButton(rt);
        BuildContinueButton(rt);
    }

    private static Vector2 ResolvePanelSize(Canvas canvas)
    {
        const float defaultWidth = 980f;
        const float defaultHeight = 560f;

        var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null) return new Vector2(defaultWidth, defaultHeight);

        float cw = canvasRect.rect.width;
        float ch = canvasRect.rect.height;
        if (cw <= 1f || ch <= 1f) return new Vector2(defaultWidth, defaultHeight);

        // Quiz intro hissini korumak için paneli ekranın yaklaşık %55'i kadar tut.
        float width = Mathf.Clamp(cw * 0.55f, 820f, 1120f);
        float height = Mathf.Clamp(ch * 0.55f, 480f, 640f);
        return new Vector2(width, height);
    }

    private void BuildIntroText(RectTransform parent)
    {
        var go = CreateRect("IntroText", parent);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.08f, 0.22f);
        rt.anchorMax = new Vector2(0.92f, 0.82f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = new Vector2(0f, 16f);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) tmp.font = _fontAsset;
        tmp.text = BuildBodyText();
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20f;
        tmp.fontSizeMax = 34f;
        tmp.lineSpacing = 0f;
        tmp.paragraphSpacing = -4f;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        tmp.raycastTarget = false;
    }

    private static string BuildBodyText()
{
    var sb = new System.Text.StringBuilder();

    const string bullet = "<indent=0%><color=#00D4FF>></color>  <indent=5%>";

    for (int i = 0; i < BodyLines.Length; i++)
    {
        sb.Append(bullet);

        string line = BodyLines[i]
            .Replace("\"Konuş\"", "\"<b>Konuş</b>\"")
            .Replace("\"Sor\"", "\"<b>Sor</b>\"")
            .Replace("\"Dinle\"", "\"<b>Dinle</b>\"")
            .Replace("\"Cevabı Gör\"", "\"<b>Cevabı Gör</b>\"")
            .Replace("\"cevapla\"", "\"<b>cevapla</b>\"");

        sb.Append(line);

        if (i < BodyLines.Length - 1) sb.Append('\n');
    }

    return sb.ToString();
}

    private void BuildBackButton(RectTransform parent)
    {
        var btn = BuildFlatButton(parent, "BackButton", "Geri");
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(170f, 70f);
        rt.sizeDelta = GetButtonSize("Geri");
        btn.onClick.AddListener(OnBackClicked);
    }

    private void BuildContinueButton(RectTransform parent)
    {
        var btn = BuildFlatButton(parent, "ContinueButton", "Devam Et");
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-190f, 70f);
        rt.sizeDelta = GetButtonSize("Devam Et");
        btn.onClick.AddListener(OnContinueClicked);
    }

    private static Vector2 GetButtonSize(string label)
    {
        // Sabit ve simetrik formu korurken metne göre dengeli genişlik ver.
        float width = label.Length <= 5 ? 150f : 180f;
        return new Vector2(width, 38f);
    }

    private Button BuildFlatButton(RectTransform parent, string goName, string label)
    {
        var go = CreateRect(goName, parent);
        go.AddComponent<CanvasRenderer>();

        var img = go.AddComponent<Image>();
        img.sprite = ResolveRoundedButtonSprite();
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        img.color = ButtonFillColor;
        img.raycastTarget = true;

        BuildButtonToneOverlay(go.transform);

        var glow = go.AddComponent<Outline>();
        glow.effectColor = ButtonGlowColor;
        glow.effectDistance = new Vector2(1.0f, 1.0f);
        glow.useGraphicAlpha = false;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.None;

        var labelGo = CreateRect("Text (TMP)", go.transform);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f, 6f);
        lrt.offsetMax = new Vector2(-12f, -6f);

        labelGo.AddComponent<CanvasRenderer>();
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) tmp.font = _fontAsset;
        tmp.text = label;
        tmp.color = TextColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 24f;
        tmp.characterSpacing = 0.35f;
        tmp.raycastTarget = false;

        var visuals = go.AddComponent<VRButtonVisualState>();
        visuals.Setup(
            img,
            glow,
            ButtonFillColor,
            ButtonHoverColor,
            ButtonPressedColor,
            ButtonGlowColor,
            ButtonGlowHoverColor,
            ButtonGlowPressedColor);

        return button;
    }

    private Sprite ResolveRoundedButtonSprite()
    {
        if (_roundedButtonSprite != null) return _roundedButtonSprite;
        _roundedButtonSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        return _roundedButtonSprite;
    }

    private void BuildButtonToneOverlay(Transform buttonTransform)
    {
        var top = CreateRect("TopTint", buttonTransform);
        var topRt = (RectTransform)top.transform;
        topRt.anchorMin = new Vector2(0f, 0.5f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.offsetMin = Vector2.zero;
        topRt.offsetMax = Vector2.zero;
        var topImage = top.AddComponent<Image>();
        topImage.color = ButtonTopTintColor;
        topImage.raycastTarget = false;

        var bottom = CreateRect("BottomTint", buttonTransform);
        var bottomRt = (RectTransform)bottom.transform;
        bottomRt.anchorMin = new Vector2(0f, 0f);
        bottomRt.anchorMax = new Vector2(1f, 0.5f);
        bottomRt.offsetMin = Vector2.zero;
        bottomRt.offsetMax = Vector2.zero;
        var bottomImage = bottom.AddComponent<Image>();
        bottomImage.color = ButtonBottomTintColor;
        bottomImage.raycastTarget = false;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        int layer = parent != null ? parent.gameObject.layer : 5;
        if (layer < 0) layer = 5;
        go.layer = layer;
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    private void CenterTitle(GameObject titleObject)
    {
        if (titleObject == null) return;

        _titleTransform = titleObject.transform;
        _titleOriginalPosition = _titleTransform.position;
        _titleOriginalScale = _titleTransform.localScale;
        _titleCentered = true;

        // 05_AIChat sahnesindeki "logo" world-space Sprite olduğu için
        // intro süresince yatay eksende merkeze alıyoruz.
        var pos = _titleTransform.position;
        _titleTransform.position = new Vector3(0f, pos.y - 14f, pos.z);
        _titleTransform.localScale = _titleOriginalScale * 1.25f;
    }

    #endregion

    #region Button Callbacks

    private void OnBackClicked()
    {
        // AI Chat intro ekranından çıkışta menü intro'su tekrar tetiklenmemeli.
        NavigationState.SkipMenuIntroOnce = true;
        // Kullanıcıyı her zaman doğrudan ana menü paneline döndür.
        NavigationState.ReturnMenuPanelName = null;
        NavigationState.ClearRuntimeOnly();
        SceneManager.LoadScene(MenuSceneName);
    }

    private void OnContinueClicked()
    {
        foreach (var go in _hiddenDuringIntro)
        {
            if (go != null) go.SetActive(true);
        }
        _hiddenDuringIntro.Clear();

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        if (_titleCentered && _titleTransform != null)
        {
            _titleTransform.position = _titleOriginalPosition;
            _titleTransform.localScale = _titleOriginalScale;
        }

        _onContinueCallback?.Invoke();
    }

    #endregion
}

[DisallowMultipleComponent]
public sealed class VRButtonVisualState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image _image;
    private Outline _glow;
    private RectTransform _rect;
    private Color _normalColor;
    private Color _hoverColor;
    private Color _pressedColor;
    private Color _normalGlowColor;
    private Color _hoverGlowColor;
    private Color _pressedGlowColor;
    private bool _isHovered;

    public void Setup(
        Image image,
        Outline glow,
        Color normalColor,
        Color hoverColor,
        Color pressedColor,
        Color normalGlowColor,
        Color hoverGlowColor,
        Color pressedGlowColor)
    {
        _image = image;
        _glow = glow;
        _rect = transform as RectTransform;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
        _pressedColor = pressedColor;
        _normalGlowColor = normalGlowColor;
        _hoverGlowColor = hoverGlowColor;
        _pressedGlowColor = pressedGlowColor;

        ApplyNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        ApplyHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isHovered) ApplyHover();
        else ApplyNormal();
    }

    private void ApplyNormal()
    {
        if (_image != null) _image.color = _normalColor;
        if (_glow != null)
        {
            _glow.effectColor = _normalGlowColor;
            _glow.effectDistance = new Vector2(1.0f, 1.0f);
        }
        if (_rect != null)
        {
            _rect.localScale = Vector3.one;
        }
    }

    private void ApplyHover()
    {
        if (_image != null) _image.color = _hoverColor;
        if (_glow != null)
        {
            _glow.effectColor = _hoverGlowColor;
            _glow.effectDistance = new Vector2(1.35f, 1.35f);
        }
        if (_rect != null)
        {
            _rect.localScale = Vector3.one;
        }
    }

    private void ApplyPressed()
    {
        if (_image != null) _image.color = _pressedColor;
        if (_glow != null)
        {
            _glow.effectColor = _pressedGlowColor;
            _glow.effectDistance = new Vector2(0.75f, 0.75f);
        }
        if (_rect != null)
        {
            _rect.localScale = Vector3.one;
        }
    }
}
