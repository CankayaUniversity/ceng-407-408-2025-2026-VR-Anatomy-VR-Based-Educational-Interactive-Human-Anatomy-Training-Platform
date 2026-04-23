using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    private const string ShowAnswerTextKey = "ShowAnswerText";

    public bool ShowAnswerText { get; private set; } = true;

    public event Action<bool> OnShowAnswerTextChanged;

    private const string MasterVolumeKey = "MasterVolume";

    public float MasterVolume { get; private set; } = 1f;

public event Action<float> OnMasterVolumeChanged;

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

    public void ResetToDefaults()
{
    SetShowAnswerText(true);
    SetMasterVolume(1f);

    Debug.Log("Settings reset to defaults.");
}
}