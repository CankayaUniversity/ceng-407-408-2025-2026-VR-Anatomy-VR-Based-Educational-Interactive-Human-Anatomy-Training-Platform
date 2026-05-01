using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelectionToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle femaleToggle;
    [SerializeField] private Toggle maleToggle;
    [SerializeField] private TMP_Text titleText;

    private bool _isInitialized;
    private bool _isUpdating;

    public void Initialize(Toggle female, Toggle male, TMP_Text title)
    {
        femaleToggle = female;
        maleToggle = male;
        titleText = title;
        SetupIfPossible();
    }

    private void Start()
    {
        SetupIfPossible();
    }

    private void OnDestroy()
    {
        if (femaleToggle != null)
            femaleToggle.onValueChanged.RemoveListener(OnFemaleToggleChanged);

        if (maleToggle != null)
            maleToggle.onValueChanged.RemoveListener(OnMaleToggleChanged);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged -= OnAvatarTypeChanged;
    }

    private void SetupIfPossible()
    {
        if (_isInitialized || femaleToggle == null || maleToggle == null) return;
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager not found.");
            return;
        }

        if (titleText != null)
            titleText.text = "Avatar Seçimi";

        UpdateToggleVisuals(SettingsManager.Instance.SelectedAvatarType);

        femaleToggle.onValueChanged.AddListener(OnFemaleToggleChanged);
        maleToggle.onValueChanged.AddListener(OnMaleToggleChanged);
        SettingsManager.Instance.OnAvatarTypeChanged += OnAvatarTypeChanged;

        _isInitialized = true;
    }

    private void OnFemaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;

        if (isOn)
        {
            SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.Female);
            return;
        }

        KeepOneSelected();
    }

    private void OnMaleToggleChanged(bool isOn)
    {
        if (_isUpdating || SettingsManager.Instance == null) return;

        if (isOn)
        {
            SettingsManager.Instance.SetAvatarType(SettingsManager.AvatarType.Male);
            return;
        }

        KeepOneSelected();
    }

    private void OnAvatarTypeChanged(SettingsManager.AvatarType avatarType)
    {
        UpdateToggleVisuals(avatarType);
    }

    private void UpdateToggleVisuals(SettingsManager.AvatarType avatarType)
    {
        _isUpdating = true;

        bool isMale = avatarType == SettingsManager.AvatarType.Male;
        femaleToggle.SetIsOnWithoutNotify(!isMale);
        maleToggle.SetIsOnWithoutNotify(isMale);

        _isUpdating = false;
    }

    private void KeepOneSelected()
    {
        if (SettingsManager.Instance != null)
            UpdateToggleVisuals(SettingsManager.Instance.SelectedAvatarType);
    }
}
