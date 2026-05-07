using UnityEngine;
using System.Collections.Generic;

public class UnitIntroManager : MonoBehaviour
{
    [System.Serializable]
    public struct IntroMapping
    {
        public int unitID;
        [TextArea(3, 10)]
        public string introText;
    }

    [Header("Intro Settings")]
    public List<IntroMapping> introList;
    public float startDelay = 1.0f; // Give the VR user a second to adjust

    void Start()
    {
        PlayIntroForSelectedUnit();
    }

    public void PlayIntroForSelectedUnit()
    {
        int selectedID = AnatomyState.SelectedAnatomyUnitID;

        foreach (var mapping in introList)
        {
            if (mapping.unitID == selectedID)
            {
                if (!string.IsNullOrEmpty(mapping.introText))
                {
                    Invoke(nameof(ExecuteSpeech), startDelay);
                }
                return;
            }
        }

        Debug.LogWarning("No intro text found for Unit ID: " + selectedID);
    }

    private void ExecuteSpeech()
    {
        int selectedID = AnatomyState.SelectedAnatomyUnitID;
        foreach (var mapping in introList)
        {
            if (mapping.unitID == selectedID)
            {
                if (TTSClient.Instance != null)
                {
                    TTSClient.Instance.Speak(mapping.introText);
                }
                return;
            }
        }
    }
}