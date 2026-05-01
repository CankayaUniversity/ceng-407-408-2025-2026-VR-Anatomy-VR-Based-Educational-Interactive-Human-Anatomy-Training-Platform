using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RagApiClient : MonoBehaviour
{
    private const string EmptyQuestionFeedback = "Devam etmek için lütfen bir soru yazın.";
    private const string NoAnswerFeedback = "Henüz bir cevap yok. Lütfen önce bir soru sorun.";
    private const string FemaleTtsVoice = "tr-TR-EmelNeural";
    private const string MaleTtsVoice = "tr-TR-AhmetNeural";
    private const string MaleTtsPitch = "+8%";
    private const string MaleTtsRate = "+0%";

    [Header("UI")]
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Button askButton;
    [SerializeField] private TMP_Text answerText;

    [Header("API")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:8000/docs/ask";

    [Header("Speech API")]
    [SerializeField] private string sttUrl = "http://127.0.0.1:8001/stt";
    [SerializeField] private string ttsUrl = "http://127.0.0.1:8001/tts";

    [Header("Kayıt Ayarları")]
    [SerializeField] private int maxRecordSeconds = 30;
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private float silenceThreshold = 0.01f;
    [SerializeField] private float silenceDurationToStop = 1.8f;
    [SerializeField] private int silenceCheckSampleWindow = 512;

    private Button _micButton;
    private Button _speakerButton;
    private Button _answerToggleButton;
    private TMP_Text _micLabel;
    private TMP_Text _speakerLabel;
    private TMP_Text _answerToggleLabel;
    private AudioSource _audio;

    private bool _isRecording;
    private bool _isAsking;
    private bool _isSttRunning;
    private AudioClip _recordingClip;
    private Color _defaultBtnColor;
    private Coroutine _ttsRoutine;
    private bool _isAnswerVisible;
    private string _latestAnswer = "";
    private string _questionDraftBeforeStt = "";
    private float _silenceTimer;
    private bool _hasDetectedSpeech;
    private float[] _silenceSampleBuffer;

    private static readonly Regex CevaplaCommandRegex =
        new Regex(@"\bcevapla\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MultiSpaceRegex =
        new Regex(@"\s+", RegexOptions.Compiled);

    [Serializable] private class AskRequest    { public string question; }
    [Serializable] private class AskResponse   { public string answer; }
    [Serializable] private class SttResponse    { public string text; }
    [Serializable] private class TtsPayload
    {
        public string text;
        public string voice;
        public string pitch;
        public string rate;
    }

    private void Awake()
    {
        askButton.onClick.AddListener(OnAskClicked);
        if (questionInput != null)
            questionInput.onValueChanged.AddListener(OnQuestionInputChanged);

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        CreateSpeechButtons();
        SetAnswerVisible(false);
        RefreshInteractableState();
        ShowIntroPanel();
    }

    private void OnDestroy()
    {
        askButton.onClick.RemoveListener(OnAskClicked);
        if (questionInput != null)
            questionInput.onValueChanged.RemoveListener(OnQuestionInputChanged);
        if (_isRecording) Microphone.End(null);
        if (_micButton != null) _micButton.onClick.RemoveAllListeners();
        if (_speakerButton != null) _speakerButton.onClick.RemoveAllListeners();
        if (_answerToggleButton != null) _answerToggleButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        if (!_isRecording || _recordingClip == null) return;

        // Cihazdan kayıt beklenmedik şekilde düşerse mevcut akışla finalize et.
        if (!Microphone.IsRecording(null))
        {
            StopRecording();
            return;
        }

        int micPosition = Microphone.GetPosition(null);
        if (micPosition <= 0) return;

        float micLevel = ReadMicLevel(micPosition);
        if (micLevel >= silenceThreshold)
        {
            _hasDetectedSpeech = true;
            _silenceTimer = 0f;
            return;
        }

        if (!_hasDetectedSpeech) return;

        _silenceTimer += Time.unscaledDeltaTime;
        if (_silenceTimer >= silenceDurationToStop)
        {
            StopRecording();
        }
    }

    #region Ask (mevcut fonksiyon)

    private void OnAskClicked()
    {
        if (_isAsking || _isSttRunning || _isRecording) return;

        string q = questionInput.text.Trim();

        if (string.IsNullOrEmpty(q))
        {
            ShowAnswerMessage(EmptyQuestionFeedback);
            return;
        }

        ShowAnswerMessage("Cevap hazırlanıyor...");
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
                ShowAnswerMessage("Sunucuya bağlanamadı. Lütfen bağlantınızı kontrol edip tekrar deneyin.");
                Debug.LogWarning($"[RagApiClient] Ask hatası: {req.error} | HTTP {req.responseCode}");
            }
            else
            {
                string responseJson = req.downloadHandler.text;
                try
                {
                    var resp = JsonUtility.FromJson<AskResponse>(responseJson);
                    ShowAnswerMessage(string.IsNullOrEmpty(resp?.answer)
                        ? "Cevap alınamadı, tekrar deneyin."
                        : resp.answer);
                }
                catch
                {
                    ShowAnswerMessage("Sunucudan geçersiz cevap geldi, tekrar deneyin.");
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

        // "Cevabı Gör" butonu — chatbox'ın altında, geniş tasarım
        _answerToggleButton = CloneButton(askButton, parent, "AnswerToggleButton");
        _answerToggleLabel = _answerToggleButton.GetComponentInChildren<TMP_Text>();
        _answerToggleLabel.text = "Cevabı Gör";

        RectTransform toggleRT = _answerToggleButton.GetComponent<RectTransform>();
        PositionAndStyleAnswerToggle(toggleRT, askRT);
        _answerToggleButton.onClick.AddListener(OnAnswerToggleClicked);
    }

    private void PositionAndStyleAnswerToggle(RectTransform toggleRT, RectTransform askRT)
    {
        if (toggleRT == null) return;

        RectTransform questionRT = questionInput != null ? questionInput.GetComponent<RectTransform>() : null;
        RectTransform answerRT = answerText != null ? answerText.GetComponent<RectTransform>() : null;

        // Kompakt boyut: chatbox ile hizalı ama bar gibi değil.
        float chatWidthRef = questionRT != null ? questionRT.sizeDelta.x :
            (answerRT != null ? answerRT.sizeDelta.x : 420f);
        float width = Mathf.Clamp(chatWidthRef * 0.42f, 210f, 300f);
        float height = Mathf.Clamp(askRT.sizeDelta.y * 0.9f, 38f, 46f);
        toggleRT.sizeDelta = new Vector2(width, height);

        // Chatbox'ın hemen altına, yatayda ortalı yerleşim.
        if (questionRT != null)
        {
            float yOffset = questionRT.anchoredPosition.y - (questionRT.sizeDelta.y * 0.5f) - (height * 0.5f) - 14f;
            toggleRT.anchoredPosition = new Vector2(questionRT.anchoredPosition.x, yOffset);
        }
        else if (answerRT != null)
        {
            float yOffset = answerRT.anchoredPosition.y - (answerRT.sizeDelta.y * 0.5f) - (height * 0.5f) - 12f;
            toggleRT.anchoredPosition = new Vector2(answerRT.anchoredPosition.x, yOffset);
        }
        else
        {
            toggleRT.anchoredPosition = new Vector2(askRT.anchoredPosition.x - 90f, askRT.anchoredPosition.y - 125f);
        }

        // Tasarımsal iyileştirme: futuristik cyan tonları + yumuşak state geçişleri.
        Image img = _answerToggleButton != null ? _answerToggleButton.GetComponent<Image>() : null;
        if (img != null)
        {
            Color normal = new Color(0.06f, 0.40f, 0.62f, 0.92f);
            Color highlighted = new Color(0.10f, 0.52f, 0.77f, 0.97f);
            Color pressed = new Color(0.05f, 0.33f, 0.52f, 0.96f);
            img.color = normal;

            var colors = _answerToggleButton.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.disabledColor = new Color(0.06f, 0.22f, 0.32f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            _answerToggleButton.colors = colors;
        }

        if (_answerToggleLabel != null)
        {
            _answerToggleLabel.fontSize = 25f;
            _answerToggleLabel.alignment = TextAlignmentOptions.Center;
            _answerToggleLabel.enableWordWrapping = false;
            _answerToggleLabel.overflowMode = TextOverflowModes.Truncate;
            _answerToggleLabel.color = new Color(0.92f, 0.98f, 1f, 1f);
        }

        PositionAnswerTextBelowToggle(toggleRT, answerRT, questionRT);
    }

    private void PositionAnswerTextBelowToggle(
        RectTransform toggleRT,
        RectTransform answerRT,
        RectTransform questionRT)
    {
        if (toggleRT == null || answerRT == null) return;

        RectTransform questionTextRT = (questionInput != null && questionInput.textComponent != null)
            ? questionInput.textComponent.rectTransform
            : null;
        RectTransform parentRT = answerRT.parent as RectTransform;
        if (parentRT == null) return;

        float gap = 16f;
        float answerHeight = Mathf.Max(answerRT.sizeDelta.y, 260f);

        // Toggle butonunun alt kenarını parent uzayına çevir.
        Vector3[] toggleWorld = new Vector3[4];
        toggleRT.GetWorldCorners(toggleWorld);
        Vector2 toggleBottomLocal = parentRT.InverseTransformPoint(toggleWorld[0]);
        float y = toggleBottomLocal.y - (answerHeight * 0.5f) - gap;

        // Cevap bloğu chatbox alt-solundan başlayıp sağa kadar uzasın.
        // Metin sola hizalı, dar sütun olmayacak.
        if (questionRT != null)
        {
            Vector3[] questionBoxWorld = new Vector3[4];
            questionRT.GetWorldCorners(questionBoxWorld);
            Vector2 questionLeftLocal = parentRT.InverseTransformPoint(questionBoxWorld[0]);
            Vector2 questionRightLocal = parentRT.InverseTransformPoint(questionBoxWorld[3]);

            const float leftInset = 10f;   // biraz daha soldan başlasın
            const float rightInset = 8f;   // sağa daha geç bitsin
            float leftStart = questionLeftLocal.x + leftInset;
            float rightEnd = questionRightLocal.x - rightInset;
            float answerWidth = Mathf.Clamp(rightEnd - leftStart, 540f, 1040f);
            answerRT.anchorMin = new Vector2(0.5f, 0.5f);
            answerRT.anchorMax = new Vector2(0.5f, 0.5f);
            answerRT.pivot = new Vector2(0f, 0.5f);
            answerRT.sizeDelta = new Vector2(answerWidth, answerHeight);
            answerRT.anchoredPosition = new Vector2(leftStart, y);
        }
        else if (questionTextRT != null)
        {
            Vector3[] questionTextWorld = new Vector3[4];
            questionTextRT.GetWorldCorners(questionTextWorld);
            Vector2 textLeftLocal = parentRT.InverseTransformPoint(questionTextWorld[0]);
            Vector2 textRightLocal = parentRT.InverseTransformPoint(questionTextWorld[3]);
            float answerWidth = Mathf.Clamp(textRightLocal.x - textLeftLocal.x, 520f, 980f);
            answerRT.anchorMin = new Vector2(0.5f, 0.5f);
            answerRT.anchorMax = new Vector2(0.5f, 0.5f);
            answerRT.pivot = new Vector2(0f, 0.5f);
            answerRT.sizeDelta = new Vector2(answerWidth, answerHeight);
            answerRT.anchoredPosition = new Vector2(textLeftLocal.x, y);
        }
        else
        {
            answerRT.anchoredPosition = new Vector2(toggleRT.anchoredPosition.x, y);
        }
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
        if (_isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            SetLatestAnswer("Mikrofon bulunamadı ❌");
            return;
        }

        _recordingClip = Microphone.Start(null, false, maxRecordSeconds, sampleRate);
        _isRecording = true;
        _silenceTimer = 0f;
        _hasDetectedSpeech = false;
        _micLabel.text = "Dur";
        _micButton.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f);
        RefreshInteractableState();
    }

    private void StopRecording()
    {
        if (!_isRecording) return;

        int pos = Microphone.GetPosition(null);
        Microphone.End(null);
        _isRecording = false;
        _silenceTimer = 0f;
        _hasDetectedSpeech = false;
        _micLabel.text = "Konuş";
        _micButton.GetComponent<Image>().color = _defaultBtnColor;
        RefreshInteractableState();

        if (pos <= 0 || _recordingClip == null)
        {
            SetLatestAnswer("Ses algılanamadı, tekrar deneyin.");
            return;
        }

        float[] samples = new float[pos * _recordingClip.channels];
        _recordingClip.GetData(samples, 0);

        AudioClip trimmed = AudioClip.Create("rec", pos,
            _recordingClip.channels, sampleRate, false);
        trimmed.SetData(samples, 0);

        byte[] wav = EncodeToWav(trimmed);
        _questionDraftBeforeStt = questionInput != null ? questionInput.text.Trim() : "";
        questionInput.text = "Konuşma algılanıyor...";
        StartCoroutine(RequestSTT(wav));
    }

    private float ReadMicLevel(int micPosition)
    {
        if (_recordingClip == null || micPosition <= 0) return 0f;

        int channels = Mathf.Max(1, _recordingClip.channels);
        int frameCount = Mathf.Clamp(silenceCheckSampleWindow, 64, 4096);
        frameCount = Mathf.Min(frameCount, micPosition);
        if (frameCount <= 0) return 0f;

        int sampleCount = frameCount * channels;
        if (_silenceSampleBuffer == null || _silenceSampleBuffer.Length != sampleCount)
            _silenceSampleBuffer = new float[sampleCount];

        int startFrame = micPosition - frameCount;
        _recordingClip.GetData(_silenceSampleBuffer, startFrame);

        float sum = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            sum += Mathf.Abs(_silenceSampleBuffer[i]);
        }

        return sampleCount > 0 ? (sum / sampleCount) : 0f;
    }

    private IEnumerator RequestSTT(byte[] wavData)
    {
        _isSttRunning = true;
        RefreshInteractableState();
        bool triggerAutoAsk = false;

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
                    SetLatestAnswer("Konuşma anlaşılamadı, tekrar deneyin.");
                }
                else
                {
                    bool hasCevapla = ContainsCevaplaCommand(recognized);
                    string cleaned = RemoveCevaplaCommand(recognized);

                    if (hasCevapla)
                    {
                        // "sadece cevapla" dendiğinde mevcut input draft'ını kullan.
                        if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            string draft = _questionDraftBeforeStt;
                            if (string.IsNullOrWhiteSpace(draft) || draft == "Konuşma algılanıyor...")
                                draft = questionInput != null ? questionInput.text.Trim() : "";

                            if (string.IsNullOrWhiteSpace(draft) || draft == "Konuşma algılanıyor...")
                            {
                                SetLatestAnswer("Gönderilecek bir soru bulunamadı. Önce sorunuzu söyleyin veya yazın.");
                                questionInput.text = "";
                            }
                            else
                            {
                                questionInput.text = draft;
                                triggerAutoAsk = true;
                            }
                        }
                        else
                        {
                            questionInput.text = cleaned;
                            triggerAutoAsk = true;
                        }
                    }
                    else
                    {
                        // Sadece soru söylendiyse input'a yaz, otomatik gönderme.
                        questionInput.text = recognized.Trim();
                        SetLatestAnswer("Sorunuz hazır. Göndermek için 'cevapla' deyin veya Sor butonuna basın.");
                    }
                }
            }
            else
            {
                questionInput.text = "";
                SetLatestAnswer("Sunucuya bağlanamadı. Konuşma algılanamadı.");
                Debug.LogWarning($"[RagApiClient] STT hatası: {req.error}");
            }
        }
        finally
        {
            req?.Dispose();
            _isSttRunning = false;
            RefreshInteractableState();
            _questionDraftBeforeStt = "";
        }

        if (triggerAutoAsk)
        {
            // STT bittiğinde Sor butonuna basılmış gibi gönder.
            OnAskClicked();
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

        string text = _latestAnswer;
        if (string.IsNullOrEmpty(text)
            || text.StartsWith("Cevap burada")
            || text.StartsWith("Cevap hazırlanıyor")
            || text.StartsWith("Düşünüyorum")
            || text.StartsWith("Bir soru yaz")
            || text.StartsWith("Devam etmek için")
            || text.StartsWith("Henüz gösterilecek"))
            return;

        if (_ttsRoutine != null) StopCoroutine(_ttsRoutine);
        _ttsRoutine = StartCoroutine(RequestTTS(text));
    }

    private IEnumerator RequestTTS(string text)
    {
        _speakerLabel.text = "...";

        bool isMaleAvatar = IsMaleAvatarSelected();
        TtsPayload payload = new TtsPayload
        {
            text = text,
            voice = isMaleAvatar ? MaleTtsVoice : FemaleTtsVoice,
            pitch = isMaleAvatar ? MaleTtsPitch : null,
            rate = isMaleAvatar ? MaleTtsRate : null
        };
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

    private bool IsMaleAvatarSelected()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.SelectedAvatarType == SettingsManager.AvatarType.Male;

        int rawValue = PlayerPrefs.GetInt("AvatarType", (int)SettingsManager.AvatarType.Female);
        return rawValue == (int)SettingsManager.AvatarType.Male;
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

    private static bool ContainsCevaplaCommand(string text)
    {
        return !string.IsNullOrWhiteSpace(text) && CevaplaCommandRegex.IsMatch(text);
    }

    private static string RemoveCevaplaCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string stripped = CevaplaCommandRegex.Replace(text, " ");
        stripped = MultiSpaceRegex.Replace(stripped, " ").Trim();
        stripped = stripped.Trim(' ', ',', '.', ';', ':', '!', '?', '-', '_', '/', '\\', '\"', '\'');
        return stripped;
    }

    private void RefreshInteractableState()
    {
        bool canAsk = !_isAsking && !_isSttRunning && !_isRecording;
        if (askButton != null) askButton.interactable = canAsk;
        if (_micButton != null) _micButton.interactable = !_isAsking && !_isSttRunning;
        if (_speakerButton != null) _speakerButton.interactable = !_isAsking && !_isSttRunning && !_isRecording;
        if (_answerToggleButton != null) _answerToggleButton.interactable = true;
    }

    private void OnAnswerToggleClicked()
    {
        SetAnswerVisible(!_isAnswerVisible);
        if (_isAnswerVisible && string.IsNullOrWhiteSpace(_latestAnswer))
            SetLatestAnswer(NoAnswerFeedback);
    }

    private void SetAnswerVisible(bool visible)
    {
        _isAnswerVisible = visible;
        if (_answerToggleLabel != null)
            _answerToggleLabel.text = visible ? "Cevabı Gizle" : "Cevabı Gör";

        if (answerText != null)
        {
            answerText.gameObject.SetActive(visible);
            if (visible)
            {
                RectTransform toggleRT = _answerToggleButton != null
                    ? _answerToggleButton.GetComponent<RectTransform>() : null;
                RectTransform answerRT = answerText.GetComponent<RectTransform>();
                RectTransform questionRT = questionInput != null
                    ? questionInput.GetComponent<RectTransform>() : null;
                PositionAnswerTextBelowToggle(toggleRT, answerRT, questionRT);

                answerText.enableWordWrapping = true;
                answerText.enableAutoSizing = false;
                answerText.fontSize = Mathf.Clamp(answerText.fontSize, 28f, 36f);
                answerText.alignment = TextAlignmentOptions.TopLeft;
                answerText.overflowMode = TextOverflowModes.Overflow;
                answerText.lineSpacing = 2f;
                answerText.text = string.IsNullOrEmpty(_latestAnswer)
                    ? ""
                    : FormatAnswerForDisplay(_latestAnswer);
            }
            else
            {
                answerText.text = "";
            }
        }
    }

    private void SetLatestAnswer(string text)
    {
        _latestAnswer = text ?? "";
        if (_isAnswerVisible && answerText != null)
            answerText.text = FormatAnswerForDisplay(_latestAnswer);
    }

    private void ShowAnswerMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ClearAnswerState();
            return;
        }

        SetLatestAnswer(text);
    }

    private void ClearAnswerState()
    {
        _latestAnswer = "";
        if (answerText != null)
            answerText.text = "";
    }

    private void OnQuestionInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ClearAnswerState();
    }

    private void ShowIntroPanel()
    {
        Canvas canvas = askButton != null ? askButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;

        var hideList = new List<GameObject>();
        if (askButton != null) hideList.Add(askButton.gameObject);
        if (_micButton != null) hideList.Add(_micButton.gameObject);
        if (_speakerButton != null) hideList.Add(_speakerButton.gameObject);
        if (_answerToggleButton != null) hideList.Add(_answerToggleButton.gameObject);
        if (questionInput != null) hideList.Add(questionInput.gameObject);

        GameObject answerGroup = answerText != null && answerText.transform.parent != null
            ? answerText.transform.parent.gameObject : null;
        if (answerGroup != null) hideList.Add(answerGroup);

        GameObject avatar = FindChatAvatar();
        if (avatar != null) hideList.Add(avatar);

        Sprite panelSprite = ResolvePanelSprite();
        TMP_FontAsset font = ResolveTmpFontAsset();
        GameObject titleLogo = ResolveTitleLogo();

        var intro = gameObject.AddComponent<AIChatIntroPanel>();
        intro.Show(canvas, panelSprite, font, titleLogo, hideList, OnIntroContinue);
    }

    private void OnIntroContinue()
    {
        // Chatbox'a geri dönünce cevap alanı kapalı kalmalı, toggle'a basılınca açılsın.
        SetAnswerVisible(false);
        RefreshInteractableState();
    }

    private GameObject FindChatAvatar()
    {
        var go = GameObject.Find("ChatAvatar");
        if (go != null) return go;

        var controller = FindObjectOfType<ChatAvatarController>(true);
        return controller != null ? controller.gameObject : null;
    }

    private Sprite ResolvePanelSprite()
    {
        // Quiz intro paneli ile birebir aynı 9-slice sprite'ı paylaşmak için sahnedeki
        // chat kutusunun (QuestionText) Image'ından atlas sprite'ını alıyoruz.
        if (questionInput != null)
        {
            var img = questionInput.GetComponent<Image>();
            if (img != null && img.sprite != null) return img.sprite;
        }
        return null;
    }

    private TMP_FontAsset ResolveTmpFontAsset()
    {
        if (askButton != null)
        {
            var label = askButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.font != null) return label.font;
        }
        if (answerText != null && answerText.font != null) return answerText.font;
        return null;
    }

    private GameObject ResolveTitleLogo()
    {
        return GameObject.Find("logo");
    }

    private string FormatAnswerForDisplay(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        string normalized = raw.Replace("\r\n", "\n");
        string[] lines = normalized.Split('\n');
        var entries = new System.Collections.Generic.List<string>();

        foreach (string line in lines)
        {
            string t = line.Trim();
            if (string.IsNullOrEmpty(t)) continue;

            if (t.StartsWith("- ")) t = t.Substring(2).Trim();
            else if (t.StartsWith("• ")) t = t.Substring(2).Trim();
            else if (t.StartsWith("– ")) t = t.Substring(2).Trim();

            entries.Add(t);
        }



        // Premium bullet + hanging indent:
        // Alt satır bullet'ın altına değil, metnin başladığı hattan devam eder.
        const string prefix = "<indent=34px><line-indent=-34px><color=#33CFFF><b>></b></color><space=10px>";
        const string suffix = "</line-indent></indent>";
        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            sb.Append(prefix);
            sb.Append(entries[i]);
            sb.Append(suffix);
            if (i < entries.Count - 1)
                sb.Append("\n");
        }
        return sb.ToString();
    }
}
