using UnityEngine;

public class AnswerTextVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager instance not found.");
            return;
        }

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
        if (targetObject != null)
        {
            targetObject.SetActive(isVisible);
        }
    }
}