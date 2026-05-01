using UnityEngine;
using UnityEngine.Events;

public class QuizTransitionManager : MonoBehaviour
{
    [Header("UI Swap")]
    [SerializeField] private GameObject lessonUI; // Drag your current Lesson UI Panel here
    [SerializeField] private GameObject quizUI;   // Drag your new Quiz UI Panel here

    [Header("Events")]
    [SerializeField] private UnityEvent onQuizStarted;

    public void TriggerQuizTransition()
    {
        Debug.Log("Switching from Lesson UI to Quiz UI...");

        // 1. Disable the Lesson UI
        if (lessonUI != null) lessonUI.SetActive(false);

        // 2. Enable the Quiz UI
        if (quizUI != null) quizUI.SetActive(true);

        // 3. Trigger additional game-logic (audio, stopping timers, etc.)
        onQuizStarted?.Invoke();
    }
}