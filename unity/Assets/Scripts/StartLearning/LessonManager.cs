using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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
    public Button previousButton;

    [Header("Bone Sequence")]
    public List<GameObject> bones;
    public BoneVisualManager visualsManager;

    [Header("Data Configuration")]
    [Tooltip("Type the path relative to the Resources folder WITHOUT the .json extension.")]
    public string jsonFilePath = "JsonFiles/StartLearning/motion_system_education_data";

    private Dictionary<string, BoneData> dataLookup = new Dictionary<string, BoneData>();
    private int currentIndex = 0;
    public bool IsReviewMode = false;

    
    public GameObject CurrentActiveBone
    {
        get
        {
            if (bones != null && currentIndex >= 0 && currentIndex < bones.Count)
                return bones[currentIndex];
            return null;
        }
    }

    void OnEnable()
    {
        Instance = this;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(PreviousStep);
        }

        LoadJsonData();
        if (bones != null && bones.Count > 0)
        {
            StartLesson();
        }
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
        if (previousButton != null) previousButton.onClick.RemoveAllListeners();
    }

    private void StartLesson()
    {
        currentIndex = 0;
        ResetActiveUnitRotation();
        ActivateStep(currentIndex);
    }

    public void ResetLesson()
    {
        IsReviewMode = false;
        currentIndex = 0;

        
        if (visualsManager != null) visualsManager.SnapAllBonesToInitialTransforms();

        ActivateStep(currentIndex);
    }

    void LoadJsonData()
    {
        dataLookup.Clear();

        if (string.IsNullOrEmpty(jsonFilePath))
        {
            Debug.LogError("[LessonManager] JSON File Path is missing in the Inspector!", this);
            return;
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFilePath);
        if (jsonAsset != null)
        {
            BoneList loadedData = JsonUtility.FromJson<BoneList>(jsonAsset.text);
            foreach (var data in loadedData.entries)
            {
                if (!dataLookup.ContainsKey(data.id))
                    dataLookup.Add(data.id, data);
            }
            Debug.Log($"[LessonManager] Successfully loaded {dataLookup.Count} entries from {jsonFilePath}.");
        }
        else
        {
            Debug.LogError($"[LessonManager] Could not find JSON file at path: Resources/{jsonFilePath}", this);
        }
    }

    public void NextStep()
    {
        if (IsReviewMode) return;

        // Reset bone translations
        if (visualsManager != null) visualsManager.SnapAllBonesToInitialTransforms();

        ResetActiveUnitRotation();

        if (currentIndex < bones.Count - 1)
        {
            currentIndex++;
            ActivateStep(currentIndex);
        }
        else
        {
            IsReviewMode = true;
            if (reviewManager != null) reviewManager.OpenReview();
        }
    }

    public void PreviousStep()
    {
        if (IsReviewMode) return;

        // Reset bone translations
        if (visualsManager != null) visualsManager.SnapAllBonesToInitialTransforms();

        ResetActiveUnitRotation();

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

        if (visualsManager != null) visualsManager.FocusBone(currentBone, bones);

        BoneIdentity identity = currentBone.GetComponent<BoneIdentity>();
        if (identity != null && dataLookup.ContainsKey(identity.id))
        {
            BoneData data = dataLookup[identity.id];
            titleText.text = data.title;
            string fullDescription = data.body;
            if (data.steps != null && data.steps.Length > 0)
            {
                fullDescription += "\n\n";
                foreach (string step in data.steps)
                    fullDescription += "• " + step + "\n";
            }
            infoText.text = fullDescription;
        }
        else
        {
            titleText.text = "Data Missing";
            infoText.text = "Check ID: " + (identity != null ? identity.id : "No Script");
        }

        if (reviewManager != null && reviewManager.reviewPanel.activeSelf == false && IsReviewMode == false)
        {
            if (previousButton != null) previousButton.gameObject.SetActive(currentIndex > 0);
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
    }



    public void ResetActiveUnitRotation()
    {
        if (visualsManager != null)
        {
            RotateUnit currentRotator = visualsManager.GetComponent<RotateUnit>();
            if (currentRotator != null)
            {
                currentRotator.ResetToInitialRotation();
            }
        }
    }


}