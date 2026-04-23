using UnityEngine;

public class AnswerTextVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager instance not found in AIChat scene.");
            return;
        }

        Debug.Log("AIChat Start - ShowAnswerText = " + SettingsManager.Instance.ShowAnswerText);

        ApplyVisibility(SettingsManager.Instance.ShowAnswerText);
        SettingsManager.Instance.OnShowAnswerTextChanged += ApplyVisibility;
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnShowAnswerTextChanged -= ApplyVisibility;
        }
    }

    private void ApplyVisibility(bool isVisible)
    {
        Debug.Log("Applying visibility: " + isVisible);

        if (targetObject != null)
        {
            targetObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogError("Target object is null.");
        }
    }
}