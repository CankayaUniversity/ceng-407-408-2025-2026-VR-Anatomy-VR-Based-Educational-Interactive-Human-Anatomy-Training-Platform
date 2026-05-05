using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIChatVoiceToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle enabledToggle;
    [SerializeField] private Toggle disabledToggle;
    [SerializeField] private TMP_Text titleText;

    private bool _isInitialized;
    private bool _isUpdating;

    public void Initialize(Toggle enabledOption, Toggle disabledOption, TMP_Text title)
    {
        enabledToggle = enabledOption;
        disabledToggle = disabledOption;
        titleText = title;
        SetupIfPossible();
    }

    private void Start()
    {
        SetupIfPossible();
    }

    private void OnDestroy()
    {
        if (enabledToggle != null)
            enabledToggle.onValueChanged.RemoveListener(OnEnabledToggleChanged);

        if (disabledToggle != null)
            disabledToggle.onValueChanged.RemoveListener(OnDisabledToggleChanged);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAIChatVoiceEnabledChanged -= OnAIChatVoiceEnabledChanged;
    }

    private void SetupIfPossible()
    {
        if (_isInitialized || enabledToggle == null || disabledToggle == null) return;
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager bulunamadı.");
            return;
        }

        if (titleText != null)
            titleText.text = "Yapay Zekâ ile Konuş Avatar Sesi";

        UpdateToggleVisuals(SettingsManager.Instance.AIChatVoiceEnabled);

        enabledToggle.onValueChanged.AddListener(OnEnabledToggleChanged);
        disabledToggle.onValueChanged.AddListener(OnDisabledToggleChanged);
        SettingsManager.Instance.OnAIChatVoiceEnabledChanged += OnAIChatVoiceEnabledChanged;

        _isInitialized = true;
    }

    private void OnEnabledToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;

        if (isOn)
        {
            SettingsManager.Instance.SetAIChatVoiceEnabled(true);
            return;
        }

        KeepOneSelected();
    }

    private void OnDisabledToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;

        if (isOn)
        {
            SettingsManager.Instance.SetAIChatVoiceEnabled(false);
            return;
        }

        KeepOneSelected();
    }

    private void OnAIChatVoiceEnabledChanged(bool isEnabled)
    {
        UpdateToggleVisuals(isEnabled);
    }

    private void UpdateToggleVisuals(bool isEnabled)
    {
        _isUpdating = true;

        enabledToggle.SetIsOnWithoutNotify(isEnabled);
        disabledToggle.SetIsOnWithoutNotify(!isEnabled);

        _isUpdating = false;
    }

    private void KeepOneSelected()
    {
        if (SettingsManager.Instance != null)
            UpdateToggleVisuals(SettingsManager.Instance.AIChatVoiceEnabled);
    }
}
