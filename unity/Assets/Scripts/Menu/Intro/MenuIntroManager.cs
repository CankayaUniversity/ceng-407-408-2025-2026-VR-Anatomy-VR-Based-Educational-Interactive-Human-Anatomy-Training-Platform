using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fully self-configuring menu intro system.
/// Auto-bootstraps when 01_Menu loads — zero Inspector wiring needed.
/// Always rebuilds steps and reloads audio from Resources at runtime
/// to avoid stale serialized references.
/// </summary>
public class MenuIntroManager : MonoBehaviour
{
    [Serializable]
    public class IntroStep
    {
        public string stepName;
        public MenuItemHighlighter menuItem;
        public AudioClip voiceClip;
        public float postDelay = 0.4f;
    }

    [SerializeField] private float postWelcomeDelay = 0.5f;

    private AudioClip _welcomeClip;
    private IntroStep[] _steps;
    private Button _skipButton;
    private CanvasGroup _menuCanvasGroup;
    private AudioSource _voiceSource;
    private Coroutine _introCoroutine;
    private bool _introRunning;
    private GameObject _skipButtonGO;

    // =====================================================================
    // Auto-Bootstrap
    // =====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Contains("Menu")) return;

        var existing = FindAnyObjectByType<MenuIntroManager>();
        if (existing != null)
        {
            Destroy(existing.gameObject);
            Debug.Log("[MenuIntro] Eski instance silindi, yenisi oluşturuluyor.");
        }

