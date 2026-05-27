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

    private const string StudentNamePrefKey = "StudentName";

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

        // affectionate name
        string studentName = PlayerPrefs.GetString(StudentNamePrefKey, "").Trim();
        if (!string.IsNullOrEmpty(studentName))
        {
            string affectionateName = BuildAffectionateName(studentName);
            
            textToRead = $"Merhaba {affectionateName}, hoþ geldin. " + textToRead;
        }

        // Request speech
        TTSClient.Instance.Speak(textToRead);

        
        float timeout = 4.0f;
        while (!TTSClient.Instance.IsSpeaking() && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        
        while (TTSClient.Instance.IsSpeaking())
        {
            yield return null;
        }

        
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

    
    private static string BuildAffectionateName(string rawName)
    {
        string name = rawName.Trim();
        if (string.IsNullOrEmpty(name)) return "";

        string firstName = name.Split(' ')[0];
        char lastVowel = FindLastTurkishVowel(firstName);
        string suffix;

        switch (lastVowel)
        {
            case 'a':
            case 'A':
            case 'ý':
            case 'I':
                suffix = "cýðým";
                break;
            case 'e':
            case 'E':
            case 'i':
            case 'Ý':
                suffix = "ciðim";
                break;
            case 'o':
            case 'O':
            case 'u':
            case 'U':
                suffix = "cuðum";
                break;
            case 'ö':
            case 'Ö':
            case 'ü':
            case 'Ü':
                suffix = "cüðüm";
                break;
            default:
                suffix = "cýðým";
                break;
        }

        return firstName + suffix;
    }

    private static char FindLastTurkishVowel(string text)
    {
        if (string.IsNullOrEmpty(text)) return '\0';
        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == 'a' || c == 'A' || c == 'e' || c == 'E' || c == 'ý' || c == 'I' ||
                c == 'i' || c == 'Ý' || c == 'o' || c == 'O' || c == 'ö' || c == 'Ö' ||
                c == 'u' || c == 'U' || c == 'ü' || c == 'Ü')
            {
                return c;
            }
        }
        return '\0';
    }
}