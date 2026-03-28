#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Checks that voice-over audio files exist in Resources/Audio/Intro.
/// No longer generates placeholder beeps — real voice-over files are used.
/// Run "VRAnatomy > Intro Ses Durumunu Kontrol Et" to verify.
/// </summary>
public static class MenuIntroSetupEditor
{
    private const string Folder = "Assets/Resources/Audio/Intro";

    private static readonly string[] ExpectedFiles =
    {
        "intro_00_hosgeldiniz",
        "intro_01_ogrenmeye_basla",
        "intro_02_test_quiz",
        "intro_03_serbest_inceleme",
        "intro_04_yapay_zeka",
        "intro_05_ayarlar",
        "intro_06_hakkinda",
    };

    [MenuItem("VRAnatomy/Intro Ses Durumunu Kontrol Et")]
    public static void CheckAudioStatus()
    {
        int found = 0;
        int missing = 0;

        foreach (var name in ExpectedFiles)
        {
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.mp3");
            if (clip == null)
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav");

            if (clip != null)
            {
                found++;
                Debug.Log($"  [OK] {name} ({clip.length:F1}s)");
            }
            else
            {
                missing++;
                Debug.LogWarning($"  [EKSIK] {name}");
            }
        }

        if (missing == 0)
            Debug.Log($"<color=green>[MenuIntro] Tüm ses dosyaları mevcut ({found}/{ExpectedFiles.Length})</color>");
        else
            Debug.LogWarning(
                $"[MenuIntro] {missing} ses dosyası eksik! " +
                $"generate_voiceover.py ile oluşturun veya {Folder}/ klasörüne ekleyin.");
    }
}
#endif