        var go = new GameObject("MenuIntroManager");
        go.AddComponent<MenuIntroManager>();
    }

    // =====================================================================
    // Lookup tables
    // =====================================================================

    private static readonly string[] ButtonNames =
    {
        "ÖğrenmeyeBaşla", "Test", "SerbestInceleme",
        "YapayZeka", "Ayarlar", "Hakkında"
    };

    private static readonly string[] StepLabels =
    {
        "Öğrenmeye Başla", "Test / Quiz", "Serbest İnceleme",
        "Yapay Zeka ile Konuş", "Ayarlar", "Hakkında"
    };

    private const string WelcomeAudioPath = "Audio/Intro/intro_00_hosgeldiniz";

    private static readonly string[] StepAudioPaths =
    {
        "Audio/Intro/intro_01_ogrenmeye_basla",
        "Audio/Intro/intro_02_test_quiz",
        "Audio/Intro/intro_03_serbest_inceleme",
        "Audio/Intro/intro_04_yapay_zeka",
        "Audio/Intro/intro_05_ayarlar",
        "Audio/Intro/intro_06_hakkinda"
    };

    // =====================================================================
    // Lifecycle
    // =====================================================================

    private IEnumerator Start()
    {
        yield return null;
        Configure();
        BeginIntro();
    }

    private void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(SkipIntro);
        if (_skipButtonGO != null)
            Destroy(_skipButtonGO);
    }

    // =====================================================================
    // Configuration — always rebuilds from scratch
    // =====================================================================

    private void Configure()
    {
        _voiceSource = gameObject.AddComponent<AudioSource>();
        _voiceSource.spatialBlend = 0f;
        _voiceSource.playOnAwake = false;

        Canvas canvas = FindMenuCanvas();
        Transform mmp = canvas != null
            ? FindChildRecursive(canvas.transform, "MainMenuPanel")
            : null;

        if (mmp == null)
        {
            Debug.LogError("[MenuIntro] MainMenuPanel bulunamadı!");
            return;
        }

        // CanvasGroup on MainMenuPanel
        _menuCanvasGroup = mmp.GetComponent<CanvasGroup>();
        if (_menuCanvasGroup == null)
            _menuCanvasGroup = mmp.gameObject.AddComponent<CanvasGroup>();

        // Welcome clip
        _welcomeClip = Resources.Load<AudioClip>(WelcomeAudioPath);
        if (_welcomeClip == null)
            Debug.LogWarning($"[MenuIntro] Welcome clip yüklenemedi: {WelcomeAudioPath}");
        else
            Debug.Log($"[MenuIntro] Welcome clip OK: {_welcomeClip.length:F1}s");

        // Steps — always built fresh
        var list = new List<IntroStep>();
        for (int i = 0; i < ButtonNames.Length; i++)
        {
            var btnT = FindChildRecursive(mmp, ButtonNames[i]);
            if (btnT == null)
            {
                Debug.LogWarning($"[MenuIntro] Buton bulunamadı: {ButtonNames[i]}");
                continue;
            }

            var hl = btnT.GetComponent<MenuItemHighlighter>();
            if (hl == null)
                hl = btnT.gameObject.AddComponent<MenuItemHighlighter>();

            AudioClip clip = Resources.Load<AudioClip>(StepAudioPaths[i]);
            if (clip != null)
                Debug.Log($"[MenuIntro] Ses OK: {StepLabels[i]} -> {clip.length:F1}s");
            else
                Debug.LogWarning($"[MenuIntro] Ses yüklenemedi: {StepAudioPaths[i]}");

            list.Add(new IntroStep
            {
                stepName = StepLabels[i],
                menuItem = hl,
                voiceClip = clip,
                postDelay = 0.5f
            });
        }
        _steps = list.ToArray();
        Debug.Log($"[MenuIntro] {_steps.Length} adım yapılandırıldı.");

        // Skip button — child of MainMenuPanel, ignores parent CanvasGroup
        _skipButtonGO = CreateSkipButton(mmp);
        _skipButton = _skipButtonGO.GetComponent<Button>();
        _skipButton.onClick.AddListener(SkipIntro);
    }

    // =====================================================================
    // Skip Button
    // =====================================================================

    private GameObject CreateSkipButton(Transform parent)
    {
        var go = new GameObject("IntroSkipButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        // CanvasGroup so it stays clickable when parent is non-interactable
        var cg = go.AddComponent<CanvasGroup>();
        cg.ignoreParentGroups = true;

        var rect = go.GetComponent<RectTransform>();
        rect.localScale = new Vector3(0.6f, 0.43f, 1f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -105f);
        rect.sizeDelta = new Vector2(130f, 45f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f, 0.85f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.85f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        textGO.layer = go.layer;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Geç >>";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    // =====================================================================
    // Intro Control
    // =====================================================================

    private void BeginIntro()
    {
        if (_introRunning || _steps == null || _steps.Length == 0) return;

        SetMenuInteractable(false);
        ShowSkipButton(true);
        _introCoroutine = StartCoroutine(IntroSequence());
    }

    public void SkipIntro()
    {
        if (!_introRunning) return;
        CleanUp();
    }

    // =====================================================================
    // Core Sequence
    // =====================================================================

    private IEnumerator IntroSequence()
    {
        _introRunning = true;

        if (_welcomeClip != null)
        {
            PlayClip(_welcomeClip);
            yield return new WaitForSeconds(_welcomeClip.length + 0.1f);
            yield return new WaitForSeconds(postWelcomeDelay);
        }

        for (int i = 0; i < _steps.Length; i++)
        {
            var step = _steps[i];
            if (step.menuItem == null) continue;

            DimAllExcept(i);
            step.menuItem.Highlight();

            if (step.voiceClip != null)
            {
                PlayClip(step.voiceClip);
                yield return new WaitForSeconds(step.voiceClip.length + 0.1f);
            }

            yield return new WaitForSeconds(step.postDelay);
            step.menuItem.Unhighlight();
        }

        Finish();
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private void PlayClip(AudioClip clip)
    {
        _voiceSource.Stop();
        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    private void DimAllExcept(int activeIndex)
    {
        for (int i = 0; i < _steps.Length; i++)
        {
            if (i == activeIndex || _steps[i].menuItem == null) continue;
            _steps[i].menuItem.SetDimmed();
        }
    }

    private void ResetAllHighlighters()
    {
        if (_steps == null) return;
        foreach (var step in _steps)
            if (step.menuItem != null)
                step.menuItem.ResetToNormal();
    }

    private void SetMenuInteractable(bool value)
    {
        if (_menuCanvasGroup == null) return;
        _menuCanvasGroup.interactable = value;
    }

    private void ShowSkipButton(bool show)
    {
        if (_skipButtonGO != null)
            _skipButtonGO.SetActive(show);
    }

    private void Finish()
    {
        _introRunning = false;
        _introCoroutine = null;
        ResetAllHighlighters();
        SetMenuInteractable(true);
        ShowSkipButton(false);
    }

    private void CleanUp()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        _voiceSource.Stop();
        _introRunning = false;
        ResetAllHighlighters();
        SetMenuInteractable(true);
        ShowSkipButton(false);
    }

    private static Canvas FindMenuCanvas()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.gameObject.name.Contains("AnatomyLabMenu"))
                return c;
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
