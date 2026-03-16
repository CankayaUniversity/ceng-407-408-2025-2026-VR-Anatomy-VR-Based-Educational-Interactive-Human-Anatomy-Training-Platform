using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreeExploreTutorialUI : MonoBehaviour
{
    [System.Serializable]
    public class TutorialPage
    {
        public string title;
        [TextArea(3, 8)] public string body;
        public Sprite image;
    }

    [Header("Page Data")]
    [SerializeField] private List<TutorialPage> pages = new();

    [Header("UI References")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageText;
    [SerializeField] private Image tutorialImage;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;

    [Header("XR Ray Objects")]
    [SerializeField] private GameObject leftRayObject;
    [SerializeField] private GameObject rightRayObject;

    [Header("Free Explore Start")]
    [SerializeField] private FreeExploreController freeExploreController;
    [SerializeField] private MotionSubUnit startSubUnit;

    private int currentPageIndex = 0;

    private void Start()
{
    if (pages == null || pages.Count == 0)
    {
        Debug.LogError("[FreeExploreTutorialUI] No tutorial pages assigned.", this);
        return;
    }

    if (tutorialRoot != null)
        tutorialRoot.SetActive(true);

    if (freeExploreController != null)
        freeExploreController.PrepareForTutorialMode();

    SetTutorialRay(true);
    ShowPage(0);
}

    public void NextPage()
    {
        if (pages == null || pages.Count == 0) return;

        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (pages == null || pages.Count == 0) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    public void FinishTutorial()
{
    Debug.Log("Tutorial finished.");

    if (tutorialRoot != null)
        tutorialRoot.SetActive(false);

    SetTutorialRay(false);

    if (freeExploreController == null)
    {
        Debug.LogWarning("[FreeExploreTutorialUI] FreeExploreController reference is missing.", this);
        return;
    }

    MotionSubUnit subUnitToStart = NavigationState.SelectedMotionSubUnit;

    Debug.Log("[FreeExploreTutorialUI] Starting Free Explore with selected subunit: " + subUnitToStart);
    freeExploreController.StartSelectionBySubUnit(subUnitToStart);
}

    private void SetTutorialRay(bool isEnabled)
    {
        if (leftRayObject != null)
            leftRayObject.SetActive(isEnabled);

        if (rightRayObject != null)
            rightRayObject.SetActive(isEnabled);
    }

    private void ShowPage(int index)
{
    if (index < 0 || index >= pages.Count)
        return;

    TutorialPage page = pages[index];

    if (titleText != null)
        titleText.text = page.title;

    if (bodyText != null)
        bodyText.text = page.body;

    if (pageText != null)
        pageText.text = $"{index + 1} / {pages.Count}";

    if (tutorialImage != null)
    {
        tutorialImage.sprite = page.image;
        tutorialImage.enabled = page.image != null;
        tutorialImage.preserveAspect = true;
    }

    if (backButton != null)
        backButton.gameObject.SetActive(index > 0);

    bool isLastPage = index == pages.Count - 1;

    if (nextButton != null)
        nextButton.gameObject.SetActive(!isLastPage);

    if (startButton != null)
        startButton.gameObject.SetActive(true);
}
}