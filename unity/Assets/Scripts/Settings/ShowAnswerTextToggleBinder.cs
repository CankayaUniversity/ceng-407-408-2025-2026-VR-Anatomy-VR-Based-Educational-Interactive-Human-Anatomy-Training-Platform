using UnityEngine;
using UnityEngine.UI;

public class ShowAnswerTextToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle showAnswerTextToggle;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager not found.");
            return;
        }

        // başlangıç değeri
        showAnswerTextToggle.isOn = SettingsManager.Instance.ShowAnswerText;

        // toggle değişince manager’a yaz
        showAnswerTextToggle.onValueChanged.AddListener(OnToggleChanged);

        // 🔥 BURASI ÖNEMLİ
        SettingsManager.Instance.OnShowAnswerTextChanged += UpdateToggleVisual;
    }

    private void OnDestroy()
    {
        if (showAnswerTextToggle != null)
            showAnswerTextToggle.onValueChanged.RemoveListener(OnToggleChanged);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnShowAnswerTextChanged -= UpdateToggleVisual;
    }

    private void OnToggleChanged(bool value)
    {
        SettingsManager.Instance.SetShowAnswerText(value);
    }

    // 🔥 RESET SONRASI UI GÜNCELLEYEN KISIM
    private void UpdateToggleVisual(bool value)
    {
        showAnswerTextToggle.SetIsOnWithoutNotify(value);
    }
}