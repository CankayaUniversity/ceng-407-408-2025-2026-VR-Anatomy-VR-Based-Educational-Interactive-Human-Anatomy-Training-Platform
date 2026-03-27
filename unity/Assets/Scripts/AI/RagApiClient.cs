using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RagApiClient : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Button askButton;
    [SerializeField] private TMP_Text answerText;

    [Header("API")]
    [SerializeField] private string apiUrl = "https://samuel-critics-knew-explorer.trycloudflare.com/ask";

    [Header("Speech API")]
    [SerializeField] private string sttUrl = "http://127.0.0.1:8001/stt";
    [SerializeField] private string ttsUrl = "http://127.0.0.1:8001/tts";

    [Header("Kayıt Ayarları")]
    [SerializeField] private int maxRecordSeconds = 30;
    [SerializeField] private int sampleRate = 16000;

    private Button _micButton;
    private Button _speakerButton;
    private TMP_Text _micLabel;
    private TMP_Text _speakerLabel;
    private AudioSource _audio;

    private bool _isRecording;
    private bool _isAsking;
    private bool _isSttRunning;
    private AudioClip _recordingClip;
    private Color _defaultBtnColor;
    private Coroutine _ttsRoutine;

    [Serializable] private class AskRequest    { public string question; }
    [Serializable] private class AskResponse   { public string answer; }
    [Serializable] private class SttResponse    { public string text; }
    [Serializable] private class TtsPayload     { public string text; }

    private void Awake()
    {
        askButton.onClick.AddListener(OnAskClicked);

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        CreateSpeechButtons();
        RefreshInteractableState();
    }

    private void OnDestroy()
    {
        askButton.onClick.RemoveListener(OnAskClicked);
        if (_isRecording) Microphone.End(null);
        if (_micButton != null) _micButton.onClick.RemoveAllListeners();
        if (_speakerButton != null) _speakerButton.onClick.RemoveAllListeners();
    }

    #region Ask (mevcut fonksiyon)

    private void OnAskClicked()
    {
        if (_isAsking || _isSttRunning || _isRecording) return;

        string q = questionInput.text.Trim();

        if (string.IsNullOrEmpty(q))
        {
            answerText.text = "Bir soru yaz da öyle bas 🙃";
            return;
        }

        answerText.text = "Düşünüyorum... 🤔";
        StartCoroutine(SendQuestion(q));
    }

    private IEnumerator SendQuestion(string question)
    {
        _isAsking = true;
        RefreshInteractableState();

        var payload = new AskRequest { question = question };
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = null;
        try
        {
            req = new UnityWebRequest(apiUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                answerText.text = "Sunucuya bağlanamadı. Lütfen bağlantınızı kontrol edip tekrar deneyin.";
                Debug.LogWarning($"[RagApiClient] Ask hatası: {req.error} | HTTP {req.responseCode}");
            }
            else
            {
                string responseJson = req.downloadHandler.text;
                try
                {
                    var resp = JsonUtility.FromJson<AskResponse>(responseJson);
                    answerText.text = string.IsNullOrEmpty(resp?.answer)
                        ? "Cevap alınamadı, tekrar deneyin."
                        : resp.answer;
                }
                catch
                {
                    answerText.text = "Sunucudan geçersiz cevap geldi, tekrar deneyin.";
                }
            }
        }
        finally
        {
            req?.Dispose();
            _isAsking = false;
            RefreshInteractableState();
        }
    }

    #endregion

    #region Speech Button Creation

    private void CreateSpeechButtons()
    {
        if (askButton == null) return;

        Transform parent = askButton.transform.parent;
        RectTransform askRT = askButton.GetComponent<RectTransform>();
        _defaultBtnColor = askButton.GetComponent<Image>().color;

        // "Konuş" butonu — "Sor" butonunun hemen altında
        _micButton = CloneButton(askButton, parent, "MicButton");
        _micLabel = _micButton.GetComponentInChildren<TMP_Text>();
        _micLabel.text = "Konuş";

        RectTransform micRT = _micButton.GetComponent<RectTransform>();
        micRT.anchoredPosition = new Vector2(
            askRT.anchoredPosition.x,
            askRT.anchoredPosition.y - askRT.sizeDelta.y - 12f
        );
        _micButton.onClick.AddListener(OnMicClicked);

        // "Dinle" butonu — "Konuş" butonunun hemen altında
        _speakerButton = CloneButton(askButton, parent, "SpeakerButton");
        _speakerLabel = _speakerButton.GetComponentInChildren<TMP_Text>();
        _speakerLabel.text = "Dinle";

        RectTransform spkRT = _speakerButton.GetComponent<RectTransform>();
        spkRT.anchoredPosition = new Vector2(
            micRT.anchoredPosition.x,
            micRT.anchoredPosition.y - askRT.sizeDelta.y - 12f
        );
        _speakerButton.onClick.AddListener(OnSpeakerClicked);
    }

    private Button CloneButton(Button template, Transform parent, string goName)
    {
        GameObject go = Instantiate(template.gameObject, parent);
        go.name = goName;
        Button btn = go.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        return btn;
    }

    #endregion

    #region STT – Mikrofon Kaydı

    private void OnMicClicked()
    {
        if (_isAsking || _isSttRunning) return;
        if (_isRecording) StopRecording();
        else              StartRecording();
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            answerText.text = "Mikrofon bulunamadı ❌";
            return;
        }

        _recordingClip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
        _isRecording = true;
        _micLabel.text = "Dur";
        _micButton.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f);
        RefreshInteractableState();
    }

    private void StopRecording()
    {
        int pos = Microphone.GetPosition(null);
        Microphone.End(null);
        _isRecording = false;
        _micLabel.text = "Konuş";
        _micButton.GetComponent<Image>().color = _defaultBtnColor;
        RefreshInteractableState();

        if (pos <= 0 || _recordingClip == null)
        {
            answerText.text = "Ses algılanamadı, tekrar deneyin.";
            return;
        }

        float[] samples = new float[pos * _recordingClip.channels];
        _recordingClip.GetData(samples, 0);

        AudioClip trimmed = AudioClip.Create("rec", pos,
            _recordingClip.channels, sampleRate, false);
        trimmed.SetData(samples, 0);

        byte[] wav = EncodeToWav(trimmed);
        questionInput.text = "Konuşma algılanıyor...";
        StartCoroutine(RequestSTT(wav));
    }

    private IEnumerator RequestSTT(byte[] wavData)
    {
        _isSttRunning = true;
        RefreshInteractableState();

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");

        UnityWebRequest req = null;
        try
        {
            req = UnityWebRequest.Post(sttUrl, form);
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                SttResponse resp =
                    JsonUtility.FromJson<SttResponse>(req.downloadHandler.text);
                string recognized = resp?.text ?? "";

                if (string.IsNullOrEmpty(recognized))
                {
                    questionInput.text = "";
                    answerText.text = "Konuşma anlaşılamadı, tekrar deneyin.";
                }
                else
                {
                    questionInput.text = recognized;
                }
            }
            else
            {
                questionInput.text = "";
                answerText.text = "Sunucuya bağlanamadı. Konuşma algılanamadı.";
                Debug.LogWarning($"[RagApiClient] STT hatası: {req.error}");
            }
        }
        finally
        {
            req?.Dispose();
            _isSttRunning = false;
            RefreshInteractableState();
        }
    }

    #endregion

    #region TTS – Cevabı Sesli Oku

    private void OnSpeakerClicked()
    {
        if (_isAsking || _isSttRunning || _isRecording) return;

        if (_audio.isPlaying)
        {
            _audio.Stop();
            _speakerLabel.text = "Dinle";
            return;
        }

        string text = answerText != null ? answerText.text : "";
        if (string.IsNullOrEmpty(text)
            || text.StartsWith("Cevap burada")
            || text.StartsWith("Düşünüyorum")
            || text.StartsWith("Bir soru yaz"))
            return;

        if (_ttsRoutine != null) StopCoroutine(_ttsRoutine);
        _ttsRoutine = StartCoroutine(RequestTTS(text));
    }

    private IEnumerator RequestTTS(string text)
    {
        _speakerLabel.text = "...";

        TtsPayload payload = new TtsPayload { text = text };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _speakerLabel.text = "Dinle";
                answerText.text = "Sunucuya bağlanamadı. Sesli okuma yapılamadı.";
                Debug.LogWarning($"[RagApiClient] TTS hatası: {req.error}");
                yield break;
            }

            string tmpPath = Path.Combine(
                Application.temporaryCachePath, "tts_response.mp3");
            File.WriteAllBytes(tmpPath, req.downloadHandler.data);

            string fileUrl = "file:///" + tmpPath.Replace("\\", "/");

            using (UnityWebRequest audioReq =
                       UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
            {
                yield return audioReq.SendWebRequest();

                if (audioReq.result == UnityWebRequest.Result.Success)
                {
                    _audio.clip = DownloadHandlerAudioClip.GetContent(audioReq);
                    _audio.Play();
                    _speakerLabel.text = "Dur";

                    while (_audio.isPlaying)
                        yield return null;

                    _speakerLabel.text = "Dinle";
                }
                else
                {
                    _speakerLabel.text = "Dinle";
                    Debug.LogWarning(
                        $"[RagApiClient] Ses dosyası yüklenemedi: {audioReq.error}");
                }
            }
        }
    }

    #endregion

    #region WAV Encoder

    private static byte[] EncodeToWav(AudioClip clip)
    {
        float[] data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter w = new BinaryWriter(ms))
        {
            int   totalSamples = data.Length;
            short channels     = (short)clip.channels;
            int   freq         = clip.frequency;
            short bits         = 16;
            int   byteRate     = freq * channels * bits / 8;
            short blockAlign   = (short)(channels * bits / 8);
            int   dataSize     = totalSamples * blockAlign;

            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataSize);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));

            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write(channels);
            w.Write(freq);
            w.Write(byteRate);
            w.Write(blockAlign);
            w.Write(bits);

            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(dataSize);

            for (int i = 0; i < totalSamples; i++)
            {
                short s = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);
                w.Write(s);
            }

            return ms.ToArray();
        }
    }

    #endregion

    private void RefreshInteractableState()
    {
        bool canAsk = !_isAsking && !_isSttRunning && !_isRecording;
        if (askButton != null) askButton.interactable = canAsk;
        if (_micButton != null) _micButton.interactable = !_isAsking && !_isSttRunning;
        if (_speakerButton != null) _speakerButton.interactable = !_isAsking && !_isSttRunning && !_isRecording;
    }
}
