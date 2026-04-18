using UnityEngine;
using UnityEngine.UI;

public class ShowAnswerTextToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle showAnswerTextToggle;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager instance not found in scene.");
            return;
        }

        if (showAnswerTextToggle == null)
        {
            Debug.LogError("Show Answer Text Toggle is not assigned.");
            return;
        }

        showAnswerTextToggle.isOn = SettingsManager.Instance.ShowAnswerText;
        showAnswerTextToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (showAnswerTextToggle != null)
        {
            showAnswerTextToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool value)
    {
        SettingsManager.Instance.SetShowAnswerText(value);
    }
}