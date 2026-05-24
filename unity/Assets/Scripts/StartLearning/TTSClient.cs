using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSClient : MonoBehaviour
{
    public static TTSClient Instance;

    [Header("TTS Settings")]
    [SerializeField] private string ttsUrl = "http://127.0.0.1:8001/tts";

    [Header("Voice Settings")]
    [SerializeField] private bool useFemaleVoice = true;

    [SerializeField] private string femaleVoice = "tr-TR-EmelNeural";
    [SerializeField] private string maleVoice = "tr-TR-AhmetNeural";

    [Header("Audio Source Search")]
    [SerializeField] private Transform audioSourceSearchRoot;


    [SerializeField] private bool refreshAudioSourceBeforeSpeaking = true;

    private AudioSource _audio;
    private int _currentRequestId = 0;

    [Serializable]
    private class TTSRequest
    {
        public string text;
        public string voice;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        RefreshActiveAudioSource();
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (refreshAudioSourceBeforeSpeaking)
        {
            RefreshActiveAudioSource();
        }

        if (_audio == null)
        {
            Debug.LogError("[AI_SERVICE] No active AudioSource found. Please assign Audio Source Search Root and make sure one model with AudioSource is active.");
            return;
        }

        if (_audio.isPlaying)
        {
            _audio.Stop();

        }

        _currentRequestId++;
        StopAllCoroutines();
        StartCoroutine(SpeakRoutine(text, _currentRequestId));
    }

    private void RefreshActiveAudioSource()
    {
        _audio = null;

        if (audioSourceSearchRoot == null)
        {

            _audio = GetComponent<AudioSource>();

            if (_audio != null)
            {
                ConfigureAudioSource(_audio);
            }

            return;
        }

        AudioSource[] audioSources = audioSourceSearchRoot.GetComponentsInChildren<AudioSource>(false);

        int activeAudioSourceCount = 0;

        foreach (AudioSource source in audioSources)
        {
            if (source != null && source.isActiveAndEnabled && source.gameObject.activeInHierarchy)
            {
                activeAudioSourceCount++;

                if (_audio == null)
                {
                    _audio = source;
                }
            }
        }

        if (_audio == null)
        {
            Debug.LogWarning("[AI_SERVICE] No active AudioSource found under: " + audioSourceSearchRoot.name);
            return;
        }

        if (activeAudioSourceCount > 1)
        {
            Debug.LogWarning("[AI_SERVICE] More than one active AudioSource found under " + audioSourceSearchRoot.name + ". Using: " + _audio.gameObject.name);
        }

        ConfigureAudioSource(_audio);

    }

    private void ConfigureAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;

        audioSource.playOnAwake = false;

    }

    public bool IsSpeaking()
    {
        if (_audio == null && refreshAudioSourceBeforeSpeaking)
        {
            RefreshActiveAudioSource();
        }

        return _audio != null && _audio.isPlaying;
    }

    public void Stop()
    {
        _currentRequestId++;

        if (_audio == null && refreshAudioSourceBeforeSpeaking)
        {
            RefreshActiveAudioSource();
        }

        if (_audio != null && _audio.isPlaying)
        {
            _audio.Stop();
        }
    }

    private IEnumerator SpeakRoutine(string text, int requestId)
    {
        string voice = useFemaleVoice ? femaleVoice : maleVoice;

        string cleanText = text.Trim()
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("?", "");

        TTSRequest requestData = new TTSRequest
        {
            text = cleanText,
            voice = voice
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (requestId != _currentRequestId)
            {
                yield break;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AI_SERVICE] FAILED! Error: " + req.error + " | Code: " + req.responseCode);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);

            if (clip != null && clip.length > 0)
            {
                if (_audio == null)
                {
                    RefreshActiveAudioSource();
                }

                if (_audio == null)
                {
                    Debug.LogError("[AI_SERVICE] Audio was generated, but no active AudioSource is available to play it.");
                    yield break;
                }

                _audio.clip = clip;
                _audio.Play();


            }
        }
    }

    public void TogglePause()
    {
        if (_audio == null && refreshAudioSourceBeforeSpeaking)
        {
            RefreshActiveAudioSource();
        }

        if (_audio == null) return;

        if (_audio.isPlaying)
        {
            _audio.Pause();

        }
        else if (_audio.clip != null && _audio.time > 0)
        {
            _audio.UnPause();

        }
    }

    public bool IsPaused()
    {
        if (_audio == null && refreshAudioSourceBeforeSpeaking)
        {
            RefreshActiveAudioSource();
        }

        return _audio != null && !_audio.isPlaying && _audio.clip != null && _audio.time > 0;
    }
}