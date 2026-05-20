using System.Collections;
using UnityEngine;

public class AIChatAvatarVisibilityController : MonoBehaviour
{
    private const string AvatarTypeKey = "AvatarType";

    [Header("Scene Avatar Roots")]
    [SerializeField] private GameObject femaleAvatar;       // model1
    [SerializeField] private GameObject maleAvatar;         // model2
    [SerializeField] private GameObject youngFemaleAvatar;  // model3
    [SerializeField] private GameObject youngMaleAvatar;    // model4

    [Header("TTS Audio Source")]
    [Tooltip("Boş bırakılırsa sahnedeki RagApiClient üzerindeki AudioSource otomatik bulunur.")]
    [SerializeField] private AudioSource ttsAudioSource;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged += ApplyAvatarSelection;
    }

    private IEnumerator Start()
    {
        yield return null;
        ApplyAvatarSelection(GetSelectedAvatarType());
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged -= ApplyAvatarSelection;
    }

    private SettingsManager.AvatarType GetSelectedAvatarType()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.SelectedAvatarType;

        int savedValue = PlayerPrefs.GetInt(
            AvatarTypeKey,
            (int)SettingsManager.AvatarType.Female
        );

        savedValue = Mathf.Clamp(savedValue, 0, 3);
        return (SettingsManager.AvatarType)savedValue;
    }

    private void ApplyAvatarSelection(SettingsManager.AvatarType avatarType)
    {
        Debug.Log("[AIChatAvatarVisibilityController] Selected avatar: " + avatarType);

        SetAvatarActive(femaleAvatar, avatarType == SettingsManager.AvatarType.Female);
        SetAvatarActive(maleAvatar, avatarType == SettingsManager.AvatarType.Male);
        SetAvatarActive(youngFemaleAvatar, avatarType == SettingsManager.AvatarType.YoungFemale);
        SetAvatarActive(youngMaleAvatar, avatarType == SettingsManager.AvatarType.YoungMale);

        GameObject activeAvatar = GetActiveAvatarObject(avatarType);

        bool isMaleAvatar =
            avatarType == SettingsManager.AvatarType.Male ||
            avatarType == SettingsManager.AvatarType.YoungMale;

        SetupActiveAvatar(activeAvatar, isMaleAvatar);
    }

    private GameObject GetActiveAvatarObject(SettingsManager.AvatarType avatarType)
    {
        switch (avatarType)
        {
            case SettingsManager.AvatarType.Male:
                return maleAvatar;

            case SettingsManager.AvatarType.YoungFemale:
                return youngFemaleAvatar;

            case SettingsManager.AvatarType.YoungMale:
                return youngMaleAvatar;

            case SettingsManager.AvatarType.Female:
            default:
                return femaleAvatar;
        }
    }

    private void SetAvatarActive(GameObject avatar, bool active)
    {
        if (avatar == null)
        {
            return;
        }

        avatar.SetActive(active);

        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = active;
        }

        SkinnedMeshRenderer[] skinnedRenderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            skinnedRenderer.enabled = active;
            skinnedRenderer.updateWhenOffscreen = true;
        }

        Animator animator = avatar.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.enabled = active;
            animator.applyRootMotion = false;
        }

        Animation animation = avatar.GetComponentInChildren<Animation>(true);
        if (animation != null)
        {
            animation.enabled = active;

            if (active)
            {
                animation.wrapMode = WrapMode.Loop;

                if (animation.clip != null)
                {
                    animation.clip.wrapMode = WrapMode.Loop;
                    animation.Play();
                }
                else
                {
                    foreach (AnimationState state in animation)
                    {
                        if (state.clip == null) continue;

                        state.wrapMode = WrapMode.Loop;
                        animation.clip = state.clip;
                        animation.Play();
                        break;
                    }
                }
            }
        }
    }

    private void SetupActiveAvatar(GameObject activeAvatar, bool isMaleAvatar)
    {
        if (activeAvatar == null)
        {
            Debug.LogWarning("[AIChatAvatarVisibilityController] Aktif avatar null. Inspector referanslarını kontrol et.");
            return;
        }

        AudioSource speechAudio = ResolveTtsAudioSource();

        if (speechAudio == null)
        {
            Debug.LogWarning("[AIChatAvatarVisibilityController] TTS AudioSource bulunamadı. Lip sync bağlanamadı.");
            return;
        }

        ChatAvatarController lipSyncController = activeAvatar.GetComponent<ChatAvatarController>();

        if (lipSyncController == null)
            lipSyncController = activeAvatar.AddComponent<ChatAvatarController>();

        lipSyncController.ConfigureExistingSceneAvatar(speechAudio, isMaleAvatar);
        lipSyncController.SetLipSyncAudioSource(speechAudio);

        Debug.Log("[AIChatAvatarVisibilityController] Aktif avatar hazır: " + activeAvatar.name);
    }

    private AudioSource ResolveTtsAudioSource()
    {
        if (ttsAudioSource != null)
            return ttsAudioSource;

        RagApiClient ragApiClient = FindFirstObjectByType<RagApiClient>();

        if (ragApiClient == null)
            ragApiClient = FindAnyObjectByType<RagApiClient>(FindObjectsInactive.Include);

        if (ragApiClient == null)
            return null;

        ttsAudioSource = ragApiClient.GetComponent<AudioSource>();

        if (ttsAudioSource == null)
            ttsAudioSource = ragApiClient.gameObject.AddComponent<AudioSource>();

        return ttsAudioSource;
    }
}