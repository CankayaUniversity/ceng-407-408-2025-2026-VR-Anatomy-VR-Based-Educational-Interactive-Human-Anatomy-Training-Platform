using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SettingsQuickLayoutFix : MonoBehaviour
{
    private const string SettingsSceneName = "07_Settings";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().name == SettingsSceneName)
            CreateRuntimeFixer();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SettingsSceneName) return;
        CreateRuntimeFixer();
    }

    private static void CreateRuntimeFixer()
    {
        GameObject go = new GameObject("SettingsQuickLayoutFix_Runtime");
        go.AddComponent<SettingsQuickLayoutFix>();
    }

    private IEnumerator Start()
    {
        // Diğer settings/avatar scriptleri önce kendi işini bitirsin.
        yield return new WaitForSecondsRealtime(0.25f);

        ApplyLayout();

        Destroy(gameObject);
    }

    private static void ApplyLayout()
    {
        RectTransform panel = FindRect("SettingsPanel");
        if (panel == null) return;

        Place(panel, null, new Vector2(0f, -4f), new Vector2(760f, 395f), new Vector2(0.5f, 0.5f));

        LayoutTitle(panel);
        LayoutShowAnswer(panel);
        LayoutVolume(panel);
        LayoutAvatar(panel);
        LayoutVoice(panel);
        LayoutButtons(panel);
    }

    private static void LayoutTitle(RectTransform panel)
    {
        TMP_Text title = FindTMPExactOrContains("AYARLAR");
        if (title == null) return;

        title.text = "AYARLAR";
        title.fontSize = 38f;
        title.enableAutoSizing = false;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        Place(title.rectTransform, panel, new Vector2(0f, 135f), new Vector2(360f, 55f), new Vector2(0.5f, 0.5f));
    }

    private static void LayoutShowAnswer(RectTransform panel)
    {
        TMP_Text label = FindTMPContains("Yapay Zeka") ??
                         FindTMPContains("Yapay Zekâ") ??
                         FindTMPContains("Metni Göster");

        if (label != null)
        {
            label.text = "Yapay Zekâ ile Konuş -\nMetni Göster";
            StyleLabel(label, 15.5f);
            Place(label.rectTransform, panel, new Vector2(-285f, 88f), new Vector2(260f, 44f), new Vector2(0f, 0.5f));
        }

        Toggle toggle = FindToggleByName("ShowAnswerTextToggle");
        if (toggle == null)
            toggle = FindToggleNearText("Metni Göster");

        if (toggle != null)
            PlaceToggle(toggle, panel, new Vector2(245f, 90f), "", 55f);
    }

    private static void LayoutVolume(RectTransform panel)
    {
        TMP_Text label = FindTMPContains("Ses Seviyesi");
        if (label != null)
        {
            label.text = "Ses Seviyesi";
            StyleLabel(label, 17f);
            Place(label.rectTransform, panel, new Vector2(-285f, 45f), new Vector2(230f, 32f), new Vector2(0f, 0.5f));
        }

        Slider slider = FindObjectInScene<Slider>();
        if (slider != null)
        {
            Place(slider.GetComponent<RectTransform>(), panel, new Vector2(145f, 45f), new Vector2(300f, 24f), new Vector2(0.5f, 0.5f));
        }
    }

    private static void LayoutAvatar(RectTransform panel)
    {
        TMP_Text label = FindTMPContains("Avatar Seçimi");
        if (label != null)
        {
            label.text = "Avatar Seçimi";
            StyleLabel(label, 17.5f);
            Place(label.rectTransform, panel, new Vector2(-285f, -10f), new Vector2(240f, 32f), new Vector2(0f, 0.5f));
        }

        Toggle female = FindToggleByLabel("Kız");
        Toggle male = FindToggleByLabel("Erkek");
        Toggle youngFemale = FindToggleByLabel("Genç Kız");
        Toggle youngMale = FindToggleByLabel("Genç Erkek");

        List<Toggle> extras = GetNonShowAnswerToggles();

        if (female == null && extras.Count > 0) female = extras[0];
        if (male == null && extras.Count > 1) male = extras[1];
        if (youngFemale == null && extras.Count > 2) youngFemale = extras[2];
        if (youngMale == null && extras.Count > 3) youngMale = extras[3];

        if (female != null)
    PlaceToggle(female, panel, new Vector2(45f, -8f), "Kız", 90f);

if (male != null)
    PlaceToggle(male, panel, new Vector2(185f, -8f), "Erkek", 100f);

if (youngFemale != null)
    PlaceToggle(youngFemale, panel, new Vector2(45f, -45f), "Genç Kız", 105f);

if (youngMale != null)
    PlaceToggle(youngMale, panel, new Vector2(185f, -45f), "Genç Erkek", 120f);
    }

    private static void LayoutVoice(RectTransform panel)
    {
        TMP_Text label = FindTMPContains("Avatar Sesi");
        if (label != null)
        {
            label.text = "Avatar Sesi";
            StyleLabel(label, 17.5f);
            Place(label.rectTransform, panel, new Vector2(-285f, -98f), new Vector2(230f, 32f), new Vector2(0f, 0.5f));
        }

        Toggle open = FindToggleByLabel("Açık");
        Toggle closed = FindToggleByLabel("Kapalı");

        List<Toggle> extras = GetNonShowAnswerToggles();

        if (open == null && extras.Count > 4) open = extras[4];
        if (closed == null && extras.Count > 5) closed = extras[5];

        if (open != null)
    PlaceToggle(open, panel, new Vector2(45f, -98f), "Açık", 90f);

if (closed != null)
    PlaceToggle(closed, panel, new Vector2(185f, -98f), "Kapalı", 105f);
    }

    private static void LayoutButtons(RectTransform panel)
    {
        Button back = FindButtonByName("BackButton");
        if (back != null)
        {
            Place(back.GetComponent<RectTransform>(), panel, new Vector2(-175f, -150f), new Vector2(150f, 36f), new Vector2(0.5f, 0.5f));
            StyleButtonText(back, 15f);
        }

        Button reset = FindButtonByName("ResetSettingsButton");
        if (reset != null)
        {
            Place(reset.GetComponent<RectTransform>(), panel, new Vector2(175f, -150f), new Vector2(200f, 36f), new Vector2(0.5f, 0.5f));
            StyleButtonText(reset, 14f);
        }
    }

    private static void PlaceToggle(Toggle toggle, RectTransform parent, Vector2 position, string labelText, float width)
    {
        if (toggle == null) return;

        RectTransform toggleRT = toggle.GetComponent<RectTransform>();
        Place(toggleRT, parent, position, new Vector2(width, 28f), new Vector2(0f, 0.5f));

        RectTransform graphicRT = toggle.graphic != null
            ? toggle.graphic.GetComponent<RectTransform>()
            : null;

        if (graphicRT != null)
        {
            graphicRT.anchorMin = new Vector2(0f, 0.5f);
            graphicRT.anchorMax = new Vector2(0f, 0.5f);
            graphicRT.pivot = new Vector2(0.5f, 0.5f);
            graphicRT.anchoredPosition = new Vector2(8f, 0f);
            graphicRT.sizeDelta = new Vector2(18f, 18f);
            graphicRT.localScale = Vector3.one;
        }

        TMP_Text tmp = toggle.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            if (!string.IsNullOrWhiteSpace(labelText))
                tmp.text = labelText;

            tmp.fontSize = 14.5f;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            Place(tmp.rectTransform, toggleRT, new Vector2(22f, 0f), new Vector2(width - 38f, 28f), new Vector2(0f, 0.5f));
        }

        Text legacy = toggle.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            if (!string.IsNullOrWhiteSpace(labelText))
                legacy.text = labelText;

            legacy.fontSize = 14;
            legacy.alignment = TextAnchor.MiddleLeft;
            legacy.color = Color.white;
            legacy.raycastTarget = false;

            Place(legacy.GetComponent<RectTransform>(), toggleRT, new Vector2(22f, 0f), new Vector2(width - 38f, 28f), new Vector2(0f, 0.5f));
        }
    }

    private static void StyleLabel(TMP_Text text, float fontSize)
    {
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void StyleButtonText(Button button, float fontSize)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.fontSize = Mathf.RoundToInt(fontSize);
            legacy.alignment = TextAnchor.MiddleCenter;
            legacy.raycastTarget = false;
        }
    }

    private static void Place(RectTransform rt, RectTransform parent, Vector2 position, Vector2 size, Vector2 pivot)
    {
        if (rt == null) return;

        if (parent != null && rt.parent != parent)
            rt.SetParent(parent, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localEulerAngles = Vector3.zero;
    }

    private static RectTransform FindRect(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static Button FindButtonByName(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static Toggle FindToggleByName(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Toggle>() : null;
    }

    private static TMP_Text FindTMPExactOrContains(string text)
    {
        TMP_Text exact = null;

        foreach (TMP_Text tmp in FindObjectsInScene<TMP_Text>())
        {
            if (tmp == null) continue;

            string normalized = Normalize(tmp.text);

            if (normalized.Equals(text, StringComparison.OrdinalIgnoreCase))
                return tmp;

            if (exact == null && normalized.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                exact = tmp;
        }

        return exact;
    }

    private static TMP_Text FindTMPContains(string keyword)
    {
        foreach (TMP_Text tmp in FindObjectsInScene<TMP_Text>())
        {
            if (tmp == null) continue;

            string normalized = Normalize(tmp.text);
            if (normalized.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;
        }

        return null;
    }

    private static Toggle FindToggleByLabel(string label)
    {
        foreach (Toggle toggle in FindObjectsInScene<Toggle>())
        {
            if (toggle == null) continue;

            string text = GetToggleLabel(toggle);
            if (Normalize(text).Equals(label, StringComparison.OrdinalIgnoreCase))
                return toggle;
        }

        return null;
    }

    private static Toggle FindToggleNearText(string keyword)
    {
        foreach (Toggle toggle in FindObjectsInScene<Toggle>())
        {
            if (toggle == null) continue;
            if (toggle.name.IndexOf("ShowAnswer", StringComparison.OrdinalIgnoreCase) >= 0)
                return toggle;
        }

        return null;
    }

    private static List<Toggle> GetNonShowAnswerToggles()
    {
        List<Toggle> result = new List<Toggle>();

        foreach (Toggle toggle in FindObjectsInScene<Toggle>())
        {
            if (toggle == null) continue;

            if (toggle.name.IndexOf("ShowAnswer", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            string label = Normalize(GetToggleLabel(toggle));
            if (label.IndexOf("Metni", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            result.Add(toggle);
        }

        return result;
    }

    private static string GetToggleLabel(Toggle toggle)
    {
        TMP_Text tmp = toggle.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) return tmp.text;

        Text legacy = toggle.GetComponentInChildren<Text>(true);
        if (legacy != null) return legacy.text;

        return "";
    }

    private static T FindObjectInScene<T>() where T : Component
    {
        foreach (T item in FindObjectsInScene<T>())
            return item;

        return null;
    }

    private static List<T> FindObjectsInScene<T>() where T : Component
    {
        List<T> result = new List<T>();
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<T>(true));
        }

        return result;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return value.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}