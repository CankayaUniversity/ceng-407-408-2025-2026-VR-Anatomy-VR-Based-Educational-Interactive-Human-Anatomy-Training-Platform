using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AvatarSelectionSettingsInstaller
{
    private const string SettingsSceneName = "07_Settings";
    private const string SourceRowName = "ShowAnswerTextRow";
    private const string InstalledRowName = "AvatarSelectionRow";
    private const string FemaleToggleName = "AvatarFemaleToggle";
    private const string MaleToggleName = "AvatarMaleToggle";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SettingsSceneName) return;
        InstallAvatarRow();
    }

    private static void InstallAvatarRow()
    {
        if (GameObject.Find(InstalledRowName) != null) return;

        GameObject sourceRow = GameObject.Find(SourceRowName);
        if (sourceRow == null)
        {
            Debug.LogWarning("[AvatarSelection] Source row not found.");
            return;
        }

        Transform parent = sourceRow.transform.parent;
        if (parent == null) return;

        GameObject newRow = Object.Instantiate(sourceRow, parent);
        newRow.name = InstalledRowName;

        var rowRect = newRow.GetComponent<RectTransform>();
        var sourceRect = sourceRow.GetComponent<RectTransform>();
        if (rowRect != null && sourceRect != null)
        {
            rowRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -58f);
        }

        var femaleToggle = newRow.GetComponentInChildren<Toggle>(true);
        if (femaleToggle == null)
        {
            Debug.LogWarning("[AvatarSelection] Toggle could not be found on cloned row.");
            return;
        }
        femaleToggle.name = FemaleToggleName;

        CleanupSourceBinders(newRow);

        TMP_Text label = null;
        var tmpLabels = newRow.GetComponentsInChildren<TMP_Text>(true);
        if (tmpLabels != null && tmpLabels.Length > 0)
            label = tmpLabels[0];

        if (label != null)
        {
            label.text = "Avatar Seçimi";
            AlignTitleWithOptions(label);
        }

        Toggle maleToggle = Object.Instantiate(femaleToggle, femaleToggle.transform.parent);
        maleToggle.name = MaleToggleName;
        CleanupSourceBinders(newRow);

        ConfigureOptionToggle(femaleToggle, new Vector2(80f, -14f), "Kız");
        ConfigureOptionToggle(maleToggle, new Vector2(205f, -14f), "Erkek");

        var avatarBinder = newRow.AddComponent<AvatarSelectionToggleBinder>();
        avatarBinder.Initialize(femaleToggle, maleToggle, label);
    }

    private static void AlignTitleWithOptions(TMP_Text label)
    {
        var labelRect = label.GetComponentInParent<RectTransform>();
        if (labelRect != null)
            labelRect.anchoredPosition = new Vector2(0f, -30f);
    }

    private static void ConfigureOptionToggle(Toggle toggle, Vector2 anchoredPosition, string optionText)
    {
        var toggleRect = toggle.GetComponent<RectTransform>();
        if (toggleRect != null)
        {
            toggleRect.anchoredPosition = anchoredPosition;
            toggleRect.sizeDelta = new Vector2(95f, 24f);
        }

        var legacyLabel = toggle.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.text = optionText;
            legacyLabel.color = Color.white;
            legacyLabel.fontSize = 14;
            legacyLabel.alignment = TextAnchor.MiddleLeft;

            var labelRect = legacyLabel.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.anchoredPosition = new Vector2(22f, 0f);
                labelRect.sizeDelta = new Vector2(-22f, 0f);
            }
        }
    }

    private static void CleanupSourceBinders(GameObject row)
    {
        foreach (var answerBinder in row.GetComponentsInChildren<ShowAnswerTextToggleBinder>(true))
        {
            answerBinder.enabled = false;
            Object.Destroy(answerBinder);
        }

        foreach (var backgroundChanger in row.GetComponentsInChildren<ToggleBackgroundColorChanger>(true))
        {
            backgroundChanger.enabled = false;
            Object.Destroy(backgroundChanger);
        }
    }
}
