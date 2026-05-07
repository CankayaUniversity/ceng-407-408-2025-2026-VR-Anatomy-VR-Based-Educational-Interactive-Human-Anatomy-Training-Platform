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
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _currentRequestId++; // Incremented to cancel older voices
        StopAllCoroutines();
        StartCoroutine(SpeakRoutine(text, _currentRequestId));
    }

    public void Stop()
    {
        _currentRequestId++;
        if (_audio.isPlaying) _audio.Stop();
    }

    private IEnumerator SpeakRoutine(string text, int requestId)
    {
        // Check gender from SettingsManager (keeping your friend's logic)
        bool isMale = SettingsManager.Instance != null &&
                      SettingsManager.Instance.SelectedAvatarType == SettingsManager.AvatarType.Male;

        string voice = isMale ? "tr-TR-AhmetNeural" : "tr-TR-EmelNeural";
        string pitch = isMale ? "+8%" : "0%";

        string json = $"{{\"text\":\"{text.Trim()}\", \"voice\":\"{voice}\", \"pitch\":\"{pitch}\"}}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (requestId != _currentRequestId) yield break; // Newer request started

            if (req.result == UnityWebRequest.Result.Success)
            {
                _audio.clip = DownloadHandlerAudioClip.GetContent(req);
                _audio.Play();
            }
        }
    }
}