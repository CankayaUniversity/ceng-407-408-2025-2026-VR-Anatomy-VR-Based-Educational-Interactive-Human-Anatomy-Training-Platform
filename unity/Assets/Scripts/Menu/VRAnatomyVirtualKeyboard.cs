using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRAnatomyVirtualKeyboard : MonoBehaviour
{
    [Header("Parent / Position")]
    [Tooltip("Klavye hangi panelin içinde oluşsun? Buraya ana mavi panelini ver.")]
    [SerializeField] private RectTransform keyboardParentOverride;

    [Tooltip("Klavye her zaman hangi input'un altına yerleşsin? 2 kutucuk varsa buraya Kod inputunu ver.")]
    [SerializeField] private TMP_InputField keyboardPositionReferenceInput;

    [Tooltip("Klavye referans inputun altından ne kadar kayacak?")]
    [SerializeField] private Vector2 keyboardOffset = new Vector2(0f, -12f);

    [Header("Compact Size")]
    [SerializeField] private Vector2 keyboardSize = new Vector2(560f, 180f);
    [SerializeField] private float letterKeyWidth = 37f;
    [SerializeField] private float letterKeyHeight = 34f;
    [SerializeField] private float controlKeyHeight = 34f;
    [SerializeField] private float keySpacing = 6f;
    [SerializeField] private float rowSpacing = 5f;

    [Header("Submit")]
    [SerializeField] private Button defaultSubmitButton;
    [SerializeField] private UnityEvent onSendPressed;
    [SerializeField] private bool hideKeyboardOnSend = true;

    private TMP_InputField activeInput;
    private Button submitButtonForCurrentInput;

    private GameObject virtualKeyboardPanel;
    private bool keyboardVisible;
    private Coroutine selectInputRoutine;

    private static Sprite runtimeRoundedSprite;

    private readonly string[][] letterRows =
    {
        new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "Ğ", "Ü" },
        new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", "Ş", "İ" },
        new[] { "Z", "X", "C", "V", "B", "N", "M", "Ö", "Ç" }
    };

    private readonly string[] controlKeys =
    {
        "SİL", "BOŞLUK", "TEMİZLE", "KAPAT", "GÖNDER"
    };

    private void Start()
    {
        SetKeyboardVisible(false);
    }

    public void ShowForInput(TMP_InputField inputField)
    {
        ShowForInput(inputField, null);
    }

    public void ShowForInput(TMP_InputField inputField, Button submitButtonOverride)
    {
        if (inputField == null)
            return;

        activeInput = inputField;
        submitButtonForCurrentInput = submitButtonOverride;

        EnsureVirtualKeyboardPanel();
        PositionKeyboard();
        SetKeyboardVisible(true);

        ForceInputLeftToRight(activeInput);
        MoveCaretToEnd(activeInput);

        if (selectInputRoutine != null)
            StopCoroutine(selectInputRoutine);

        selectInputRoutine = StartCoroutine(SelectInputNextFrame(activeInput));
    }

    private IEnumerator SelectInputNextFrame(TMP_InputField inputField)
    {
        // OnSelect / OnPointerDown sırasında Unity zaten seçim işlemi yapıyor olabilir.
        // Bir frame bekleyince "while already selecting an object" hatası kesilir.
        yield return null;

        if (inputField == null || !inputField.gameObject.activeInHierarchy)
        {
            selectInputRoutine = null;
            yield break;
        }

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != inputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }

        inputField.ActivateInputField();
        MoveCaretToEnd(inputField);

        selectInputRoutine = null;
    }

    public void HideKeyboard()
    {
        SetKeyboardVisible(false);
    }

    private void SetKeyboardVisible(bool visible)
    {
        keyboardVisible = visible;

        if (!visible && selectInputRoutine != null)
        {
            StopCoroutine(selectInputRoutine);
            selectInputRoutine = null;
        }

        if (visible)
            EnsureVirtualKeyboardPanel();

        if (virtualKeyboardPanel != null)
        {
            virtualKeyboardPanel.SetActive(visible);

            if (visible)
                virtualKeyboardPanel.transform.SetAsLastSibling();
        }

        if (!visible && activeInput != null)
            activeInput.DeactivateInputField();
    }

    private void EnsureVirtualKeyboardPanel()
    {
        if (virtualKeyboardPanel != null)
            return;

        RectTransform parentRT = GetKeyboardParent();

        if (parentRT == null)
        {
            Debug.LogError("[VRAnatomyVirtualKeyboard] Parent bulunamadı. Keyboard Parent Override alanına ana panelini ver.");
            return;
        }

        virtualKeyboardPanel = new GameObject(
            "VRVirtualKeyboardPanel_Compact",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        virtualKeyboardPanel.transform.SetParent(parentRT, false);

        RectTransform panelRT = virtualKeyboardPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.sizeDelta = keyboardSize;
        panelRT.localScale = Vector3.one;

        Image panelImage = virtualKeyboardPanel.GetComponent<Image>();
        panelImage.sprite = GetRoundedRuntimeSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.04f, 0.23f, 0.32f, 0.92f);
        panelImage.raycastTarget = true;

        VerticalLayoutGroup vertical = virtualKeyboardPanel.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.MiddleCenter;
        vertical.childControlWidth = false;
        vertical.childControlHeight = false;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;
        vertical.spacing = rowSpacing;
        vertical.padding = new RectOffset(10, 10, 10, 10);

        foreach (string[] row in letterRows)
            CreateKeyboardRow(panelRT, row, false);

        CreateKeyboardRow(panelRT, controlKeys, true);

        virtualKeyboardPanel.SetActive(false);
    }

    private RectTransform GetKeyboardParent()
    {
        if (keyboardParentOverride != null)
            return keyboardParentOverride;

        if (activeInput == null)
            return null;

        return activeInput.transform.parent as RectTransform;
    }

    private void PositionKeyboard()
    {
        if (virtualKeyboardPanel == null)
            return;

        RectTransform panelRT = virtualKeyboardPanel.GetComponent<RectTransform>();
        RectTransform parentRT = virtualKeyboardPanel.transform.parent as RectTransform;

        if (panelRT == null || parentRT == null)
            return;

        TMP_InputField referenceInput =
            keyboardPositionReferenceInput != null ? keyboardPositionReferenceInput : activeInput;

        if (referenceInput == null)
            return;

        RectTransform refRT = referenceInput.GetComponent<RectTransform>();

        if (refRT == null)
            return;

        Vector3[] corners = new Vector3[4];
        refRT.GetWorldCorners(corners);

        Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;
        Vector2 bottomCenterLocal = parentRT.InverseTransformPoint(bottomCenterWorld);

        panelRT.anchoredPosition = bottomCenterLocal + keyboardOffset;
    }

    private void CreateKeyboardRow(RectTransform parent, string[] keys, bool controlRow)
    {
        GameObject rowGO = new GameObject(
            controlRow ? "KeyboardControlRow" : "KeyboardLetterRow",
            typeof(RectTransform)
        );

        rowGO.transform.SetParent(parent, false);

        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(keyboardSize.x - 24f, controlRow ? controlKeyHeight : letterKeyHeight);
        rowRT.localScale = Vector3.one;

        HorizontalLayoutGroup horizontal = rowGO.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = false;
        horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        horizontal.spacing = keySpacing;

        foreach (string key in keys)
        {
            float width = controlRow ? GetControlKeyWidth(key) : letterKeyWidth;
            float height = controlRow ? controlKeyHeight : letterKeyHeight;

            CreateKeyboardKey(rowRT, key, width, height);
        }
    }

    private float GetControlKeyWidth(string key)
{
    switch (key)
    {
        case "SİL": return 32f;
        case "BOŞLUK": return 78f;
        case "TEMİZLE": return 58f;
        case "KAPAT": return 44f;
        case "GÖNDER": return 60f;
        default: return 36f;
    }
}

    private void CreateKeyboardKey(RectTransform rowRT, string label, float width, float height)
    {
        GameObject keyGO = new GameObject(
            "Key_" + label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        keyGO.transform.SetParent(rowRT, false);

        RectTransform keyRT = keyGO.GetComponent<RectTransform>();
        keyRT.sizeDelta = new Vector2(width, height);
        keyRT.localScale = Vector3.one;

        bool isControl = IsControlKeyboardKey(label);

        Image keyImage = keyGO.GetComponent<Image>();
        keyImage.sprite = GetRoundedRuntimeSprite();
        keyImage.type = Image.Type.Sliced;
        keyImage.color = isControl
            ? new Color(0.09f, 0.47f, 0.68f, 0.96f)
            : new Color(0.78f, 0.93f, 1f, 0.94f);
        keyImage.raycastTarget = true;

        Button keyButton = keyGO.GetComponent<Button>();
        keyButton.targetGraphic = keyImage;

        ColorBlock colors = keyButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.99f, 1f, 1f);
        colors.pressedColor = new Color(0.58f, 0.84f, 0.96f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.50f, 0.60f, 0.65f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        keyButton.colors = colors;

        GameObject textGO = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textGO.transform.SetParent(keyGO.transform, false);

        TMP_Text keyText = textGO.GetComponent<TMP_Text>();
        RectTransform textRT = keyText.GetComponent<RectTransform>();

        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        keyText.text = label;
        keyText.fontSize = isControl ? 11.5f : 20f;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = isControl
            ? new Color(0.94f, 0.99f, 1f, 1f)
            : new Color(0.06f, 0.32f, 0.46f, 1f);
        keyText.raycastTarget = false;

        string capturedLabel = label;
        keyButton.onClick.AddListener(() => HandleKeyboardKey(capturedLabel));
    }

    private bool IsControlKeyboardKey(string key)
    {
        return key == "SİL" ||
               key == "BOŞLUK" ||
               key == "TEMİZLE" ||
               key == "KAPAT" ||
               key == "GÖNDER";
    }

    private void HandleKeyboardKey(string key)
    {
        if (key == "KAPAT")
        {
            SetKeyboardVisible(false);
            return;
        }

        if (activeInput == null)
            return;

        switch (key)
        {
            case "SİL":
                BackspaceActiveInput();
                break;

            case "BOŞLUK":
                InsertTextIntoActiveInput(" ");
                break;

            case "TEMİZLE":
                activeInput.text = "";
                MoveCaretToEnd(activeInput);
                break;

            case "GÖNDER":
                HandleSendPressed();
                break;

            default:
                InsertTextIntoActiveInput(ConvertTurkishKeyToInput(key));
                break;
        }

        if (keyboardVisible && activeInput != null)
        {
            ForceInputLeftToRight(activeInput);
            MoveCaretToEnd(activeInput);
        }
    }

    private void InsertTextIntoActiveInput(string value)
    {
        if (activeInput == null || string.IsNullOrEmpty(value))
            return;

        ForceInputLeftToRight(activeInput);

        string current = activeInput.text ?? "";
        activeInput.text = current + value;

        MoveCaretToEnd(activeInput);
        activeInput.ForceLabelUpdate();
    }

    private void BackspaceActiveInput()
    {
        if (activeInput == null)
            return;

        ForceInputLeftToRight(activeInput);

        string current = activeInput.text ?? "";
        if (current.Length == 0)
            return;

        activeInput.text = current.Remove(current.Length - 1, 1);

        MoveCaretToEnd(activeInput);
        activeInput.ForceLabelUpdate();
    }

    private void HandleSendPressed()
    {
        if (hideKeyboardOnSend)
            SetKeyboardVisible(false);

        if (submitButtonForCurrentInput != null)
        {
            submitButtonForCurrentInput.onClick.Invoke();
            return;
        }

        if (defaultSubmitButton != null)
        {
            defaultSubmitButton.onClick.Invoke();
            return;
        }

        onSendPressed?.Invoke();
    }

    private void ForceInputLeftToRight(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        if (inputField.textComponent != null)
            inputField.textComponent.isRightToLeftText = false;

        if (inputField.placeholder is TMP_Text placeholderText)
            placeholderText.isRightToLeftText = false;
    }

    private void MoveCaretToEnd(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        int endPosition = inputField.text != null ? inputField.text.Length : 0;

        inputField.caretPosition = endPosition;
        inputField.stringPosition = endPosition;
        inputField.selectionAnchorPosition = endPosition;
        inputField.selectionFocusPosition = endPosition;
    }

    private string ConvertTurkishKeyToInput(string key)
    {
        switch (key)
        {
            case "I": return "ı";
            case "İ": return "i";
            case "Ğ": return "ğ";
            case "Ü": return "ü";
            case "Ş": return "ş";
            case "Ö": return "ö";
            case "Ç": return "ç";
            default: return key.ToLowerInvariant();
        }
    }

    private Sprite GetRoundedRuntimeSprite()
    {
        if (runtimeRoundedSprite != null)
            return runtimeRoundedSprite;

        const int size = 96;
        const float radius = 24f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "RuntimeRoundedKeyboardSprite";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;

                float qx = Mathf.Abs(px) - (half - radius);
                float qy = Mathf.Abs(py) - (half - radius);

                float outsideX = Mathf.Max(qx, 0f);
                float outsideY = Mathf.Max(qy, 0f);
                float outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
                float insideDistance = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float signedDistance = outsideDistance + insideDistance - radius;

                float alpha = 1f - Mathf.SmoothStep(0f, 2f, signedDistance);
                alpha = Mathf.Clamp01(alpha);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        runtimeRoundedSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(26f, 26f, 26f, 26f)
        );

        return runtimeRoundedSprite;
    }
}