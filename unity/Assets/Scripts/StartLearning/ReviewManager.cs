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

    private const string ReviewSentenceTemplate =
        "B\u00F6l\u00FCm\u00FCn sonuna geldik {0}. Akl\u0131na tak\u0131lan bir yap\u0131 kald\u0131ysa, buradaki butonlar\u0131 kullanarak tekrar g\u00F6zden ge\u00E7irebilirsin.";

    public void OpenReview()
    {
        if (TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
        }

        if (lessonPanel != null)
        {
            lessonPanel.SetActive(false);
        }

        if (reviewPanel != null)
        {
            reviewPanel.SetActive(true);
        }

        if (BoneVisualManager.Active != null && LessonManager.Instance != null)
        {
            BoneVisualManager.Active.ResetAllBones(LessonManager.Instance.bones);
            BoneVisualManager.Active.SnapAllBonesToInitialTransforms();

            LessonManager.Instance.ResetActiveUnitRotation();
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        string studentName = PlayerPrefs.GetString(StudentNamePrefKey, "").Trim();
        string finalSpeechText;

        if (!string.IsNullOrEmpty(studentName))
        {
            string affectionateName = BuildAffectionateName(studentName);
            finalSpeechText = string.Format(ReviewSentenceTemplate, affectionateName);
        }
        else
        {
            finalSpeechText = string.Format(ReviewSentenceTemplate, "");
        }

        if (reviewDescriptionText != null)
        {
            reviewDescriptionText.text = finalSpeechText;
        }

        if (!string.IsNullOrWhiteSpace(finalSpeechText) && TTSClient.Instance != null)
        {
            TTSClient.Instance.Speak(finalSpeechText);
        }

        PopulateButtons();

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (previousButton != null)
        {
            previousButton.SetActive(false);
        }

        if (anladimButton != null)
        {
            GridSetActive(anladimButton, true);
        }
    }

    private void PopulateButtons()
    {
        if (buttonContainer == null)
        {
            Debug.LogError("[REVIEW] Button container is null!");
            return;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("[REVIEW] Button prefab is null!");
            return;
        }

        if (LessonManager.Instance == null || LessonManager.Instance.bones == null)
        {
            Debug.LogError("[REVIEW] LessonManager.Instance or bones list is null!");
            return;
        }

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        List<GameObject> bones = LessonManager.Instance.bones;

        for (int i = 0; i < bones.Count; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            btnObj.layer = LayerMask.NameToLayer("UI");

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();

            if (img != null)
            {
                img.enabled = true;
            }

            if (btn != null)
            {
                btn.enabled = true;
            }

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
            {
                txt.enabled = true;
                txt.gameObject.SetActive(true);

                BoneIdentity identity = bones[i].GetComponent<BoneIdentity>();

                txt.text = identity != null && !string.IsNullOrEmpty(identity.fallbackDisplayName)
                    ? identity.fallbackDisplayName
                    : bones[i].name;
            }

            int index = i;

            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectBone(index));
            }
        }
    }

    private void SelectBone(int index)
    {
        if (BoneVisualManager.Active != null)
        {
            BoneVisualManager.Active.SnapAllBonesToInitialTransforms();
        }

        if (LessonManager.Instance != null)
        {
            LessonManager.Instance.ResetActiveUnitRotation();
        }

        if (reviewPanel != null)
        {
            reviewPanel.SetActive(false);
        }

        if (lessonPanel != null)
        {
            lessonPanel.SetActive(true);
        }

        if (LessonManager.Instance != null)
        {
            LessonManager.Instance.ActivateStep(index);
            LessonManager.Instance.IsReviewMode = false;
        }

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (previousButton != null)
        {
            previousButton.SetActive(false);
        }

        if (anladimButton != null)
        {
            anladimButton.SetActive(true);
        }
    }

    private void GridSetActive(GameObject go, bool value)
    {
        if (go != null)
        {
            go.SetActive(value);
        }
    }

    public void ReturnToReview()
    {
        if (TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
        }

        if (lessonPanel != null)
        {
            lessonPanel.SetActive(false);
        }

        if (reviewPanel != null)
        {
            reviewPanel.SetActive(true);
        }

        if (BoneVisualManager.Active != null && LessonManager.Instance != null)
        {
            BoneVisualManager.Active.ResetAllBones(LessonManager.Instance.bones);
            BoneVisualManager.Active.SnapAllBonesToInitialTransforms();

            LessonManager.Instance.ResetActiveUnitRotation();
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        if (LessonManager.Instance != null)
        {
            LessonManager.Instance.IsReviewMode = true;
        }

        if (reviewDescriptionText != null && TTSClient.Instance != null)
        {
            TTSClient.Instance.Speak(reviewDescriptionText.text);
        }
    }

    public void ExitReviewMode()
    {
        if (TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
        }

        if (reviewPanel != null)
        {
            reviewPanel.SetActive(false);
        }

        if (lessonPanel != null)
        {
            lessonPanel.SetActive(false);
        }

        if (LessonManager.Instance != null)
        {
            LessonManager.Instance.ResetLesson();
        }

        if (anladimButton != null)
        {
            anladimButton.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.SetActive(true);
        }

        if (previousButton != null)
        {
            previousButton.SetActive(true);
        }

        if (buttonContainer != null)
        {
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static string BuildAffectionateName(string rawName)
    {
        string name = rawName.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return "";
        }

        string firstName = name.Split(' ')[0];
        char lastVowel = FindLastTurkishVowel(firstName);

        string suffix;

        switch (lastVowel)
        {
            case 'a':
            case 'A':
            case '\u0131':
            case 'I':
                suffix = "c\u0131\u011F\u0131m";
                break;

            case 'e':
            case 'E':
            case 'i':
            case '\u0130':
                suffix = "ci\u011Fim";
                break;

            case 'o':
            case 'O':
            case 'u':
            case 'U':
                suffix = "cu\u011Fum";
                break;

            case '\u00F6':
            case '\u00D6':
            case '\u00FC':
            case '\u00DC':
                suffix = "c\u00FC\u011F\u00FCm";
                break;

            default:
                suffix = "c\u0131\u011F\u0131m";
                break;
        }

        return firstName + suffix;
    }

    private static char FindLastTurkishVowel(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return '\0';
        }

        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];

            switch (c)
            {
                case 'a':
                case 'A':
                case 'e':
                case 'E':
                case '\u0131':
                case 'I':
                case 'i':
                case '\u0130':
                case 'o':
                case 'O':
                case '\u00F6':
                case '\u00D6':
                case 'u':
                case 'U':
                case '\u00FC':
                case '\u00DC':
                    return c;
            }
        }

        return '\0';
    }
}