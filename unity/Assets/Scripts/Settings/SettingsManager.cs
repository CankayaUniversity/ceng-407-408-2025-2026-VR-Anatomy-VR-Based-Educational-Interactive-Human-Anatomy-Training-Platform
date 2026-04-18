using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    private const string ShowAnswerTextKey = "ShowAnswerText";

    public bool ShowAnswerText { get; private set; } = true;

    public event Action<bool> OnShowAnswerTextChanged;

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
    }

    private void LoadSettings()
    {
        ShowAnswerText = PlayerPrefs.GetInt(ShowAnswerTextKey, 1) == 1;
    }

    public void SetShowAnswerText(bool value)
    {
        ShowAnswerText = value;

        PlayerPrefs.SetInt(ShowAnswerTextKey, value ? 1 : 0);
        PlayerPrefs.Save();

        OnShowAnswerTextChanged?.Invoke(value);
    }
}