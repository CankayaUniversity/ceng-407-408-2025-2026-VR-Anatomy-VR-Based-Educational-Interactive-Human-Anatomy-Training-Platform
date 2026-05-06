using UnityEngine;
using TMPro; 

public class LessonUIReader : MonoBehaviour
{
  

    void OnEnable()
    {
        LessonManager.OnBoneChanged += HandleBoneChanged;
    }

    void OnDisable()
    {
        LessonManager.OnBoneChanged -= HandleBoneChanged;
    }

    private void HandleBoneChanged(Transform newBoneTransform)
    {
        if (LessonManager.Instance != null)
        {
            // Access the texts
            string title = LessonManager.Instance.titleText.text;
            string description = LessonManager.Instance.infoText.text;

            ReadText(title, description);
        }
    }

    private void ReadText(string title, string body)
    {
        
        Debug.Log("AI Voice is reading: " + title);
        Debug.Log("Description: " + body);

        // Example: If you have an AI service, you would call it here
        // AIVoiceEngine.Speak(title + ". " + body);
    }
}