using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettingsSceneLayoutAdjuster
{
    private const string SettingsSceneName = "07_Settings";
    private const float RowLabelX = -320f;
    private const float RowControlX = 82f;
    private const float OptionOneX = 0f;
    private const float OptionTwoX = 140f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SettingsSceneName) return;
        ApplyLayout();
    }

    public static void ApplyLayout()
    {
        PositionPanel();
        PositionTitle();
        LayoutSingleToggleRow("ShowAnswerTextRow", new Vector2(0f, 76f), "Yapay Zekâ ile Konuş - Metni Göster");
        LayoutSliderRow("VolumeRow", new Vector2(0f, 18f), "                 Ses Seviyesi", 400);
        LayoutFourOptionRow("AvatarSelectionRow", new Vector2(0f, -40f), "Avatar Seçimi",
        "Kadın", "Erkek", "Genç Kız", "Genç Erkek");
        LayoutTwoOptionRow("AIChatVoiceRow", new Vector2(0f, -104f), "Yapay Zekâ ile Konuş - Avatar Sesi", "Açık", "Kapalı", true);
        LayoutBottomButtons();
    }

    private static void PositionPanel()
    {
        GameObject panel = GameObject.Find("SettingsPanel");
        if (panel == null) return;

        RectTransform panelRT = panel.GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchoredPosition = new Vector2(0f, -14f);
            panelRT.sizeDelta = new Vector2(720f, 342f);
        }
    }

    private static void PositionTitle()
    {
        GameObject title = GameObject.Find("Title_Settings");
        if (title == null) return;

        RectTransform titleRT = title.GetComponent<RectTransform>();
        if (titleRT != null)
        {
            titleRT.anchoredPosition = new Vector2(0f, 204f);
            titleRT.sizeDelta = new Vector2(280f, 44f);
        }

        TMP_Text titleText = title.GetComponent<TMP_Text>();
        if (titleText != null)
        {
            titleText.fontSize = 32f;
            titleText.enableAutoSizing = false;
            titleText.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void LayoutSingleToggleRow(string rowName, Vector2 anchoredPosition, string labelText)
    {
        GameObject row = PrepareRow(rowName, anchoredPosition);
        if (row == null) return;

        ConfigureRowLabel(row, labelText, false);

        Toggle toggle = row.GetComponentInChildren<Toggle>(true);
        if (toggle != null)
            ConfigureToggle(toggle, new Vector2(RowControlX + 170f, 0f), "", 30f);
    }

    private static void LayoutSliderRow(string rowName, Vector2 anchoredPosition, string labelText, float labelOffsetX)
    {
        GameObject row = PrepareRow(rowName, anchoredPosition);
        if (row == null) return;

        ConfigureRowLabel(row, labelText, false, labelOffsetX);

        Slider slider = row.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            RectTransform sliderRT = slider.GetComponent<RectTransform>();
            if (sliderRT != null)
            {
                sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
                sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
                sliderRT.pivot = new Vector2(0.5f, 0.5f);
                sliderRT.anchoredPosition = new Vector2(RowControlX + 92f, 0f);
                sliderRT.sizeDelta = new Vector2(250f, 20f);
                sliderRT.localScale = Vector3.one;
            }
        }
    }

    private static void LayoutTwoOptionRow(string rowName, Vector2 anchoredPosition, string labelText,
        string firstOptionText, string secondOptionText, bool compactLabel)
    {
        GameObject row = PrepareRow(rowName, anchoredPosition);
        if (row == null) return;

        ConfigureRowLabel(row, labelText, compactLabel);

        Toggle[] toggles = row.GetComponentsInChildren<Toggle>(true);
        if (toggles.Length > 0)
            ConfigureToggle(toggles[0], new Vector2(RowControlX + OptionOneX, 0f), firstOptionText, 120f);
        if (toggles.Length > 1)
            ConfigureToggle(toggles[1], new Vector2(RowControlX + OptionTwoX, 0f), secondOptionText, 130f);
    }
   private static void LayoutFourOptionRow(
    string rowName,
    Vector2 anchoredPosition,
    string labelText,
    string firstOptionText,
    string secondOptionText,
    string thirdOptionText,
    string fourthOptionText)
{
    GameObject row = PrepareRow(rowName, anchoredPosition);
    if (row == null) return;

    ConfigureRowLabel(row, labelText, false);

    Toggle[] toggles = EnsureToggleCount(row, 4);

    if (toggles.Length > 0)
    ConfigureToggle(toggles[0], new Vector2(RowControlX - 60f, 0f), firstOptionText, 85f);

if (toggles.Length > 1)
    ConfigureToggle(toggles[1], new Vector2(RowControlX + 20f, 0f), secondOptionText, 85f);

if (toggles.Length > 2)
    ConfigureToggle(toggles[2], new Vector2(RowControlX + 95f, 0f), thirdOptionText, 105f);

if (toggles.Length > 3)
    ConfigureToggle(toggles[3], new Vector2(RowControlX + 185f, 0f), fourthOptionText, 115f);
}

private static Toggle[] EnsureToggleCount(GameObject row, int requiredCount)
{
    Toggle[] toggles = row.GetComponentsInChildren<Toggle>(true);

    if (toggles.Length == 0)
        return toggles;

    Toggle templateToggle = toggles[toggles.Length - 1];
    Transform parent = templateToggle.transform.parent;

    ToggleGroup toggleGroup = row.GetComponentInChildren<ToggleGroup>(true);

    for (int i = toggles.Length; i < requiredCount; i++)
    {
        GameObject clone = Object.Instantiate(templateToggle.gameObject, parent);
        clone.name = i == 2 ? "YoungFemaleAvatarToggle" : "YoungMaleAvatarToggle";

        Toggle cloneToggle = clone.GetComponent<Toggle>();
        if (cloneToggle != null)
        {
            cloneToggle.isOn = false;

            if (toggleGroup != null)
                cloneToggle.group = toggleGroup;
        }
    }

    return row.GetComponentsInChildren<Toggle>(true);
}

    private static GameObject PrepareRow(string rowName, Vector2 anchoredPosition, float rowHeight = 54f)
{
    GameObject row = GameObject.Find(rowName);
    if (row == null) return null;

    RectTransform rowRT = row.GetComponent<RectTransform>();
    if (rowRT != null)
    {
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = anchoredPosition;
        rowRT.sizeDelta = new Vector2(760, rowHeight);
    }

    return row;
}

    private static void ConfigureRowLabel(GameObject row, string labelText, bool compact, float offsetX = 0f)
    {
        TMP_Text label = FindPrimaryTmpLabel(row);
        if (label == null) return;

        RectTransform labelRT = label.GetComponent<RectTransform>();
        RectTransform labelHostRT = label.transform.parent != null && label.transform.parent != row.transform
            ? label.transform.parent as RectTransform
            : labelRT;

        if (labelHostRT != null)
        {
            labelHostRT.anchorMin = new Vector2(0.5f, 0.5f);
            labelHostRT.anchorMax = new Vector2(0.5f, 0.5f);
            labelHostRT.pivot = new Vector2(0f, 0.5f);
            labelHostRT.anchoredPosition = new Vector2(RowLabelX + offsetX, 0f);
            labelHostRT.sizeDelta = compact ? new Vector2(380f, 38f) : new Vector2(380f, 38f);
            labelHostRT.localScale = Vector3.one;
        }

        if (labelRT != null)
        {
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.localScale = Vector3.one;
        }

        label.text = labelText;
        label.fontSize = compact ? 17f : 18f;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.lineSpacing = 0f;

// Bu label sadece yazı; tıklamayı/tutmayı engellemesin.
label.raycastTarget = false;
    }

    private static TMP_Text FindPrimaryTmpLabel(GameObject row)
    {
        TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>(true);
        return labels != null && labels.Length > 0 ? labels[0] : null;
    }

    private static void ConfigureToggle(Toggle toggle, Vector2 anchoredPosition, string optionText, float width)
{
    RectTransform toggleRT = toggle.GetComponent<RectTransform>();
    if (toggleRT != null)
    {
        toggleRT.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRT.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRT.pivot = new Vector2(0f, 0.5f);
        toggleRT.anchoredPosition = anchoredPosition;
        toggleRT.sizeDelta = new Vector2(width, 28f);
        toggleRT.localScale = Vector3.one;
    }

    // Checkbox kutusu
    RectTransform graphicRT = toggle.graphic != null ? toggle.graphic.GetComponent<RectTransform>() : null;
    if (graphicRT != null)
    {
        graphicRT.anchorMin = new Vector2(0f, 0.5f);
        graphicRT.anchorMax = new Vector2(0f, 0.5f);
        graphicRT.pivot = new Vector2(0.5f, 0.5f);
        graphicRT.anchoredPosition = new Vector2(10f, 0f);
        graphicRT.sizeDelta = new Vector2(18f, 18f);
        graphicRT.localScale = Vector3.one;
    }

    // Yazı
    Text legacyLabel = toggle.GetComponentInChildren<Text>(true);
    if (legacyLabel != null)
    {
        legacyLabel.text = optionText;
        legacyLabel.fontSize = 14;
        legacyLabel.color = Color.white;
        legacyLabel.alignment = TextAnchor.MiddleLeft;
        legacyLabel.raycastTarget = false;

        RectTransform labelRT = legacyLabel.GetComponent<RectTransform>();
        if (labelRT != null)
        {
            labelRT.anchorMin = new Vector2(0f, 0.5f);
            labelRT.anchorMax = new Vector2(0f, 0.5f);
            labelRT.pivot = new Vector2(0f, 0.5f);
            labelRT.anchoredPosition = new Vector2(24f, 0f);
            labelRT.sizeDelta = new Vector2(width - 24f, 22f);
            labelRT.localScale = Vector3.one;
        }
    }
}

    private static void LayoutBottomButtons()
    {
        ConfigureButton("BackButton", new Vector2(-290f, -206f), new Vector2(150f, 34f));
        ConfigureButton("ResetSettingsButton", new Vector2(290f, -206f), new Vector2(190f, 34f));
    }

    private static void ConfigureButton(string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject button = GameObject.Find(name);
        if (button == null) return;

        RectTransform buttonRT = button.GetComponent<RectTransform>();
        if (buttonRT != null)
        {
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);
            buttonRT.anchoredPosition = anchoredPosition;
            buttonRT.sizeDelta = size;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.fontSize = 16f;
            tmpText.enableAutoSizing = false;
            tmpText.alignment = TextAlignmentOptions.Center;
        }
    }
}
