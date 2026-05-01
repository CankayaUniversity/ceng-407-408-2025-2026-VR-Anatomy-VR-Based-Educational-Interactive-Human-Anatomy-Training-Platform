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
    public List<BoneData> entries;
}

public class LessonManager : MonoBehaviour
{
    public QuizTransitionManager transitionManager;

    [Header("UI References")]
    public AnatomyUIController uiController;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    [Header("Bone Sequence")]
    public List<GameObject> bones;
    public BoneVisualManager visualsManager;

    private Dictionary<string, BoneData> dataLookup = new Dictionary<string, BoneData>();
    private int currentIndex = 0;

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
        currentIndex = 0;
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
        if (currentIndex < bones.Count - 1)
        {
            currentIndex++;
            ActivateStep(currentIndex);
        }
        else
        {
            Debug.Log("Skeleton System Lesson Complete!");

            //Trigger the Quiz transition instead of just logging
            if (transitionManager != null)
            {
                transitionManager.TriggerQuizTransition();
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

    void ActivateStep(int index)
    {
        if (index < 0 || index >= bones.Count) return;

        GameObject currentBone = bones[index];
        uiController.SetNewTarget(currentBone.transform);

        if (visualsManager != null)
        {
            // This now triggers the material change AND the grab-script toggle
            visualsManager.FocusBone(currentBone, bones);
        }

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