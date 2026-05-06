using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI; // Required for the Button reference

[System.Serializable]
public class BoneData
{
    public string id;
    public string title;
    public string body;
    public string[] steps;
}

[System.Serializable]
public class BoneList
{
    public List<BoneData> entries;
}

public class LessonManager : MonoBehaviour
{
    public static LessonManager Instance;
    public static event System.Action<Transform> OnBoneChanged;

    [Header("UI Managers")]
    public ReviewManager reviewManager;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    
    public Button nextButton;

    [Header("Bone Sequence")]
    public List<GameObject> bones;
    public BoneVisualManager visualsManager;

    private Dictionary<string, BoneData> dataLookup = new Dictionary<string, BoneData>();
    private int currentIndex = 0;
    public bool IsReviewMode = false;

    public bool TryGetBoneReviewPayload(int index, out string boneName, out string unitName, out string originalText)
    {
        boneName = "";
        unitName = GetSelectedUnitName();
        originalText = "";

        if (index < 0 || index >= bones.Count)
            return false;

        GameObject selectedBone = bones[index];
        BoneIdentity identity = selectedBone != null ? selectedBone.GetComponent<BoneIdentity>() : null;

        if (identity != null && !string.IsNullOrEmpty(identity.id) && dataLookup.TryGetValue(identity.id, out BoneData data))
        {
            boneName = data.title;
            originalText = BuildFullDescription(data);
            return !string.IsNullOrWhiteSpace(boneName) && !string.IsNullOrWhiteSpace(originalText);
        }

        boneName = identity != null && !string.IsNullOrEmpty(identity.fallbackDisplayName)
            ? identity.fallbackDisplayName
            : selectedBone != null ? selectedBone.name : "";

        originalText = infoText != null ? infoText.text : "";
        return !string.IsNullOrWhiteSpace(boneName) && !string.IsNullOrWhiteSpace(originalText);
    }

    void OnEnable()
    {
        Instance = this;
        EnsureLessonVoiceReader();

        // Automatically wire up the button when this unit becomes active
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;

        // Clean up the button to prevent cross-talk
        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();
    }

    private void EnsureLessonVoiceReader()
    {
        if (GetComponent<LessonUIReader>() != null)
            return;

        if (FindFirstObjectByType<LessonUIReader>() != null)
            return;

        gameObject.AddComponent<LessonUIReader>();
        Debug.Log("[LessonManager] LessonUIReader bulunamad?; bu LessonManager ùzerine otomatik eklendi.", this);
    }

    void Start()
    {
        LoadJsonData();
        if (bones.Count > 0)
            Invoke(nameof(StartLesson), 0.1f);
    }

    private void StartLesson()
    {
        currentIndex = 0;
        ActivateStep(currentIndex);
    }

    public void ResetLesson()
    {
        IsReviewMode = false;

        currentIndex = 0;

        //Reenable the button listener just in case
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }

        ActivateStep(currentIndex);
    }

    void LoadJsonData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("JsonFiles/StartLearning/motion_system_education_data");
        if (jsonAsset != null)
        {
            BoneList loadedData = JsonUtility.FromJson<BoneList>(jsonAsset.text);
            foreach (var data in loadedData.entries)
            {
                if (!dataLookup.ContainsKey(data.id))
                    dataLookup.Add(data.id, data);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) NextStep();
        if (Input.GetKeyDown(KeyCode.Backspace)) PreviousStep();
    }

    public void NextStep()
    {
        if (IsReviewMode) return;

        if (currentIndex < bones.Count - 1)
        {
            currentIndex++;
            ActivateStep(currentIndex);
        }
        else
        {
            IsReviewMode = true;
            if (reviewManager != null)
            {
                reviewManager.OpenReview();
            }
        }
    }

    public void PreviousStep()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ActivateStep(currentIndex);
        }
    }

    public void ActivateStep(int index)
    {
        if (index < 0 || index >= bones.Count) return;

        currentIndex = index;
        GameObject currentBone = bones[index];

        OnBoneChanged?.Invoke(currentBone.transform);

        if (visualsManager != null)
        {
            visualsManager.FocusBone(currentBone, bones);
        }

        BoneIdentity identity = currentBone.GetComponent<BoneIdentity>();
        if (identity != null && dataLookup.ContainsKey(identity.id))
        {
            BoneData data = dataLookup[identity.id];
            titleText.text = data.title;
            infoText.text = BuildFullDescription(data);
        }
    }

    private static string BuildFullDescription(BoneData data)
    {
        if (data == null)
            return "";

        string fullDescription = data.body;
        if (data.steps != null && data.steps.Length > 0)
        {
            fullDescription += "\n\n";
            foreach (string step in data.steps)
                fullDescription += "ù " + step + "\n";
        }

        return fullDescription;
    }

    private static string GetSelectedUnitName()
    {
        switch (AnatomyState.SelectedLessonSection)
        {
            case LessonUIReader.LessonSection.HeadAndFaceBones:
                return "Ba\u015f ve Y\u00fcz Kemikleri";
            case LessonUIReader.LessonSection.TrunkBones:
                return "G\u00f6vde Kemikleri";
            case LessonUIReader.LessonSection.UpperExtremityBones:
                return "\u00dcst Ekstremite Kemikleri";
            case LessonUIReader.LessonSection.LowerExtremityBones:
                return "Alt Ekstremite Kemikleri";
            case LessonUIReader.LessonSection.SkeletalMuscles:
                return "\u0130skelet Kaslar\u0131";
            case LessonUIReader.LessonSection.HeartStructure:
                return "Kalbin Yap\u0131s\u0131";
            case LessonUIReader.LessonSection.Vessels:
                return "Damarlar";
        }

        switch (AnatomyState.SelectedAnatomyUnitID)
        {
            case 1:
                return "Ba\u015f ve Y\u00fcz Kemikleri";
            case 2:
                return "G\u00f6vde Kemikleri";
            case 3:
                return "\u00dcst Ekstremite Kemikleri";
            case 4:
                return "Alt Ekstremite Kemikleri";
            case 5:
                return "\u0130skelet Kaslar\u0131";
            case 6:
                return "Kalbin Yap\u0131s\u0131";
            case 7:
                return "Damarlar";
            default:
                return "Bilinmeyen \u00dcnite";
        }
    }
}