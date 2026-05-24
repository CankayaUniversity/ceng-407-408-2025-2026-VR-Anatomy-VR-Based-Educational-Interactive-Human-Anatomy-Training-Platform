using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ReviewManager : MonoBehaviour
{
    [Header("Review AI Settings")]
    public TextMeshProUGUI reviewDescriptionText;

    [Header("Lesson UI Buttons")]
    public GameObject skipButton;
    public GameObject anladimButton;

    [Header("UI Panels")]
    public GameObject lessonPanel;
    public GameObject reviewPanel;

    [Header("Button Settings")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    public void OpenReview()
    {
        //stop the last bone audio
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);


        // Reset bones to original mat
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


        skipButton.SetActive(false);
        anladimButton.SetActive(true);

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
            btn.onClick.AddListener(() => SelectBone(index)); //instead of dragging buttons OnClick() one by one we do this
        }
    }

    private void SelectBone(int index)
    {
        reviewPanel.SetActive(false);
        lessonPanel.SetActive(true);


        LessonManager.Instance.IsReviewMode = false;

        LessonManager.Instance.ActivateStep(index);
    }


    public void ReturnToReview()
    {
        if (TTSClient.Instance != null) TTSClient.Instance.Stop();

        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);


        // Reset bones to original mat
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
        if (skipButton != null) skipButton.SetActive(true);


        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

    }


}