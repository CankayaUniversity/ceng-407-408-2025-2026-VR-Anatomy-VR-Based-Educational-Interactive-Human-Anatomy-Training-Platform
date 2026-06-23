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
    private const string ReviewSentence =
        "Bölümün sonuna geldiniz. Aklınıza takılan bir yapı kaldıysa, buradaki butonları kullanarak tekrar gözden geçirebilirsiniz.";

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

        string finalSpeechText = ReviewSentence;

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

}