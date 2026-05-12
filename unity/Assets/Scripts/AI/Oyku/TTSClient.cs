using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSClient : MonoBehaviour
{
    public static TTSClient Instance;
    [SerializeField] private string ttsUrl = "http://127.0.0.1:8001/tts";
    private AudioSource _audio;
    private int _currentRequestId = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0;
        _audio.playOnAwake = false;
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_audio.isPlaying)
        {
            _audio.Stop();
            Debug.LogError("[AI_SERVICE] Previous audio killed to make room for new bone.");
        }

        _currentRequestId++;
        StopAllCoroutines();
        StartCoroutine(SpeakRoutine(text, _currentRequestId));
    }

    // New logic to check if AI is currently talking
    public bool IsSpeaking()
    {
        return _audio != null && _audio.isPlaying;
    }

    public void Stop()
    {
        _currentRequestId++;
        if (_audio.isPlaying) _audio.Stop();
    }

    private IEnumerator SpeakRoutine(string text, int requestId)
    {
        bool isMale = SettingsManager.Instance != null &&
                      SettingsManager.Instance.SelectedAvatarType == SettingsManager.AvatarType.Male;

        string voice = isMale ? "tr-TR-AhmetNeural" : "tr-TR-EmelNeural";

        // CLEANING THE TEXT (Fixes Code 422)
        string cleanText = text.Trim()
            .Replace("\n", " ")      // Remove line breaks
            .Replace("\r", " ")      // Remove carriage returns
            .Replace("\"", "\\\"")   // Escape double quotes
            .Replace("•", "");       // Remove bullet points

        string json = "{\"text\":\"" + cleanText + "\", \"voice\":\"" + voice + "\"}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (requestId != _currentRequestId) yield break;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AI_SERVICE] FAILED! Error: " + req.error + " | Code: " + req.responseCode);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip != null && clip.length > 0)
            {
                _audio.clip = clip;
                _audio.Play();
                Debug.LogError("[AI_SERVICE] SUCCESS! Playing " + clip.length.ToString("F2") + "s of audio.");
            }
        }
    }
}