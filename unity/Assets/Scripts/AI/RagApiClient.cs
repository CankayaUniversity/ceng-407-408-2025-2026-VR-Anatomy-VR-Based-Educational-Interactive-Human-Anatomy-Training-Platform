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
    private const float ChatUiTargetWorldX = 0f;
    private const float ChatAvatarLeftShift = 0.35f;

    [Header("UI")]
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Button askButton;
    [SerializeField] private TMP_Text answerText;

    [Header("Answer Toggle Layout")]
    [SerializeField] private Vector2 answerToggleOffset = Vector2.zero;
    [SerializeField] private Vector2 answerToggleSizeOverride = Vector2.zero;

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
    private static Sprite _runtimeRoundedSprite;
    private bool _chatUiRedesigned;

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

        if (answerToggleSizeOverride.x > 0f)
            width = answerToggleSizeOverride.x;

        if (answerToggleSizeOverride.y > 0f)
            height = answerToggleSizeOverride.y;

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
        toggleRT.anchoredPosition += answerToggleOffset;

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
    return;

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
                answerText.fontSize = Mathf.Clamp(answerText.fontSize, 23f, 26f);
                answerText.alignment = TextAlignmentOptions.TopLeft;
                answerText.overflowMode = TextOverflowModes.Truncate;
                answerText.lineSpacing = 2f;
                answerText.margin = new Vector4(0f, 0f, 0f, 0f);
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
        // Sadece intro kapandıktan sonra chatbox ekranını yeniden tasarla.
        // Intro / bilgilendirme paneline dokunmaz.
        ApplyChatUiRedesign();

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

    private void ApplyChatUiRedesign()
    {
        if (_chatUiRedesigned) return;
        _chatUiRedesigned = true;

        RectTransform questionRT = questionInput != null
            ? questionInput.GetComponent<RectTransform>()
            : null;

        RectTransform answerRT = answerText != null
            ? answerText.GetComponent<RectTransform>()
            : null;

        RectTransform answerPanelRT = answerRT != null
            ? answerRT.parent as RectTransform
            : null;

        // =========================
        // 1) ÜST SORU / INPUT PANELİ
        // =========================
        if (questionInput != null && questionRT != null)
        {
            questionRT.anchorMin = new Vector2(0.5f, 0.5f);
            questionRT.anchorMax = new Vector2(0.5f, 0.5f);
            questionRT.pivot = new Vector2(0.5f, 0.5f);
            questionRT.localScale = Vector3.one;

            questionRT.anchoredPosition = new Vector2(
                ResolveAnchoredXForWorldCenter(questionRT.parent as RectTransform, ChatUiTargetWorldX),
                questionRT.anchoredPosition.y);
            questionRT.sizeDelta = new Vector2(1000f, 88f);

            Image questionImage = questionInput.GetComponent<Image>();
            if (questionImage != null)
            {
                StyleChatPanel(
                    questionImage,
                    new Color(0.95f, 0.985f, 1f, 0.96f),
                    new Color(0.40f, 0.84f, 1f, 0.98f),
                    new Color(0.00f, 0.76f, 1.00f, 0.34f));
            }

            CreateOrUpdatePanelGlow(questionRT, "QuestionOuterGlow", new Color(0.20f, 0.86f, 1f, 0.16f), 16f);
            CreateOrUpdateSoftInputFrame(questionInput.transform);
            CreateOrUpdateInputBadge(questionInput.transform, "InputChatIcon", "", false);
            CreateOrUpdateInputBadge(questionInput.transform, "InputSendIcon", ">", true);
            CreateOrUpdatePanelAccentLine(questionRT, "QuestionAccentLine", new Color(0.64f, 0.92f, 1f, 0.48f), 2f, 28f);

            RectTransform textArea = questionInput.transform.Find("Text Area") as RectTransform;
            if (textArea != null)
            {
                textArea.anchorMin = Vector2.zero;
                textArea.anchorMax = Vector2.one;
                textArea.pivot = new Vector2(0.5f, 0.5f);
                textArea.offsetMin = new Vector2(10, 14f);
                textArea.offsetMax = new Vector2(-92f, -14f);
            }

            ApplyInputTextStyle(questionInput.textComponent, false);
            ApplyInputTextStyle(questionInput.placeholder as TMP_Text, true);
        }

        // =========================
        // 2) ALT CEVAP PANELİ
        // =========================
        if (answerPanelRT != null)
        {
            answerPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
            answerPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            answerPanelRT.pivot = new Vector2(0.5f, 1f);
            answerPanelRT.localScale = Vector3.one;
            answerPanelRT.sizeDelta = new Vector2(1000f, 420f);

            if (questionRT != null && answerPanelRT.parent is RectTransform answerParentRT)
            {
                Vector3[] qCorners = new Vector3[4];
                questionRT.GetWorldCorners(qCorners);
                Vector3 questionBottomCenterWorld = (qCorners[0] + qCorners[3]) * 0.5f;
                Vector2 questionBottomCenterLocal = answerParentRT.InverseTransformPoint(questionBottomCenterWorld);
                const float gap = 10f;
                answerPanelRT.anchoredPosition = questionBottomCenterLocal + new Vector2(0f, -gap);
            }

            Image answerImage = answerPanelRT.GetComponent<Image>();
            if (answerImage != null)
            {
                StyleChatPanel(
                    answerImage,
                    new Color(0.74f, 0.93f, 1f, 0.78f),
                    new Color(0.30f, 0.84f, 1f, 0.98f),
                    new Color(0.00f, 0.72f, 1.00f, 0.30f));
            }

            CreateOrUpdatePanelGlow(answerPanelRT, "AnswerOuterGlow", new Color(0.18f, 0.82f, 1f, 0.22f), 26f);
            CreateOrUpdateAnswerInnerFrame(answerPanelRT);
            CreateOrUpdateAnswerBottomGlow(answerPanelRT);
            CreateOrUpdateAnswerShine(answerPanelRT);
            CreateOrUpdateAnswerHeader(answerPanelRT);
            CreateOrUpdateAnswerDecor(answerPanelRT);

            RectMask2D mask = answerPanelRT.GetComponent<RectMask2D>();
            if (mask == null)
                mask = answerPanelRT.gameObject.AddComponent<RectMask2D>();

            RectTransform contentHost = EnsureAnswerContentHost(answerPanelRT);

            if (answerText != null)
            {
                if (answerText.transform.parent != contentHost)
                    answerText.transform.SetParent(contentHost, false);

                answerRT = answerText.GetComponent<RectTransform>();
                answerRT.anchorMin = Vector2.zero;
                answerRT.anchorMax = Vector2.one;
                answerRT.pivot = new Vector2(0.5f, 0.5f);
                answerRT.localScale = Vector3.one;
                answerRT.offsetMin = Vector2.zero;
                answerRT.offsetMax = Vector2.zero;
            }
        }

        if (answerText != null)
        {
            answerText.color = new Color(0.08f, 0.27f, 0.40f, 1f);
            answerText.fontSize = 27f;
            answerText.alignment = TextAlignmentOptions.TopLeft;
            answerText.enableWordWrapping = true;
            answerText.enableAutoSizing = false;
            answerText.overflowMode = TextOverflowModes.Overflow;
            answerText.lineSpacing = 4f;
            answerText.margin = new Vector4(0f, 0f, 0f, 0f);
        }

        if (_answerToggleButton != null && askButton != null)
        {
            PositionAndStyleAnswerToggle(
                _answerToggleButton.GetComponent<RectTransform>(),
                askButton.GetComponent<RectTransform>());
        }

        AlignTitleToChatCenter();
        ShiftChatAvatarLeft();
    }

    private static float ResolveAnchoredXForWorldCenter(RectTransform parentRT, float targetWorldX)
    {
        if (parentRT == null) return 0f;

        Vector3 targetWorldPosition = new Vector3(targetWorldX, parentRT.position.y, parentRT.position.z);
        return parentRT.InverseTransformPoint(targetWorldPosition).x;
    }

    private void AlignTitleToChatCenter()
    {
        GameObject titleLogo = ResolveTitleLogo();
        if (titleLogo == null) return;

        Transform titleTransform = titleLogo.transform;
        Vector3 pos = titleTransform.position;
        titleTransform.position = new Vector3(ChatUiTargetWorldX, pos.y, pos.z);
    }

    private void ShiftChatAvatarLeft()
    {
        GameObject avatar = FindChatAvatar();
        if (avatar == null) return;

        Transform avatarTransform = avatar.transform;
        Vector3 pos = avatarTransform.position;
        avatarTransform.position = new Vector3(pos.x - ChatAvatarLeftShift, pos.y, pos.z);
    }

    private RectTransform EnsureAnswerContentHost(RectTransform answerPanelRT)
    {
        Transform existing = answerPanelRT.Find("AnswerContentHost");
        RectTransform hostRT;

        if (existing == null)
        {
            GameObject go = new GameObject("AnswerContentHost", typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(answerPanelRT, false);
            hostRT = go.GetComponent<RectTransform>();
        }
        else
        {
            hostRT = existing as RectTransform;
        }

        hostRT.anchorMin = Vector2.zero;
        hostRT.anchorMax = Vector2.one;
        hostRT.pivot = new Vector2(0.5f, 0.5f);
        hostRT.localScale = Vector3.one;
        hostRT.offsetMin = new Vector2(46f, 30f);
        hostRT.offsetMax = new Vector2(-46f, -88f);
        hostRT.SetAsLastSibling();

        RectMask2D mask = hostRT.GetComponent<RectMask2D>();
        if (mask == null) mask = hostRT.gameObject.AddComponent<RectMask2D>();

        return hostRT;
    }

    private void CreateOrUpdatePanelGlow(RectTransform panelRT, string name, Color color, float padding)
    {
        if (panelRT == null) return;

        Transform glow = panelRT.Find(name);
        if (glow == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelRT, false);
            glow = go.transform;
        }

        RectTransform glowRT = glow.GetComponent<RectTransform>();
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.pivot = new Vector2(0.5f, 0.5f);
        glowRT.offsetMin = new Vector2(-padding, -padding);
        glowRT.offsetMax = new Vector2(padding, padding);
        glowRT.localScale = Vector3.one;

        Image glowImage = glow.GetComponent<Image>();
        glowImage.sprite = GetRoundedRuntimeSprite();
        glowImage.type = Image.Type.Sliced;
        glowImage.color = color;
        glowImage.raycastTarget = false;

        glow.SetAsFirstSibling();
    }

    private void CreateOrUpdatePanelAccentLine(RectTransform panelRT, string name, Color color, float height, float horizontalInset)
    {
        if (panelRT == null) return;

        Transform line = panelRT.Find(name);
        if (line == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelRT, false);
            line = go.transform;
        }

        RectTransform lineRT = line.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0f, 1f);
        lineRT.anchorMax = new Vector2(1f, 1f);
        lineRT.pivot = new Vector2(0.5f, 1f);
        lineRT.anchoredPosition = new Vector2(0f, -10f);
        lineRT.sizeDelta = new Vector2(-(horizontalInset * 2f), height);

        Image lineImage = line.GetComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;

        line.SetAsLastSibling();
    }

    private void CreateOrUpdateAnswerInnerFrame(RectTransform answerPanelRT)
    {
        if (answerPanelRT == null) return;

        Transform frame = answerPanelRT.Find("AnswerInnerFrame");
        if (frame == null)
        {
            GameObject go = new GameObject("AnswerInnerFrame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(answerPanelRT, false);
            frame = go.transform;
        }

        RectTransform frameRT = frame.GetComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.pivot = new Vector2(0.5f, 0.5f);
        frameRT.offsetMin = new Vector2(12f, 12f);
        frameRT.offsetMax = new Vector2(-12f, -12f);
        frameRT.localScale = Vector3.one;

        Image frameImage = frame.GetComponent<Image>();
        frameImage.sprite = GetRoundedRuntimeSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = new Color(1f, 1f, 1f, 0.06f);
        frameImage.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>();
        if (outline == null) outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.50f, 0.88f, 1f, 0.28f);
        outline.effectDistance = new Vector2(1.4f, 1.4f);
        outline.useGraphicAlpha = true;

        frame.SetAsFirstSibling();
    }

    private void CreateOrUpdateAnswerBottomGlow(RectTransform answerPanelRT)
    {
        if (answerPanelRT == null) return;

        Transform glow = answerPanelRT.Find("AnswerBottomGlow");
        if (glow == null)
        {
            GameObject go = new GameObject("AnswerBottomGlow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(answerPanelRT, false);
            glow = go.transform;
        }

        RectTransform glowRT = glow.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.5f, 0f);
        glowRT.anchorMax = new Vector2(0.5f, 0f);
        glowRT.pivot = new Vector2(0.5f, 0f);
        glowRT.anchoredPosition = new Vector2(0f, 16f);
        glowRT.sizeDelta = new Vector2(760f, 86f);
        glowRT.localScale = Vector3.one;

        Image glowImage = glow.GetComponent<Image>();
        glowImage.sprite = GetRoundedRuntimeSprite();
        glowImage.type = Image.Type.Sliced;
        glowImage.color = new Color(0.16f, 0.82f, 1f, 0.12f);
        glowImage.raycastTarget = false;

        glow.SetAsFirstSibling();
    }

    private void CreateOrUpdateAnswerShine(RectTransform answerPanelRT)
    {
        if (answerPanelRT == null) return;

        Transform shine = answerPanelRT.Find("AnswerPanelShine");
        if (shine == null)
        {
            GameObject go = new GameObject("AnswerPanelShine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(answerPanelRT, false);
            shine = go.transform;
        }

        RectTransform shineRT = shine.GetComponent<RectTransform>();
        shineRT.anchorMin = new Vector2(0f, 1f);
        shineRT.anchorMax = new Vector2(1f, 1f);
        shineRT.pivot = new Vector2(0.5f, 1f);
        shineRT.anchoredPosition = new Vector2(0f, 0f);
        shineRT.sizeDelta = new Vector2(-30f, 94f);

        Image shineImage = shine.GetComponent<Image>();
        shineImage.sprite = GetRoundedRuntimeSprite();
        shineImage.type = Image.Type.Sliced;
        shineImage.color = new Color(1f, 1f, 1f, 0.10f);
        shineImage.raycastTarget = false;

        shine.SetAsFirstSibling();
    }

    private void StyleChatPanel(Image panelImage, Color fill, Color border, Color shadowColor)
    {
        if (panelImage == null) return;

        // Eski koyu/kütük sprite'ı bırakıp temiz rounded panel kullanıyoruz.
        panelImage.sprite = GetRoundedRuntimeSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.material = null;
        panelImage.color = fill;
        panelImage.raycastTarget = true;

        Outline outline = panelImage.GetComponent<Outline>();
        if (outline == null) outline = panelImage.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2.8f, 2.8f);
        outline.useGraphicAlpha = true;

        Shadow shadow = panelImage.GetComponent<Shadow>();
        if (shadow == null) shadow = panelImage.gameObject.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;
    }

    private Sprite GetRoundedRuntimeSprite()
    {
        if (_runtimeRoundedSprite != null)
            return _runtimeRoundedSprite;

        const int size = 96;
        const float radius = 24f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "RuntimeRoundedChatUISprite";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;

                float qx = Mathf.Abs(px) - (half - radius);
                float qy = Mathf.Abs(py) - (half - radius);

                float outsideX = Mathf.Max(qx, 0f);
                float outsideY = Mathf.Max(qy, 0f);
                float outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
                float insideDistance = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float signedDistance = outsideDistance + insideDistance - radius;

                float alpha = 1f - Mathf.SmoothStep(0f, 2f, signedDistance);
                alpha = Mathf.Clamp01(alpha);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        _runtimeRoundedSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(26f, 26f, 26f, 26f));

        return _runtimeRoundedSprite;
    }

    private void ApplyInputTextStyle(TMP_Text text, bool isPlaceholder)
    {
        if (text == null) return;

        RectTransform rt = text.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        text.fontSize = isPlaceholder ? 28f : 29f;
        text.fontStyle = isPlaceholder ? FontStyles.Italic : FontStyles.Normal;
        text.color = isPlaceholder
            ? new Color(0.38f, 0.54f, 0.65f, 0.78f)
            : new Color(0.11f, 0.31f, 0.44f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.margin = new Vector4(8f, 3f, 8f, 3f);
    }

    private void CreateOrUpdateSoftInputFrame(Transform parent)
    {
        if (parent == null) return;

        Transform frame = parent.Find("SoftInputFrame");
        if (frame == null)
        {
            GameObject go = new GameObject("SoftInputFrame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            frame = go.transform;
        }

        RectTransform frameRT = frame.GetComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.pivot = new Vector2(0.5f, 0.5f);
        frameRT.offsetMin = new Vector2(70f, 13f);
        frameRT.offsetMax = new Vector2(-76f, -13f);
        frameRT.localScale = Vector3.one;

        Image frameImage = frame.GetComponent<Image>();
        frameImage.sprite = GetRoundedRuntimeSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.color = new Color(0.80f, 0.92f, 0.985f, 0.42f);
        frameImage.raycastTarget = false;

        frame.SetAsFirstSibling();
    }

    private void CreateOrUpdateInputBadge(Transform parent, string name, string text, bool rightSide)
    {
        if (parent == null) return;

        Transform badge = parent.Find(name);
        if (badge == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            badge = go.transform;
        }

        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = rightSide ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        badgeRT.anchorMax = badgeRT.anchorMin;
        badgeRT.pivot = new Vector2(0.5f, 0.5f);
        badgeRT.sizeDelta = rightSide ? new Vector2(58f, 58f) : new Vector2(48f, 48f);
        badgeRT.anchoredPosition = rightSide ? new Vector2(-44f, 0f) : new Vector2(42f, 0f);
        badgeRT.localScale = Vector3.one;

        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.sprite = GetRoundedRuntimeSprite();
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = rightSide
            ? new Color(0.24f, 0.67f, 0.95f, 0.96f)
            : new Color(0.70f, 0.90f, 1f, 0.98f);
        badgeImage.raycastTarget = false;

        TMP_Text label = badge.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(badge, false);
            label = labelGO.GetComponent<TMP_Text>();
        }

        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        if (rightSide)
        {
            label.gameObject.SetActive(true);
            label.text = text;
            label.fontSize = 29f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.95f, 0.99f, 1f, 1f);
            label.raycastTarget = false;

            SetChildActive(badge, "IconDot01", false);
            SetChildActive(badge, "IconDot02", false);
            SetChildActive(badge, "IconDot03", false);
        }
        else
        {
            // Sol ikonda font yerine üç noktalı sade chat göstergesi kullan.
            label.gameObject.SetActive(false);
            CreateOrUpdateDotIcon(badge, "IconDot01", new Vector2(-10f, 0f), 5.5f);
            CreateOrUpdateDotIcon(badge, "IconDot02", new Vector2(0f, 0f), 5.5f);
            CreateOrUpdateDotIcon(badge, "IconDot03", new Vector2(10f, 0f), 5.5f);
        }

        badge.SetAsLastSibling();
    }

    private void CreateOrUpdateDotIcon(Transform parent, string name, Vector2 position, float size)
    {
        Transform dot = parent.Find(name);
        if (dot == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            dot = go.transform;
        }

        RectTransform dotRT = dot.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0.5f, 0.5f);
        dotRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotRT.pivot = new Vector2(0.5f, 0.5f);
        dotRT.anchoredPosition = position;
        dotRT.sizeDelta = new Vector2(size, size);
        dotRT.localScale = Vector3.one;
        dotRT.localEulerAngles = Vector3.zero;

        Image dotImage = dot.GetComponent<Image>();
        dotImage.sprite = GetRoundedRuntimeSprite();
        dotImage.type = Image.Type.Sliced;
        dotImage.color = new Color(0.07f, 0.47f, 0.74f, 1f);
        dotImage.raycastTarget = false;

        dot.gameObject.SetActive(true);
        dot.SetAsLastSibling();
    }

    private void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private void CreateOrUpdateAnswerHeader(RectTransform answerPanelRT)
    {
        if (answerPanelRT == null) return;

        Transform oldBadge = answerPanelRT.Find("ChatAnswerHeaderBadge");
        if (oldBadge != null)
            oldBadge.gameObject.SetActive(false);

        Transform title = answerPanelRT.Find("ChatAnswerHeaderTitle");
        if (title == null)
        {
            GameObject titleGO = new GameObject("ChatAnswerHeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(answerPanelRT, false);
            title = titleGO.transform;
        }

        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(0f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(46f, -28f);
        titleRT.sizeDelta = new Vector2(430f, 42f);

        TMP_Text titleText = title.GetComponent<TMP_Text>();
        titleText.text = "Yapay Zekâ Cevabı";
        titleText.fontSize = 30f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.09f, 0.50f, 0.78f, 1f);
        titleText.raycastTarget = false;

        Transform underline = answerPanelRT.Find("ChatAnswerHeaderUnderline");
        if (underline == null)
        {
            GameObject underlineGO = new GameObject("ChatAnswerHeaderUnderline", typeof(RectTransform), typeof(Image));
            underlineGO.transform.SetParent(answerPanelRT, false);
            underline = underlineGO.transform;
        }

        RectTransform underlineRT = underline.GetComponent<RectTransform>();
        underlineRT.anchorMin = new Vector2(0f, 1f);
        underlineRT.anchorMax = new Vector2(1f, 1f);
        underlineRT.pivot = new Vector2(0.5f, 1f);
        underlineRT.anchoredPosition = new Vector2(0f, -64f);
        underlineRT.sizeDelta = new Vector2(-92f, 4f);

        Image underlineImage = underline.GetComponent<Image>();
        underlineImage.sprite = GetRoundedRuntimeSprite();
        underlineImage.type = Image.Type.Sliced;
        underlineImage.color = new Color(0.34f, 0.82f, 1f, 0.72f);
        underlineImage.raycastTarget = false;

        Shadow underlineShadow = underline.GetComponent<Shadow>();
        if (underlineShadow == null) underlineShadow = underline.gameObject.AddComponent<Shadow>();
        underlineShadow.effectColor = new Color(0.18f, 0.80f, 1f, 0.22f);
        underlineShadow.effectDistance = new Vector2(0f, -1f);
        underlineShadow.useGraphicAlpha = true;

        Transform oldLine = answerPanelRT.Find("ChatAnswerHeaderLine");
        if (oldLine != null)
            oldLine.gameObject.SetActive(false);

        title.SetAsLastSibling();
        underline.SetAsLastSibling();
    }

    private void CreateOrUpdateAnswerDecor(RectTransform answerPanelRT)
    {
        if (answerPanelRT == null) return;

        CreateDecorLine(answerPanelRT, "ChatDecorWaveLeft", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(310f, 2f), new Vector2(190f, 86f), 11f, new Color(1f, 1f, 1f, 0.14f));

        CreateDecorLine(answerPanelRT, "ChatDecorWaveCenter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(380f, 2f), new Vector2(0f, 76f), -7f, new Color(1f, 1f, 1f, 0.18f));

        CreateDecorLine(answerPanelRT, "ChatDecorWaveRight", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(310f, 2f), new Vector2(-190f, 90f), -13f, new Color(1f, 1f, 1f, 0.22f));

        CreateDecorDot(answerPanelRT, "ChatDecorDot01", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(90f, 125f), 10f);
        CreateDecorDot(answerPanelRT, "ChatDecorDot02", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(145f, 165f), 7f);
        CreateDecorDot(answerPanelRT, "ChatDecorDot03", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-125f, 130f), 8f);

        foreach (Transform child in answerPanelRT)
        {
            if (child.name.StartsWith("ChatDecor"))
                child.SetAsFirstSibling();
        }
    }

    private void CreateDecorLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, float rotation, Color color)
    {
        Image img = GetOrCreateDecorImage(parent, name);
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        rt.localEulerAngles = new Vector3(0f, 0f, rotation);
        rt.localScale = Vector3.one;

        img.color = color;
        img.raycastTarget = false;
    }

    private void CreateDecorDot(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, float size)
    {
        Image img = GetOrCreateDecorImage(parent, name);
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = position;
        rt.localEulerAngles = Vector3.zero;
        rt.localScale = Vector3.one;

        img.sprite = GetRoundedRuntimeSprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.24f, 0.66f, 0.92f, 0.28f);
        img.raycastTarget = false;
    }

    private Image GetOrCreateDecorImage(RectTransform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            existing = go.transform;
        }

        return existing.GetComponent<Image>();
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
        const string prefix = "<indent=22px><line-indent=-22px><color=#29C8FF><b>•</b></color><space=8px>";
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
