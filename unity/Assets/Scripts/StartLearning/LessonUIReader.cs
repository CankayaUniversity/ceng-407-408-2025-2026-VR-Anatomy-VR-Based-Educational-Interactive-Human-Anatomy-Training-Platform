using UnityEngine;
using TMPro;

public class LessonUIReader : MonoBehaviour
{
    [Header("Drag UI Text Objects From Canvas")]
    public TextMeshProUGUI titleSlot;
    public TextMeshProUGUI descriptionSlot;

    // Drag your LessonPanel here
    public GameObject lessonPanel;

    void OnEnable()
    {
        LessonManager.OnBoneChanged += HandleBoneChanged;
    }

    void OnDisable()
    {
        LessonManager.OnBoneChanged -= HandleBoneChanged;
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();
    }

    private void HandleBoneChanged(Transform newBone)
    {
        // Only read if the panel is active
        if (lessonPanel != null && lessonPanel.activeInHierarchy)
        {
            CancelInvoke(nameof(ReadCurrentCard));
            Invoke(nameof(ReadCurrentCard), 0.1f);
        }
    }

    private void ReadCurrentCard()
    {
        if (lessonPanel == null || !lessonPanel.activeInHierarchy) return;

        if (string.IsNullOrWhiteSpace(titleSlot.text))
        {
            Invoke(nameof(ReadCurrentCard), 0.2f);
            return;
        }

        string text = titleSlot.text + ". " + descriptionSlot.text;
        TTSClient.Instance.Speak(text);
    }
}