using UnityEngine;
using UnityEngine.UI;

public class ToggleBackgroundColorChanger : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image backgroundImage;

    [SerializeField] private Color onColor = Color.green;
    [SerializeField] private Color offColor = Color.white;

    private void Start()
    {
        if (toggle == null)
        {
            Debug.LogError("Toggle is not assigned.");
            return;
        }

        if (backgroundImage == null)
        {
            Debug.LogError("Background Image is not assigned.");
            return;
        }

        toggle.onValueChanged.AddListener(UpdateBackgroundColor);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnShowAnswerTextChanged += UpdateBackgroundColor;
        }

        UpdateBackgroundColor(toggle.isOn);
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(UpdateBackgroundColor);
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnShowAnswerTextChanged -= UpdateBackgroundColor;
        }
    }

    private void UpdateBackgroundColor(bool isOn)
    {
        backgroundImage.color = isOn ? onColor : offColor;
    }
}