using UnityEngine;

public class ResetSettingsButtonBinder : MonoBehaviour
{
    public void OnResetSettingsClicked()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager instance not found.");
            return;
        }

        SettingsManager.Instance.ResetToDefaults();
    }
}