using UnityEngine;

public class AvatarSelection : MonoBehaviour
{
    [Header("Scene Avatar GameObjects")]
    [SerializeField] private GameObject femaleAvatar;       // ID: 0
    [SerializeField] private GameObject maleAvatar;         // ID: 1
    [SerializeField] private GameObject youngFemaleAvatar;  // ID: 2
    [SerializeField] private GameObject youngMaleAvatar;    // ID: 3

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnAvatarTypeChanged += UpdateActiveAvatar;
        }
    }

    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            UpdateActiveAvatar(SettingsManager.Instance.SelectedAvatarType);
        }
        else
        {
            LoadFromPlayerPrefsDirectly();
        }
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnAvatarTypeChanged -= UpdateActiveAvatar;
        }
    }

    private void UpdateActiveAvatar(SettingsManager.AvatarType selectedType)
    {
        Debug.Log($"[AvatarVisibility] Pure toggle applied. Setting active model to: {selectedType}");

        // 1. Structural GameObject adjustments
        if (femaleAvatar != null) femaleAvatar.SetActive(selectedType == SettingsManager.AvatarType.Female);
        if (maleAvatar != null) maleAvatar.SetActive(selectedType == SettingsManager.AvatarType.Male);
        if (youngFemaleAvatar != null) youngFemaleAvatar.SetActive(selectedType == SettingsManager.AvatarType.YoungFemale);
        if (youngMaleAvatar != null) youngMaleAvatar.SetActive(selectedType == SettingsManager.AvatarType.YoungMale);

        // 2. Automated AI Voice Synthesis Alignment
        if (TTSClient.Instance != null)
        {
            // If the selected avatar is Male or YoungMale, UseFemaleVoice becomes false. Otherwise, true!
            bool isFemale = (selectedType == SettingsManager.AvatarType.Female || selectedType == SettingsManager.AvatarType.YoungFemale);
            TTSClient.Instance.UseFemaleVoice = isFemale;

            Debug.Log($"[AvatarVisibility] Automatically aligned TTSClient voice engine setting. Female voice active: {isFemale}");
        }
    }

    private void LoadFromPlayerPrefsDirectly()
    {
        int savedTypeIndex = PlayerPrefs.GetInt("AvatarType", 0);
        savedTypeIndex = Mathf.Clamp(savedTypeIndex, 0, 3);
        UpdateActiveAvatar((SettingsManager.AvatarType)savedTypeIndex);
    }
}