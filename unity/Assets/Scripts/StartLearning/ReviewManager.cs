using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ReviewManager : MonoBehaviour
{
    [Header("Review AI Settings")]
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

    public void OpenReview()
    {
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);

        if (BoneVisualManager.Active != null && LessonManager.Instance != null)
        {
            BoneVisualManager.Active.ResetAllBones(LessonManager.Instance.bones);
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        if (reviewDescriptionText != null && !string.IsNullOrWhiteSpace(reviewDescriptionText.text))
        {
            TTSClient.Instance.Speak(reviewDescriptionText.text);
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

        // 1. Activate the model step data first
        LessonManager.Instance.ActivateStep(index);

        // 2. Keep this false during the temporary inspection window
        LessonManager.Instance.IsReviewMode = false;

        // 3. Force structural visibility updates LAST so it cannot be overridden by LessonManager
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
        }
        else
        {
            Debug.LogError("[REVIEW] Reset failed. Active Visuals or LessonInstance is null!");
        }

        LessonManager.Instance.IsReviewMode = true;

        if (reviewDescriptionText != null)
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
}