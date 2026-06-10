using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


[RequireComponent(typeof(AudioSource))]
public class FreeExploreBoneNameSpeaker : MonoBehaviour
{
    [Header("Render TTS Endpoint")]
    [Tooltip("Render'daki TTS endpoint URL'i. Örn: https://vr-anatomy-backend2.onrender.com/tts")]
    [SerializeField] private string ttsEndpointUrl;

    [Tooltip("Backend MP3 dönüyorsa MPEG, WAV dönüyorsa WAV seç.")]
    [SerializeField] private AudioType responseAudioType = AudioType.MPEG;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Behavior")]
    [SerializeField] private bool cacheClips = true;
    [SerializeField] private bool stopCurrentBeforeSpeaking = true;

    private readonly Dictionary<string, AudioClip> _clipCache = new();
    private Coroutine _speakRoutine;

    [System.Serializable]
    private class TtsRequest
    {
        public string text;
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;

            // VR'da UI sesi gibi duyulsun, mesafeye göre kısılmasın.
            audioSource.spatialBlend = 0f;
        }
    }

    public void SpeakBoneName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        string textToSpeak = BuildBoneSentence(displayName);

        if (_speakRoutine != null)
        {
            StopCoroutine(_speakRoutine);
            _speakRoutine = null;
        }

        if (stopCurrentBeforeSpeaking && audioSource != null)
            audioSource.Stop();

        if (cacheClips && _clipCache.TryGetValue(textToSpeak, out AudioClip cachedClip) && cachedClip != null)
        {
            PlayClip(cachedClip);
            return;
        }

        _speakRoutine = StartCoroutine(RequestTtsAndPlay(textToSpeak));
    }

    public void StopSpeaking()
    {
        if (_speakRoutine != null)
        {
            StopCoroutine(_speakRoutine);
            _speakRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private string BuildBoneSentence(string displayName)
    {
        string name = displayName.Trim();
        string lower = name.ToLowerInvariant();

        // İsim zaten "Alın kemiği" gibi geliyorsa:
        // "Bu Alın kemiği kemiğidir." demesin diye koruma.
        if (lower.EndsWith("kemiği") || lower.EndsWith("kemigi"))
            return "Bu " + name + "dir.";

        // Örn: "Os frontale (Alın kemiği)" gibi bir isimse daha doğal dursun.
        if (lower.Contains("kemiği") || lower.Contains("kemigi"))
            return "Bu " + name + ".";

        return "Bu " + name + " kemiğidir.";
    }

    private IEnumerator RequestTtsAndPlay(string textToSpeak)
{
    if (string.IsNullOrWhiteSpace(ttsEndpointUrl))
    {
        Debug.LogWarning("[FreeExploreBoneNameSpeaker] TTS endpoint URL boş.");
        yield break;
    }

    string jsonBody = JsonUtility.ToJson(new TtsRequest
    {
        text = textToSpeak
    });

    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

    using (UnityWebRequest request = new UnityWebRequest(ttsEndpointUrl, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // Backend audio/mpeg döndürüyor, yani MP3.
        request.downloadHandler = new DownloadHandlerAudioClip(ttsEndpointUrl, AudioType.MPEG);

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/mpeg");

        yield return request.SendWebRequest();

        string contentType = request.GetResponseHeader("Content-Type");
        Debug.Log($"[FreeExploreBoneNameSpeaker] TTS response code={request.responseCode}, contentType={contentType}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[FreeExploreBoneNameSpeaker] TTS failed. Code={request.responseCode}, Error={request.error}"
            );
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

        if (clip == null)
        {
            Debug.LogWarning("[FreeExploreBoneNameSpeaker] MP3 response geldi ama AudioClip oluşturulamadı.");
            yield break;
        }

        clip.name = textToSpeak;

        if (cacheClips)
            _clipCache[textToSpeak] = clip;

        PlayClip(clip);
    }

    _speakRoutine = null;
}

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.clip = clip;
        audioSource.Play();
    }
}