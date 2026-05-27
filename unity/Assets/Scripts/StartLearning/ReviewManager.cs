using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ReviewManager : MonoBehaviour
{
    [Header("Review UI Layout")]
    [Tooltip("The component that will display the final combined paragraph on screen.")]
    public TextMeshProUGUI reviewDescriptionText;

    [Header("Lesson UI Buttons")]
    public GameObject nextButton;
    public GameObject previousButton;
    public GameObject anladimButton;

    [Header("UI Panels")]
    public GameObject lessonPanel;
    public GameObject reviewPanel;

    [Header("Button Settings")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    private const string StudentNamePrefKey = "StudentName";

    
    private const string ReviewSentenceTemplate = "Bölümün sonuna geldik {0}. Aklýna takýlan bir yapý kaldýysa, buradaki butonlarý kullanarak tekrar gözden geçirebilirsin.";

    public void OpenReview()
    {
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);

        if (BoneVisualManager.Active != null && LessonManager.Instance != null)
        {
            BoneVisualManager.Active.ResetAllBones(LessonManager.Instance.bones);
            BoneVisualManager.Active.SnapAllBonesToInitialTransforms();
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        // Fetch the student's name from storage
        string studentName = PlayerPrefs.GetString(StudentNamePrefKey, "").Trim();
        string finalSpeechText = "";

        // Generate the dynamic affectionate name prefix 
        if (!string.IsNullOrEmpty(studentName))
        {
            string affectionateName = BuildAffectionateName(studentName);
            
            finalSpeechText = string.Format(ReviewSentenceTemplate, affectionateName);
        }
        else
        {
            
            finalSpeechText = string.Format(ReviewSentenceTemplate, " ");
        }

        // update the text component on screen
        if (reviewDescriptionText != null)
        {
            reviewDescriptionText.text = finalSpeechText;
        }

        
        if (!string.IsNullOrWhiteSpace(finalSpeechText) && TTSClient.Instance != null)
        {
            TTSClient.Instance.Speak(finalSpeechText);
        }

        PopulateButtons();

        if (nextButton != null) nextButton.SetActive(false);
        if (previousButton != null) previousButton.SetActive(false);
        if (anladimButton != null) GridSetActive(anladimButton, true);
    }

    private void PopulateButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        List<GameObject> bones = LessonManager.Instance.bones;

        for (int i = 0; i < bones.Count; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            btnObj.layer = LayerMask.NameToLayer("UI");

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();
            if (img != null) img.enabled = true;
            if (btn != null) btn.enabled = true;

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.enabled = true;
                txt.gameObject.SetActive(true);

                BoneIdentity identity = bones[i].GetComponent<BoneIdentity>();
                txt.text = (identity != null && !string.IsNullOrEmpty(identity.fallbackDisplayName))
                           ? identity.fallbackDisplayName
                           : bones[i].name;
            }

            int index = i;
            btn.onClick.AddListener(() => SelectBone(index));
        }
    }

    private void SelectBone(int index)
    {
        reviewPanel.SetActive(false);
        lessonPanel.SetActive(true);

        LessonManager.Instance.ActivateStep(index);
        LessonManager.Instance.IsReviewMode = false;

        if (nextButton != null) nextButton.SetActive(false);
        if (previousButton != null) previousButton.SetActive(false);
        if (anladimButton != null) anladimButton.SetActive(true);
    }

    private void GridSetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    public void ReturnToReview()
    {
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);

        if (BoneVisualManager.Active != null && LessonManager.Instance != null)
        {
            BoneVisualManager.Active.ResetAllBones(LessonManager.Instance.bones);
            BoneVisualManager.Active.SnapAllBonesToInitialTransforms();
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        LessonManager.Instance.IsReviewMode = true;

        if (reviewDescriptionText != null && TTSClient.Instance != null)
        {
            TTSClient.Instance.Speak(reviewDescriptionText.text);
        }
    }

    public void ExitReviewMode()
    {
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        reviewPanel.SetActive(false);
        lessonPanel.SetActive(false);

        LessonManager.Instance.ResetLesson();

        if (anladimButton != null) anladimButton.SetActive(false);
        if (nextButton != null) nextButton.SetActive(true);
        if (previousButton != null) previousButton.SetActive(true);

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
    }

    private static string BuildAffectionateName(string rawName)
    {
        string name = rawName.Trim();
        if (string.IsNullOrEmpty(name)) return "";

        string firstName = name.Split(' ')[0];
        char lastVowel = FindLastTurkishVowel(firstName);
        string suffix;

        switch (lastVowel)
        {
            case 'a':
            case 'A':
            case 'ý':
            case 'I':
                suffix = "cýðým";
                break;
            case 'e':
            case 'E':
            case 'i':
            case 'Ý':
                suffix = "ciðim";
                break;
            case 'o':
            case 'O':
            case 'u':
            case 'U':
                suffix = "cuðum";
                break;
            case 'ö':
            case 'Ö':
            case 'ü':
            case 'Ü':
                suffix = "cüðüm";
                break;
            default:
                suffix = "cýðým";
                break;
        }

        return firstName + suffix;
    }

    private static char FindLastTurkishVowel(string text)
    {
        if (string.IsNullOrEmpty(text)) return '\0';
        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == 'a' || c == 'A' || c == 'e' || c == 'E' || c == 'ý' || c == 'I' ||
                c == 'i' || c == 'Ý' || c == 'o' || c == 'O' || c == 'ö' || c == 'Ö' ||
                c == 'u' || c == 'U' || c == 'ü' || c == 'Ü')
            {
                return c;
            }
        }
        return '\0';
    }
}