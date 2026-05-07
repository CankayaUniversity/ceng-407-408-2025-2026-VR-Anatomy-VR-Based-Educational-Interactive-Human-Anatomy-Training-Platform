using UnityEngine;
using TMPro;

public class LessonUIReader : MonoBehaviour
{
    [Header("Drag UI Text Objects From Canvas")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    void OnEnable() { LessonManager.OnBoneChanged += HandleBoneChanged; }
    void OnDisable()
    {
        LessonManager.OnBoneChanged -= HandleBoneChanged;
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();
    }

    private void HandleBoneChanged(Transform newBone)
    {
        // Delay 0.1s to allow JSON script to update the text slots first
        Invoke(nameof(ReadCurrentCard), 0.1f);
    }

    private void ReadCurrentCard()
    {
        if (titleText == null || descriptionText == null) return;
        string text = $"{titleText.text}. {descriptionText.text}";
        TTSClient.Instance.Speak(text);
    }
}