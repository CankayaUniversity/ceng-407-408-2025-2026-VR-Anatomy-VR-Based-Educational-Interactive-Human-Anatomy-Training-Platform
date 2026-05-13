using System.Collections;
using UnityEngine;

public class AIChatAvatarVisibilityController : MonoBehaviour
{
    private const string AvatarTypeKey = "AvatarType";

    [Header("Scene Avatar Roots")]
    [SerializeField] private GameObject femaleAvatar;
    [SerializeField] private GameObject maleAvatar;

    [Header("TTS Audio Source")]
    [Tooltip("Boş bırakılırsa sahnedeki RagApiClient üzerindeki AudioSource otomatik bulunur.")]
    [SerializeField] private AudioSource ttsAudioSource;

    private ChatAvatarController _activeLipSyncController;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged += ApplyAvatarSelection;
    }

    private IEnumerator Start()
    {
        // RagApiClient Awake içinde AudioSource eklediği için bir frame beklemek güvenli.
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

        savedValue = Mathf.Clamp(
            savedValue,
            (int)SettingsManager.AvatarType.Female,
            (int)SettingsManager.AvatarType.Male
        );

        return (SettingsManager.AvatarType)savedValue;
    }

    private void ApplyAvatarSelection(SettingsManager.AvatarType avatarType)
    {
        if (femaleAvatar == null)
            Debug.LogWarning("[AIChatAvatarVisibilityController] Female avatar reference missing.", this);

        if (maleAvatar == null)
            Debug.LogWarning("[AIChatAvatarVisibilityController] Male avatar reference missing.", this);

        bool showMaleAvatar = avatarType == SettingsManager.AvatarType.Male;

        if (femaleAvatar != null)
            femaleAvatar.SetActive(!showMaleAvatar);

        if (maleAvatar != null)
            maleAvatar.SetActive(showMaleAvatar);

        GameObject activeAvatar = showMaleAvatar ? maleAvatar : femaleAvatar;

        AttachLipSyncController(activeAvatar, showMaleAvatar);
        TryStartIdleAnimation(activeAvatar);

        Debug.Log("[AIChatAvatarVisibilityController] Active avatar: " + avatarType);
    }

    private void AttachLipSyncController(GameObject avatarRoot, bool isMaleAvatar)
    {
        if (avatarRoot == null) return;

        AudioSource speechAudio = ResolveTtsAudioSource();

        if (speechAudio == null)
        {
            Debug.LogWarning("[AIChatAvatarVisibilityController] RagApiClient AudioSource bulunamadı; lip sync bağlanamadı.", this);
            return;
        }

        _activeLipSyncController = avatarRoot.GetComponent<ChatAvatarController>();

        if (_activeLipSyncController == null)
            _activeLipSyncController = avatarRoot.AddComponent<ChatAvatarController>();

        _activeLipSyncController.ConfigureExistingSceneAvatar(speechAudio, isMaleAvatar);
        _activeLipSyncController.SetLipSyncAudioSource(speechAudio);
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

    private void TryStartIdleAnimation(GameObject avatarRoot)
    {
        if (avatarRoot == null) return;

        Animation legacyAnimation = avatarRoot.GetComponentInChildren<Animation>(true);

        if (legacyAnimation != null)
        {
            legacyAnimation.enabled = true;
            legacyAnimation.playAutomatically = true;
            legacyAnimation.wrapMode = WrapMode.Loop;

            if (legacyAnimation.clip != null)
            {
                legacyAnimation.clip.wrapMode = WrapMode.Loop;

                if (!legacyAnimation.isPlaying)
                    legacyAnimation.Play();

                return;
            }

            foreach (AnimationState state in legacyAnimation)
            {
                if (state.clip == null) continue;

                state.wrapMode = WrapMode.Loop;
                legacyAnimation.clip = state.clip;
                legacyAnimation.Play();
                return;
            }
        }

        Animator animator = avatarRoot.GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
        }
    }
}