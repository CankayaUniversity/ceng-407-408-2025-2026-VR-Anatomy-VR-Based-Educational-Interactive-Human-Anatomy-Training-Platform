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

    [Header("Bone Sequence")]
    public List<GameObject> bones;
    public BoneVisualManager visualsManager;

    private Dictionary<string, BoneData> dataLookup = new Dictionary<string, BoneData>();
    private int currentIndex = 0;
    public bool IsReviewMode = false;

    void OnEnable()
    {
        Instance = this;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }

        // Logic only starts when IntroManager enables this script
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
    }

    void Start()
    {
        // Removed the Invoke. We wait for OnEnable.
        //LoadJsonData();
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
        ActivateStep(currentIndex);
    }

    void LoadJsonData()
    {
        if (dataLookup.Count > 0) return;
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
            if (reviewManager != null) reviewManager.OpenReview();
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
    }
}