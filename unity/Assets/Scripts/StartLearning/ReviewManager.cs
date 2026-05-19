using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; 

public class ReviewManager : MonoBehaviour
{
    private const string ReviewTitleMessage = "Tekrar İncele";
    private const string ReviewPromptMessage = "Anlamadığınız bir kısım varsa, tekrar incelemek istediğiniz kemiği seçebilirsiniz.";
    private const string StudentNamePrefKey = "StudentName";
    private const string ReviewWelcomeWithNameTemplate = "Çok güzel ilerledin {0}. Aklında kalan bir yer varsa buradan tekrar inceleyebilirsin.";
    private const string ReviewWelcomeWithoutName = "Çok güzel ilerledin. Aklında kalan bir yer varsa buradan tekrar inceleyebilirsin.";

    [Header("Lesson UI Buttons")]
    public GameObject skipButton;
    public GameObject anladimButton;

    [Header("UI Panels")]
    public GameObject lessonPanel;
    public GameObject reviewPanel;
    [SerializeField] private TextMeshProUGUI reviewTitleText;
    [SerializeField] private TextMeshProUGUI reviewPromptText;

    [Header("Button Settings")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    private bool _hasSpokenReviewPrompt;

    public void OpenReview()
    {
        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);
        SetReviewStaticTexts();

        PopulateButtons();
        SpeakReviewPromptOnce();


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

    private void SetReviewStaticTexts()
    {
        SetReviewTitleText();
        SetReviewPromptText();
    }

    private void SetReviewTitleText()
    {
        if (reviewTitleText == null)
            reviewTitleText = ResolveReviewTitleText();

        if (reviewTitleText != null)
            reviewTitleText.text = ReviewTitleMessage;
    }

    private void SetReviewPromptText()
    {
        if (reviewPromptText == null)
            reviewPromptText = ResolveReviewPromptText();

        if (reviewPromptText != null)
            reviewPromptText.text = ReviewPromptMessage;
    }

    private TextMeshProUGUI ResolveReviewTitleText()
    {
        if (reviewPanel == null) return null;

        TextMeshProUGUI[] texts = reviewPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.name == "TitleText")
                return text;
        }

        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.text == "Review")
                return text;
        }

        return null;
    }

    private TextMeshProUGUI ResolveReviewPromptText()
    {
        if (reviewPanel == null) return null;

        TextMeshProUGUI[] texts = reviewPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.name == "DescriptionText")
                return text;
        }

        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.text.Contains("Would you like to review"))
                return text;
        }

        return null;
    }

    private void SpeakReviewPromptOnce()
    {
        if (_hasSpokenReviewPrompt)
            return;

        LessonUIReader lessonUIReader = ResolveLessonUIReader(LessonManager.Instance);
        if (lessonUIReader == null)
            return;

        _hasSpokenReviewPrompt = true;
        lessonUIReader.SpeakReviewText(BuildReviewWelcomeSpeech());
    }

    private static string BuildReviewWelcomeSpeech()
{
    string studentName = PlayerPrefs.GetString(StudentNamePrefKey, "").Trim();
    if (string.IsNullOrEmpty(studentName))
        return ReviewWelcomeWithoutName;

    return string.Format(ReviewWelcomeWithNameTemplate, BuildAffectionateName(studentName));
}
private static string BuildAffectionateName(string rawName)
{
    string name = rawName.Trim();

    if (string.IsNullOrEmpty(name))
        return "";

    string firstName = name.Split(' ')[0];

    char lastVowel = FindLastTurkishVowel(firstName);

    string suffix;

    switch (lastVowel)
    {
        case 'a':
        case 'A':
        case 'ı':
        case 'I':
            suffix = "cığım";
            break;

        case 'e':
        case 'E':
        case 'i':
        case 'İ':
            suffix = "ciğim";
            break;

        case 'o':
        case 'O':
        case 'u':
        case 'U':
            suffix = "cuğum";
            break;

        case 'ö':
        case 'Ö':
        case 'ü':
        case 'Ü':
            suffix = "cüğüm";
            break;

        default:
            suffix = "cığım";
            break;
    }

    return firstName + suffix;
}

private static char FindLastTurkishVowel(string text)
{
    if (string.IsNullOrEmpty(text))
        return '\0';

    for (int i = text.Length - 1; i >= 0; i--)
    {
        char c = text[i];

        if (IsTurkishVowel(c))
            return c;
    }

    return '\0';
}

private static bool IsTurkishVowel(char c)
{
    return c == 'a' || c == 'A'
        || c == 'e' || c == 'E'
        || c == 'ı' || c == 'I'
        || c == 'i' || c == 'İ'
        || c == 'o' || c == 'O'
        || c == 'ö' || c == 'Ö'
        || c == 'u' || c == 'U'
        || c == 'ü' || c == 'Ü';
}

    private void SelectBone(int index)
    {
        reviewPanel.SetActive(false);
        lessonPanel.SetActive(true);

        LessonManager lessonManager = LessonManager.Instance;
        if (lessonManager == null)
        {
            Debug.LogError("[ReviewManager] LessonManager bulunamadı; review kemik seçimi yapılamadı.", this);
            return;
        }

        lessonManager.IsReviewMode = false;
        lessonManager.ActivateStep(index);
    }

    private LessonUIReader ResolveLessonUIReader(LessonManager lessonManager)
    {
        LessonUIReader reader = lessonManager != null ? lessonManager.GetComponent<LessonUIReader>() : null;
        if (reader != null)
            return reader;

        return FindFirstObjectByType<LessonUIReader>();
    }

    public void ReturnToReview()
    {
        lessonPanel.SetActive(false);
        reviewPanel.SetActive(true);
        SetReviewStaticTexts();

        LessonManager.Instance.IsReviewMode = true;
    }


    public void ExitReviewMode()
    {
        reviewPanel.SetActive(false);
        lessonPanel.SetActive(false);

        LessonManager.Instance.ResetLesson();
        _hasSpokenReviewPrompt = false;

        if (anladimButton != null) anladimButton.SetActive(false);
        if (skipButton != null) skipButton.SetActive(true);


        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

    }


}