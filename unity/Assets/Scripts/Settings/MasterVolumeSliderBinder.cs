using UnityEngine;
using UnityEngine.UI;

public class MasterVolumeSliderBinder : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager not found.");
            return;
        }

        volumeSlider.value = SettingsManager.Instance.MasterVolume;
        volumeSlider.onValueChanged.AddListener(OnSliderChanged);

        SettingsManager.Instance.OnMasterVolumeChanged += UpdateVisual;
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnMasterVolumeChanged -= UpdateVisual;
    }

    private void OnSliderChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
    }

    private void UpdateVisual(float value)
    {
        volumeSlider.SetValueWithoutNotify(value);
    }

    
}