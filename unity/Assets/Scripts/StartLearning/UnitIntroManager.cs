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
            textToRead = $"Merhaba {affectionateName}, ho\u015F geldin. " + textToRead;
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

                    // FIXED: Instantly clear any rotations applied during the intro sequence
                    mapping.lessonManager.ResetActiveUnitRotation();
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
        // a, ı -> cığım
        case 'a':
        case 'A':
        case '\u0131': // ı
        case 'I':
            suffix = "c\u0131\u011F\u0131m"; // cığım
            break;

        // e, i -> ciğim
        case 'e':
        case 'E':
        case 'i':
        case '\u0130': // İ
            suffix = "ci\u011Fim"; // ciğim
            break;

        // o, u -> cuğum
        case 'o':
        case 'O':
        case 'u':
        case 'U':
            suffix = "cu\u011Fum"; // cuğum
            break;

        // ö, ü -> cüğüm
        case '\u00F6': // ö
        case '\u00D6': // Ö
        case '\u00FC': // ü
        case '\u00DC': // Ü
            suffix = "c\u00FC\u011F\u00FCm"; // cüğüm
            break;

        default:
            suffix = "c\u0131\u011F\u0131m"; // cığım
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

        switch (c)
        {
            case 'a':
            case 'A':
            case 'e':
            case 'E':
            case '\u0131': // ı
            case 'I':
            case 'i':
            case '\u0130': // İ
            case 'o':
            case 'O':
            case '\u00F6': // ö
            case '\u00D6': // Ö
            case 'u':
            case 'U':
            case '\u00FC': // ü
            case '\u00DC': // Ü
                return c;
        }
    }

    return '\0';
}
}