using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Avatar seçimi toggle'larını SettingsManager ile bağlar.
/// 4 tip: Female (0), Male (1), YoungFemale (2), YoungMale (3)
/// </summary>
public class AvatarSelectionToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle femaleToggle;
    [SerializeField] private Toggle maleToggle;
    [SerializeField] private Toggle youngFemaleToggle;
    [SerializeField] private Toggle youngMaleToggle;
    [SerializeField] private TMP_Text titleText;

    private bool _isInitialized;
    private bool _isUpdating;

    public void Initialize(Toggle female, Toggle male, Toggle youngFemale, Toggle youngMale, TMP_Text title)
    {
        femaleToggle      = female;
        maleToggle        = male;
        youngFemaleToggle = youngFemale;
        youngMaleToggle   = youngMale;
        titleText         = title;
        SetupIfPossible();
    }

    private void Start()
    {
        SetupIfPossible();
    }

    private void OnDestroy()
    {
        if (femaleToggle != null)      femaleToggle.onValueChanged.RemoveListener(OnFemaleToggleChanged);
        if (maleToggle != null)        maleToggle.onValueChanged.RemoveListener(OnMaleToggleChanged);
        if (youngFemaleToggle != null) youngFemaleToggle.onValueChanged.RemoveListener(OnYoungFemaleToggleChanged);
        if (youngMaleToggle != null)   youngMaleToggle.onValueChanged.RemoveListener(OnYoungMaleToggleChanged);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged -= OnAvatarTypeChanged;
    }

    private void SetupIfPossible()
    {
        if (_isInitialized || femaleToggle == null || maleToggle == null) return;
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[AvatarSelectionToggleBinder] SettingsManager not found.");
            return;
        }

        if (titleText != null)
            titleText.text = "Avatar Seçimi";

        UpdateToggleVisuals(SettingsManager.Instance.SelectedAvatarType);

        femaleToggle.onValueChanged.AddListener(OnFemaleToggleChanged);
        maleToggle.onValueChanged.AddListener(OnMaleToggleChanged);

        if (youngFemaleToggle != null)
            youngFemaleToggle.onValueChanged.AddListener(OnYoungFemaleToggleChanged);

        if (youngMaleToggle != null)
            youngMaleToggle.onValueChanged.AddListener(OnYoungMaleToggleChanged);

        SettingsManager.Instance.OnAvatarTypeChanged += OnAvatarTypeChanged;

        _isInitialized = true;
    }

    // ──────────────────────────── Toggle Handlers ────────────────────────────

    private void OnFemaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;
        if (isOn) SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.Female);
        else      KeepOneSelected();
    }

    private void OnMaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;
        if (isOn) SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.Male);
        else      KeepOneSelected();
    }

    private void OnYoungFemaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;
        if (isOn) SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.YoungFemale);
        else      KeepOneSelected();
    }

    private void OnYoungMaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;
        if (isOn) SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.YoungMale);
        else      KeepOneSelected();
    }

    private void OnAvatarTypeChanged(SettingsManager.AvatarType avatarType)
    {
        UpdateToggleVisuals(avatarType);
    }

    // ──────────────────────────── Visual Sync ────────────────────────────────

    private void UpdateToggleVisuals(SettingsManager.AvatarType avatarType)
    {
        _isUpdating = true;

        if (femaleToggle != null)
            femaleToggle.SetIsOnWithoutNotify(avatarType == SettingsManager.AvatarType.Female);
        if (maleToggle != null)
            maleToggle.SetIsOnWithoutNotify(avatarType == SettingsManager.AvatarType.Male);
        if (youngFemaleToggle != null)
            youngFemaleToggle.SetIsOnWithoutNotify(avatarType == SettingsManager.AvatarType.YoungFemale);
        if (youngMaleToggle != null)
            youngMaleToggle.SetIsOnWithoutNotify(avatarType == SettingsManager.AvatarType.YoungMale);

        _isUpdating = false;
    }

    private void KeepOneSelected()
    {
        if (SettingsManager.Instance != null)
            UpdateToggleVisuals(SettingsManager.Instance.SelectedAvatarType);
    }
}
