using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitIntroManager : MonoBehaviour
{
    [System.Serializable]
    public struct IntroMapping
    {
        public int unitID;
        [TextArea(3, 10)] public string introText;
    }

    public UnitInitializer initializer;
    public List<IntroMapping> introList;
    public GameObject lessonPanel;
    public float startDelay = 1.0f;

    void Start()
    {
        if (lessonPanel != null) lessonPanel.SetActive(false);
        StartCoroutine(IntroSequenceRoutine());
    }

    private IEnumerator IntroSequenceRoutine()
    {
        int selectedID = AnatomyState.SelectedAnatomyUnitID;
        string textToRead = "";

        foreach (var mapping in introList)
        {
            if (mapping.unitID == selectedID)
            {
                textToRead = mapping.introText;
                break;
            }
        }

        if (string.IsNullOrEmpty(textToRead))
        {
            EnableSystems();
            yield break;
        }

        yield return new WaitForSeconds(startDelay);

        // request speech
        TTSClient.Instance.Speak(textToRead);

        // wait until the AI actually starts talking (Handshaking)
        float timeout = 4.0f;
        while (!TTSClient.Instance.IsSpeaking() && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // wait until it stops talking
        while (TTSClient.Instance.IsSpeaking())
        {
            yield return null;
        }

        // final safety buffer
        yield return new WaitForSeconds(0.3f);

        EnableSystems();
    }

    private void EnableSystems()
    {
        int selectedID = AnatomyState.SelectedAnatomyUnitID;

        if (lessonPanel != null) lessonPanel.SetActive(true);

        if (initializer != null)
        {
            foreach (var mapping in initializer.unitList)
            {
                if (mapping.unitID == selectedID && mapping.lessonManager != null)
                {
                    mapping.lessonManager.enabled = true;
                    return;
                }
            }
        }
    }
}