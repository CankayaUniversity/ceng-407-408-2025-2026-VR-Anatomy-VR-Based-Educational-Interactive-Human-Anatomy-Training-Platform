using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class StartLearningAvatarVisibilityController : MonoBehaviour
{
    private const string AvatarTypeKey = "AvatarType";
    private const float AnimationLookupTimeoutSeconds = 8f;
    private const float AnimationLookupRetryInterval = 0.1f;
    private const float MinAvatarClipDurationSeconds = 0.4f;

    private static readonly string[] UIClipNameKeywords =
    {
        "open", "close", "toggle", "fade", "bounce",
        "rotation", "panel", "menu", "anchor", "preview", "scale"
    };

    [Header("Scene Avatar Roots")]
    [SerializeField] private GameObject femaleAvatar;
    [SerializeField] private GameObject maleAvatar;

    [Header("Optional Clip Overrides")]
    [Tooltip("Otomatik bulunan idle clip yanlışsa, ilgili avatar için doğru clip'i buraya sürükle.")]
    [SerializeField] private AnimationClip femaleIdleClip;
    [SerializeField] private AnimationClip maleIdleClip;

    private PlayableGraph _idleGraph;
    private AnimationClipPlayable _idlePlayable;
    private float _idleClipLength;
    private Coroutine _animationLookupRoutine;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged += ApplyAvatarSelection;

        ApplyAvatarSelection(GetSelectedAvatarType());
    }

    private void Start()
    {
        ApplyAvatarSelection(GetSelectedAvatarType());
    }

    private void Update()
    {
        if (!_idleGraph.IsValid() || !_idlePlayable.IsValid() || _idleClipLength <= 0f) return;

        double wrappedTime = _idlePlayable.GetTime() % _idleClipLength;
        _idlePlayable.SetTime(wrappedTime);
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAvatarTypeChanged -= ApplyAvatarSelection;

        StopAnimationLookup();
        StopPlayableIdle();
    }

    private SettingsManager.AvatarType GetSelectedAvatarType()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.SelectedAvatarType;

        int savedValue = PlayerPrefs.GetInt(AvatarTypeKey, (int)SettingsManager.AvatarType.Female);
        savedValue = Mathf.Clamp(
            savedValue,
            (int)SettingsManager.AvatarType.Female,
            (int)SettingsManager.AvatarType.Male);

        return (SettingsManager.AvatarType)savedValue;
    }

    private void ApplyAvatarSelection(SettingsManager.AvatarType avatarType)
    {
        if (femaleAvatar == null)
            Debug.LogWarning("[StartLearningAvatarVisibilityController] Female avatar reference is missing.", this);

        if (maleAvatar == null)
            Debug.LogWarning("[StartLearningAvatarVisibilityController] Male avatar reference is missing.", this);

        bool showMaleAvatar = avatarType == SettingsManager.AvatarType.Male;

        if (femaleAvatar != null)
            femaleAvatar.SetActive(!showMaleAvatar);

        if (maleAvatar != null)
            maleAvatar.SetActive(showMaleAvatar);

        GameObject activeAvatar = showMaleAvatar ? maleAvatar : femaleAvatar;
        AnimationClip overrideClip = showMaleAvatar ? maleIdleClip : femaleIdleClip;

        StartAnimationLookup(activeAvatar, overrideClip, showMaleAvatar);
    }

    private void StartAnimationLookup(GameObject avatarRoot, AnimationClip overrideClip, bool isMaleAvatar)
    {
        StopAnimationLookup();
        StopPlayableIdle();

        if (avatarRoot == null) return;
        if (!isActiveAndEnabled) return;

        _animationLookupRoutine = StartCoroutine(EnsureAvatarAnimationPlays(avatarRoot, overrideClip, isMaleAvatar));
    }

    private IEnumerator EnsureAvatarAnimationPlays(GameObject avatarRoot, AnimationClip overrideClip, bool isMaleAvatar)
    {
        float elapsed = 0f;

        while (elapsed < AnimationLookupTimeoutSeconds)
        {
            if (avatarRoot == null) yield break;

            if (TryStartAvatarAnimation(avatarRoot, overrideClip, isMaleAvatar))
            {
                _animationLookupRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(AnimationLookupRetryInterval);
            elapsed += AnimationLookupRetryInterval;
        }

        Debug.LogWarning(
            "[StartLearningAvatarVisibilityController] Avatar için oynatılabilir bir animasyon bulunamadı. " +
            "Idle clip'i Inspector'daki override alanına sürükleyebilirsin.",
            this);

        _animationLookupRoutine = null;
    }

    private bool TryStartAvatarAnimation(GameObject avatarRoot, AnimationClip overrideClip, bool isMaleAvatar)
    {
        if (TryPlayLegacyAnimation(avatarRoot))
            return true;

        Animator animator = avatarRoot.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return false;

        animator.enabled = true;
        animator.applyRootMotion = false;

        if (animator.runtimeAnimatorController != null)
            return true;

        AnimationClip clip = overrideClip != null
            ? overrideClip
            : ResolveIdleClip(avatarRoot, isMaleAvatar);

        if (clip == null)
            return false;

        return PlayClipViaPlayables(animator, clip);
    }

    private static bool TryPlayLegacyAnimation(GameObject avatarRoot)
    {
        Animation legacyAnimation = avatarRoot.GetComponentInChildren<Animation>(true);
        if (legacyAnimation == null) return false;

        legacyAnimation.enabled = true;
        legacyAnimation.playAutomatically = true;
        legacyAnimation.wrapMode = WrapMode.Loop;

        if (legacyAnimation.clip != null)
        {
            legacyAnimation.clip.wrapMode = WrapMode.Loop;
            if (!legacyAnimation.isPlaying)
                legacyAnimation.Play();
            return true;
        }

        foreach (AnimationState state in legacyAnimation)
        {
            if (state.clip == null) continue;

            state.wrapMode = WrapMode.Loop;
            legacyAnimation.clip = state.clip;
            legacyAnimation.Play();
            return true;
        }

        return false;
    }

    private bool PlayClipViaPlayables(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null) return false;

        StopPlayableIdle();

        _idleGraph = PlayableGraph.Create($"{animator.gameObject.name}_IdleGraph");
        _idleGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_idleGraph, "Idle", animator);
        _idlePlayable = AnimationClipPlayable.Create(_idleGraph, clip);
        _idlePlayable.SetDuration(clip.length);
        _idlePlayable.SetTime(0);
        _idleClipLength = clip.length;

        output.SetSourcePlayable(_idlePlayable);
        _idleGraph.Play();
        return true;
    }

    private static AnimationClip ResolveIdleClip(GameObject avatarRoot, bool isMaleAvatar)
    {
        AnimationClip[] all = Resources.FindObjectsOfTypeAll<AnimationClip>();
        if (all == null || all.Length == 0) return null;

        var candidates = new List<AnimationClip>();
        foreach (AnimationClip clip in all)
        {
            if (clip == null) continue;
            if ((clip.hideFlags & HideFlags.DontSaveInBuild) != 0) continue;
            if (clip.length < MinAvatarClipDurationSeconds) continue;

            string nameLower = clip.name.ToLowerInvariant();
            if (HasUIKeyword(nameLower)) continue;

            candidates.Add(clip);
        }

        if (candidates.Count == 0) return null;

        AnimationClip avatarMatched = PickClipMatchingAvatar(candidates, isMaleAvatar);
        if (avatarMatched != null) return avatarMatched;

        AnimationClip idleNamed = PickFirstWithKeyword(candidates, "idle");
        if (idleNamed != null) return idleNamed;

        return PickLongest(candidates);
    }

    private static AnimationClip PickClipMatchingAvatar(List<AnimationClip> clips, bool isMaleAvatar)
    {
        string[] avatarKeywords = isMaleAvatar
            ? new[] { "model 2", "model2", "male" }
            : new[] { "model 1", "model1", "female" };

        AnimationClip bestIdle = null;
        AnimationClip bestAny = null;

        foreach (AnimationClip clip in clips)
        {
            string nameLower = clip.name.ToLowerInvariant();

            bool matchesAvatar = false;
            foreach (string keyword in avatarKeywords)
            {
                if (nameLower.Contains(keyword))
                {
                    matchesAvatar = true;
                    break;
                }
            }

            if (!matchesAvatar) continue;

            if (bestAny == null) bestAny = clip;
            if (nameLower.Contains("idle") && bestIdle == null) bestIdle = clip;
        }

        return bestIdle != null ? bestIdle : bestAny;
    }

    private static AnimationClip PickFirstWithKeyword(List<AnimationClip> clips, string keyword)
    {
        string keywordLower = keyword.ToLowerInvariant();
        foreach (AnimationClip clip in clips)
            if (clip.name.ToLowerInvariant().Contains(keywordLower))
                return clip;

        return null;
    }

    private static AnimationClip PickLongest(List<AnimationClip> clips)
    {
        AnimationClip longest = null;
        float longestLength = 0f;

        foreach (AnimationClip clip in clips)
        {
            if (clip.length > longestLength)
            {
                longest = clip;
                longestLength = clip.length;
            }
        }

        return longest;
    }

    private static bool HasUIKeyword(string nameLower)
    {
        foreach (string keyword in UIClipNameKeywords)
            if (nameLower.Contains(keyword))
                return true;

        return false;
    }

    private void StopAnimationLookup()
    {
        if (_animationLookupRoutine == null) return;

        StopCoroutine(_animationLookupRoutine);
        _animationLookupRoutine = null;
    }

    private void StopPlayableIdle()
    {
        if (!_idleGraph.IsValid()) return;

        _idleGraph.Destroy();
        _idlePlayable = default;
        _idleClipLength = 0f;
    }
}
