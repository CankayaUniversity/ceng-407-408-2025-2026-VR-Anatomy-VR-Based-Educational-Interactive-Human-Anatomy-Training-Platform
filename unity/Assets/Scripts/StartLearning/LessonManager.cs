using UnityEngine;
using System.Collections.Generic;
using TMPro;

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
    public List<BoneData> bones;
}

public class LessonManager : MonoBehaviour
{
    [Header("UI References")]
    public AnatomyUIController uiController;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    [Header("Bone Sequence")]
    public List<GameObject> bones;

    private Dictionary<string, BoneData> dataLookup = new Dictionary<string, BoneData>();
    private int currentIndex = 0;

    void Start()
    {
        LoadJsonData();
        if (bones.Count > 0) ActivateStep(0);
    }

    void LoadJsonData()
    {
        // Path is relative to the "Resources" folder. 
        // IMPORTANT: Do NOT include the ".json" extension here!
        TextAsset jsonAsset = Resources.Load<TextAsset>("JsonFiles/StartLearningBones");

        if (jsonAsset != null)
        {
            BoneList loadedData = JsonUtility.FromJson<BoneList>(jsonAsset.text);

            foreach (var data in loadedData.bones)
            {
                if (!dataLookup.ContainsKey(data.id))
                    dataLookup.Add(data.id, data);
            }
            Debug.Log($"Successfully loaded {dataLookup.Count} bones from Resources.");
        }
        else
        {
            Debug.LogError("Could not find 'StartLearningBones' in Resources/JsonFiles/");
        }
    }

    void Update()
    {
        // Space to go forward
        if (Input.GetKeyDown(KeyCode.Space)) NextStep();

        // Backspace to go backward (Optional but helpful!)
        if (Input.GetKeyDown(KeyCode.Backspace)) PreviousStep();
    }

    public void NextStep()
    {
        if (currentIndex < bones.Count - 1)
        {
            currentIndex++;
            ActivateStep(currentIndex);
        }
        else
        {
            Debug.Log("Skeleton System Lesson Complete!");
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

    void ActivateStep(int index)
    {
        GameObject currentBone = bones[index];
        uiController.SetNewTarget(currentBone.transform);

        BoneIdentity identity = currentBone.GetComponent<BoneIdentity>();

        if (identity != null && dataLookup.ContainsKey(identity.id))
        {
            BoneData data = dataLookup[identity.id];

            titleText.text = !string.IsNullOrEmpty(data.title) ? data.title :
                             (!string.IsNullOrEmpty(identity.fallbackDisplayName) ? identity.fallbackDisplayName : currentBone.name);

            string fullDescription = data.body;
            if (data.steps != null && data.steps.Length > 0)
            {
                fullDescription += "\n\n";
                foreach (string step in data.steps)
                {
                    fullDescription += "• " + step + "\n";
                }
            }
            infoText.text = fullDescription;
        }
        else
        {
            titleText.text = identity != null ? identity.fallbackDisplayName : currentBone.name;
            infoText.text = "Missing data for ID: " + (identity != null ? identity.id : "No ID Script");
        }
    }
}