using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public enum AvatarType
    {
        Female      = 0,
        Male        = 1,
        YoungFemale = 2,
        YoungMale   = 3
    }

    public static SettingsManager Instance;

    private const string ShowAnswerTextKey = "ShowAnswerText";

    public bool ShowAnswerText { get; private set; } = true;

    public event Action<bool> OnShowAnswerTextChanged;

    private const string MasterVolumeKey = "MasterVolume";

    public float MasterVolume { get; private set; } = 1f;

    public event Action<float> OnMasterVolumeChanged;

    private const string AvatarTypeKey = "AvatarType";

    public AvatarType SelectedAvatarType { get; private set; } = AvatarType.Female;

    public event Action<AvatarType> OnAvatarTypeChanged;

    public const string AIChatVoiceEnabledKey = "AIChatVoiceEnabled";

    public bool AIChatVoiceEnabled { get; private set; } = true;

    public event Action<bool> OnAIChatVoiceEnabledChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();

        Debug.Log("SettingsManager Awake - Loaded ShowAnswerText: " + ShowAnswerText);
    }

    private void LoadSettings()
    {
        ShowAnswerText = PlayerPrefs.GetInt(ShowAnswerTextKey, 1) == 1;
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        SelectedAvatarType = (AvatarType)Mathf.Clamp(
            PlayerPrefs.GetInt(AvatarTypeKey, (int)AvatarType.Female),
            (int)AvatarType.Female,
            (int)AvatarType.YoungMale);
        AIChatVoiceEnabled = PlayerPrefs.GetInt(AIChatVoiceEnabledKey, 1) == 1;
        AudioListener.volume = MasterVolume;
    }

    public void SetShowAnswerText(bool value)
    {
        ShowAnswerText = value;

        PlayerPrefs.SetInt(ShowAnswerTextKey, value ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Saved ShowAnswerText = " + ShowAnswerText);

        OnShowAnswerTextChanged?.Invoke(value);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = value;

        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();

        AudioListener.volume = value;

        Debug.Log("MasterVolume set to: " + value);

        OnMasterVolumeChanged?.Invoke(value);
    }

    public void SetAvatarType(AvatarType avatarType)
    {
        SelectedAvatarType = avatarType;

        PlayerPrefs.SetInt(AvatarTypeKey, (int)avatarType);
        PlayerPrefs.Save();

        Debug.Log("AvatarType set to: " + avatarType);

        OnAvatarTypeChanged?.Invoke(avatarType);
    }

    public void SetAIChatVoiceEnabled(bool value)
    {
        AIChatVoiceEnabled = value;

        PlayerPrefs.SetInt(AIChatVoiceEnabledKey, value ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("AIChatVoiceEnabled set to: " + value);

        OnAIChatVoiceEnabledChanged?.Invoke(value);
    }

    public void ResetToDefaults()
    {
        SetShowAnswerText(true);
        SetMasterVolume(1f);
        SetAvatarType(AvatarType.Female); // Varsayılan: Kız
        SetAIChatVoiceEnabled(true);

        Debug.Log("Settings reset to defaults.");
    }
}