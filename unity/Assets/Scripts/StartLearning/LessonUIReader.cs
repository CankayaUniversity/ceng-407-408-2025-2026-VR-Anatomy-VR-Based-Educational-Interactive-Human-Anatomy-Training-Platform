using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class LessonUIReader : MonoBehaviour
{
    private const string FemaleTtsVoice = "tr-TR-EmelNeural";
    private const string MaleTtsVoice = "tr-TR-AhmetNeural";
    private const string MaleTtsPitch = "+8%";
    private const string MaleTtsRate = "+0%";
    private const string StudentNamePrefKey = "StudentName";

    public enum LessonSection
    {
        Auto = 0,
        HeadAndFaceBones = 1,
        TrunkBones = 2,
        UpperExtremityBones = 3,
        LowerExtremityBones = 4,
        SkeletalMuscles = 5,
        HeartStructure = 6,
        Vessels = 7
    }

    [Header("Lesson")]
    [Tooltip("Auto, NavigationState/AnatomyState üzerinden seçili bölümü bulur. Bulunamazsa buradaki manuel değer kullanılır.")]
    [SerializeField] private LessonSection section = LessonSection.Auto;
    [SerializeField] private LessonSection fallbackSection = LessonSection.HeadAndFaceBones;
    [SerializeField] private bool playIntroOnEnable = true;

    [Header("Speech API")]
    [SerializeField] private string ttsUrl = "http://127.0.0.1:8001/tts";
    [SerializeField] private bool respectAIChatVoiceSetting = true;

    private AudioSource _audio;
    private Coroutine _lessonRoutine;
    private Coroutine _cardRoutine;
    private int _speechRequestId;
    private bool _introFinished;
    private bool _introPlayed;
    private bool _cardChangePendingDuringIntro;
    private bool _suppressNextCardRead;
    private string _lastSpokenCardKey = "";

    [Serializable]
    private class TtsPayload
    {
        public string text;
        public string voice;
        public string pitch;
        public string rate;
    }

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        LessonManager.OnBoneChanged += HandleBoneChanged;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAIChatVoiceEnabledChanged += HandleVoiceSettingChanged;

        if (playIntroOnEnable)
            _lessonRoutine = StartCoroutine(PlayIntroThenCurrentCard());
        else
            _introFinished = true;
    }

    private void OnDisable()
    {
        LessonManager.OnBoneChanged -= HandleBoneChanged;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnAIChatVoiceEnabledChanged -= HandleVoiceSettingChanged;

        StopSpeech();
        StopRunningRoutine(ref _lessonRoutine);
        StopRunningRoutine(ref _cardRoutine);
    }

    private IEnumerator PlayIntroThenCurrentCard()
    {
        if (_introPlayed)
            yield break;

        _introPlayed = true;
        _introFinished = false;
        _cardChangePendingDuringIntro = false;

        LessonSection resolvedSection = ResolveSection();
        string introText = BuildIntroSpeechText(resolvedSection);
        string introTitle = GetIntroTitle(resolvedSection);

        Debug.Log(
            $"[LessonUIReader] Seçilen LessonSection={resolvedSection} | Intro başlığı='{introTitle}' | " +
            $"AnatomyUnitID={AnatomyState.SelectedAnatomyUnitID} | MotionSubUnit={NavigationState.SelectedMotionSubUnit} | " +
            $"CirculationSubUnit={NavigationState.SelectedCirculationSubUnit}",
            this);

        if (!string.IsNullOrWhiteSpace(introText))
            yield return SpeakAndWait(introText, "intro", stopCurrentSpeech: true);
        else
            Debug.LogWarning($"[LessonUIReader] Bölüm giriş metni bulunamadı: {resolvedSection}", this);

        _introFinished = true;
        Debug.Log("[LessonUIReader] Bölüm giriş okuması bitti. Görünen bilgi kartı okunacak.", this);

        yield return ReadCurrentCardAfterUiSettles(forceRead: true);
        _cardChangePendingDuringIntro = false;
        _lessonRoutine = null;
    }

    private void HandleBoneChanged(Transform newBoneTransform)
    {
        string boneName = newBoneTransform != null ? newBoneTransform.name : "null";
        Debug.Log($"[LessonUIReader] Kart değişimi algılandı: {boneName}", this);

        if (_suppressNextCardRead)
        {
            _suppressNextCardRead = false;
            _cardChangePendingDuringIntro = false;
            StopRunningRoutine(ref _cardRoutine);
            Debug.Log("[LessonUIReader] Review basit anlatım akışı için otomatik kart okuması atlandı.", this);
            return;
        }

        if (!_introFinished)
        {
            _cardChangePendingDuringIntro = true;
            Debug.Log("[LessonUIReader] Giriş okuması sürüyor; kart okuması giriş bittikten sonra yapılacak.", this);
            return;
        }

        StopRunningRoutine(ref _cardRoutine);
        _cardRoutine = StartCoroutine(ReadCurrentCardAfterUiSettles(forceRead: false));
    }

    public void SuppressNextCardRead()
    {
        _suppressNextCardRead = true;
    }

    public void SpeakReviewText(string text)
    {
        StopRunningRoutine(ref _cardRoutine);
        _cardRoutine = StartCoroutine(SpeakReviewTextRoutine(text));
    }

    public void StopCurrentSpeech()
    {
        StopSpeech();
    }

    private IEnumerator SpeakReviewTextRoutine(string text)
    {
        string safeText = SafeTrim(text);
        if (string.IsNullOrEmpty(safeText))
        {
            Debug.Log("[LessonUIReader] Review metni boş; TTS çağrısı yapılmadı.", this);
            _cardRoutine = null;
            yield break;
        }

        _lastSpokenCardKey = safeText;
        Debug.Log("[LessonUIReader] Review metni okunuyor.", this);
        yield return SpeakAndWait(safeText, "review", stopCurrentSpeech: true);
        _cardRoutine = null;
    }

    private IEnumerator ReadCurrentCardAfterUiSettles(bool forceRead)
    {
        yield return null;

        if (!_introFinished)
            yield break;

        if (_cardChangePendingDuringIntro)
        {
            Debug.Log("[LessonUIReader] Giriş sırasında bekleyen son kart okunuyor.", this);
            _cardChangePendingDuringIntro = false;
        }

        string title = SafeTrim(LessonManager.Instance != null && LessonManager.Instance.titleText != null
            ? LessonManager.Instance.titleText.text
            : null);
        string body = SafeTrim(LessonManager.Instance != null && LessonManager.Instance.infoText != null
            ? LessonManager.Instance.infoText.text
            : null);

        string textToSpeak = BuildCardSpeechText(title, body);
        if (string.IsNullOrEmpty(textToSpeak))
        {
            Debug.Log("[LessonUIReader] Kart başlığı ve açıklaması boş; TTS çağrısı yapılmadı.", this);
            _cardRoutine = null;
            yield break;
        }

        string cardKey = $"{title}\n{body}";
        if (!forceRead && string.Equals(cardKey, _lastSpokenCardKey, StringComparison.Ordinal))
        {
            Debug.Log($"[LessonUIReader] Aynı kart tekrar geldi, duplicate okuma atlandı: {title}", this);
            _cardRoutine = null;
            yield break;
        }

        _lastSpokenCardKey = cardKey;
        Debug.Log($"[LessonUIReader] Bilgi kartı okunuyor: {title}", this);
        yield return SpeakAndWait(textToSpeak, "card", stopCurrentSpeech: true);
        _cardRoutine = null;
    }

    private IEnumerator SpeakAndWait(string text, string reason, bool stopCurrentSpeech)
    {
        if (!CanUseVoice())
        {
            Debug.Log($"[LessonUIReader] Sesli okuma kapalı; {reason} metni atlandı.", this);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(text))
            yield break;

        if (stopCurrentSpeech)
            StopSpeech();

        int requestId = ++_speechRequestId;
        bool isMaleAvatar = IsMaleAvatarSelected();
        TtsPayload payload = new TtsPayload
        {
            text = text.Trim(),
            voice = isMaleAvatar ? MaleTtsVoice : FemaleTtsVoice,
            pitch = isMaleAvatar ? MaleTtsPitch : null,
            rate = isMaleAvatar ? MaleTtsRate : null
        };

        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        Debug.Log($"[LessonUIReader] TTS isteği gönderiliyor ({reason}, request={requestId}, chars={payload.text.Length}).", this);

        using (UnityWebRequest req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (requestId != _speechRequestId)
            {
                Debug.Log($"[LessonUIReader] Eski TTS isteği iptal edildi ({reason}, request={requestId}).", this);
                yield break;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LessonUIReader] TTS hatası ({reason}): {req.error} | HTTP {req.responseCode}", this);
                yield break;
            }

            string tmpPath = Path.Combine(Application.temporaryCachePath, $"lesson_tts_{requestId}.mp3");
            File.WriteAllBytes(tmpPath, req.downloadHandler.data);

            string fileUrl = "file:///" + tmpPath.Replace("\\", "/");
            using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
            {
                yield return audioReq.SendWebRequest();

                if (requestId != _speechRequestId)
                {
                    Debug.Log($"[LessonUIReader] Eski ses yükleme isteği iptal edildi ({reason}, request={requestId}).", this);
                    yield break;
                }

                if (audioReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[LessonUIReader] Ses dosyası yüklenemedi ({reason}): {audioReq.error}", this);
                    yield break;
                }

                _audio.clip = DownloadHandlerAudioClip.GetContent(audioReq);
                _audio.Play();
                Debug.Log($"[LessonUIReader] Ses oynatılıyor ({reason}, request={requestId}, duration={_audio.clip.length:F2}s).", this);

                while (_audio != null && _audio.isPlaying && requestId == _speechRequestId)
                    yield return null;

                if (requestId == _speechRequestId)
                    Debug.Log($"[LessonUIReader] Ses oynatma tamamlandı ({reason}, request={requestId}).", this);
            }
        }
    }

    private void StopSpeech()
    {
        _speechRequestId++;

        if (_audio != null && _audio.isPlaying)
        {
            _audio.Stop();
            Debug.Log("[LessonUIReader] Önceki ses durduruldu.", this);
        }
    }

    private void StopRunningRoutine(ref Coroutine routine)
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
    }

    private void HandleVoiceSettingChanged(bool enabled)
    {
        if (!enabled)
            StopSpeech();
    }

    private bool CanUseVoice()
    {
        if (!respectAIChatVoiceSetting)
            return true;

        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.AIChatVoiceEnabled;

        return PlayerPrefs.GetInt(SettingsManager.AIChatVoiceEnabledKey, 1) == 1;
    }

    private static string BuildCardSpeechText(string title, string body)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
            return "";

        if (string.IsNullOrEmpty(title))
            return body;

        if (string.IsNullOrEmpty(body))
            return title;

        return $"{title}. {body}";
    }

    private static string SafeTrim(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private LessonSection ResolveSection()
    {
        if (section != LessonSection.Auto)
        {
            Debug.Log($"[LessonUIReader] Inspector section kullanılıyor: {section}", this);
            return section;
        }

        if (AnatomyState.SelectedLessonSection != LessonSection.Auto)
        {
            Debug.Log($"[LessonUIReader] AnatomyState.SelectedLessonSection kullanılıyor: {AnatomyState.SelectedLessonSection}", this);
            return AnatomyState.SelectedLessonSection;
        }

        LessonSection resolved = ResolveSectionFromNavigation();
        if (resolved != LessonSection.Auto)
            return resolved;

        Debug.LogWarning($"[LessonUIReader] Seçili bölüm runtime state içinde bulunamadı. Fallback kullanılacak: {fallbackSection}", this);
        return fallbackSection;
    }

    private static LessonSection ResolveSectionFromNavigation()
    {
        switch (NavigationState.SelectedMotionSubUnit)
        {
            case MotionSubUnit.HeadFaceBones:
                return LessonSection.HeadAndFaceBones;
            case MotionSubUnit.Rib:
            case MotionSubUnit.Spine:
                return LessonSection.TrunkBones;
            case MotionSubUnit.UpperExtremityBones:
                return LessonSection.UpperExtremityBones;
            case MotionSubUnit.LowerExtremityBones:
                return LessonSection.LowerExtremityBones;
            case MotionSubUnit.UpperExtremityMuscles:
            case MotionSubUnit.LowerExtremityMuscles:
                return LessonSection.SkeletalMuscles;
        }

        switch (NavigationState.SelectedCirculationSubUnit)
        {
            case CirculationSubUnit.HeartInnerStructure:
            case CirculationSubUnit.HeartOuterStructure:
                return LessonSection.HeartStructure;
            case CirculationSubUnit.UpperExtremityArteries:
            case CirculationSubUnit.AbdominalAortaBranches:
            case CirculationSubUnit.LowerExtremityArteries:
            case CirculationSubUnit.PalpableArteries:
            case CirculationSubUnit.UpperExtremityVeins:
            case CirculationSubUnit.LowerExtremityVeins:
            case CirculationSubUnit.SystemicCirculation:
            case CirculationSubUnit.PulmonaryCirculation:
                return LessonSection.Vessels;
        }

        switch (AnatomyState.SelectedAnatomyUnitID)
        {
            case 0:
                Debug.LogWarning("[LessonUIReader] Legacy 0-based AnatomyUnitID=0 algılandı; HeadAndFaceBones olarak yorumlanıyor.", null);
                return LessonSection.HeadAndFaceBones;
            case 1:
                return LessonSection.HeadAndFaceBones;
            case 2:
                return LessonSection.TrunkBones;
            case 3:
                return LessonSection.UpperExtremityBones;
            case 4:
                return LessonSection.LowerExtremityBones;
            case 5:
                return LessonSection.SkeletalMuscles;
            case 6:
                return LessonSection.HeartStructure;
            case 7:
                return LessonSection.Vessels;
        }

        return LessonSection.Auto;
    }

    private static string GetIntroText(LessonSection resolvedSection)
    {
        switch (resolvedSection)
        {
            case LessonSection.HeadAndFaceBones:
                return "Şimdi baş ve yüz kemikleri bölümünü birlikte inceleyelim. Bu bölümde kafatasını oluşturan temel kemikleri ve yüz bölgesindeki önemli yapıları adım adım göreceksin. Bilgi kartları sırayla ekrana gelecek. Hazır olduğunda Sıradaki butonuna basarak bir sonraki karta geçebilirsin.";
            case LessonSection.TrunkBones:
                return "Şimdi gövde kemikleri bölümünü birlikte inceleyelim. Bu bölümde omurga, göğüs kafesi ve gövdeyi destekleyen temel kemik yapıları üzerinde duracağız. Her bilgi kartında ilgili kemiğe ait kısa ve anlaşılır bilgiler göreceksin. Hazır olduğunda Sıradaki butonuna basarak kartlar arasında ilerleyebilirsin.";
            case LessonSection.UpperExtremityBones:
                return "Şimdi üst ekstremite kemikleri bölümünü birlikte inceleyelim. Bu bölümde omuz, kol, ön kol ve el bölgesindeki kemikleri adım adım ele alacağız. Bilgi kartları kemiklerin konumunu ve temel görevini sade bir dille anlatacak. Hazır olduğunda Sıradaki butonuna basarak ilerleyebilirsin.";
            case LessonSection.LowerExtremityBones:
                return "Şimdi alt ekstremite kemikleri bölümünü birlikte inceleyelim. Bu bölümde kalça, uyluk, bacak ve ayak bölgesindeki kemik yapıları üzerinde duracağız. Bilgi kartları ekrana sırayla gelecek ve her kartta ilgili kemiğin temel bilgileri yer alacak. Hazır olduğunda Sıradaki butonuna basarak ilerleyebilirsin.";
            case LessonSection.SkeletalMuscles:
                return "Şimdi iskelet kasları bölümünü birlikte inceleyelim. Bu bölümde vücudun hareket etmesini sağlayan temel kas gruplarını tanıyacağız. Bilgi kartlarında kasların bulunduğu bölge ve genel görevleri kısa ve anlaşılır şekilde göreceksin. Hazır olduğunda Sıradaki butonuna basarak bir sonraki karta geçebilirsin.";
            case LessonSection.HeartStructure:
                return "Şimdi kalbin yapısı bölümünü birlikte inceleyelim. Bu bölümde kalbin temel bölümlerini, odacıklarını ve kanın kalp içindeki ilerleyişini adım adım göreceğiz. Bilgi kartları sırayla ekrana gelecek. Hazır olduğunda Sıradaki butonuna basarak bir sonraki karta geçebilirsin.";
            case LessonSection.Vessels:
                return "Şimdi damarlar bölümünü birlikte inceleyelim. Bu bölümde atardamarlar, toplardamarlar ve kılcal damarlar gibi dolaşım sisteminin temel damar yapılarını ele alacağız. Her bilgi kartında damarların görevleri kısa ve anlaşılır şekilde anlatılacak. Hazır olduğunda Sıradaki butonuna basarak kartlar arasında ilerleyebilirsin.";
            default:
                return "";
        }
    }

    private static string BuildIntroSpeechText(LessonSection resolvedSection)
{
    string intro = GetIntroText(resolvedSection);
    if (string.IsNullOrWhiteSpace(intro))
        return "";

    string studentName = SafeTrim(PlayerPrefs.GetString(StudentNamePrefKey, ""));
    if (string.IsNullOrEmpty(studentName))
        return intro;

    string affectionateName = BuildAffectionateName(studentName);

    return $"Merhaba {affectionateName}, hoş geldin. {intro}";
}
private static string BuildAffectionateName(string rawName)
{
    string name = SafeTrim(rawName);

    if (string.IsNullOrEmpty(name))
        return "";

    // Eğer kullanıcı "Çağla Pelin" gibi iki isim girdiyse,
    // selamlamada ilk ismi kullanmak daha doğal olur.
    string firstName = name.Split(' ')[0];

    char lastVowel = FindLastTurkishVowel(firstName);

    string suffix;

    switch (lastVowel)
    {
        case 'a':
        case 'A':
        case 'ı':
        case 'I':
            suffix = "cığım";
            break;

        case 'e':
        case 'E':
        case 'i':
        case 'İ':
            suffix = "ciğim";
            break;

        case 'o':
        case 'O':
        case 'u':
        case 'U':
            suffix = "cuğum";
            break;

        case 'ö':
        case 'Ö':
        case 'ü':
        case 'Ü':
            suffix = "cüğüm";
            break;

        default:
            suffix = "cığım";
            break;
    }

    // TTS daha doğal okusun diye apostrof koymuyoruz:
    // Çağla'cığım yerine Çağlacığım
    return firstName + suffix;
}

private static char FindLastTurkishVowel(string text)
{
    if (string.IsNullOrEmpty(text))
        return '\0';

    for (int i = text.Length - 1; i >= 0; i--)
    {
        char c = text[i];

        if (IsTurkishVowel(c))
            return c;
    }

    return '\0';
}

private static bool IsTurkishVowel(char c)
{
    return c == 'a' || c == 'A'
        || c == 'e' || c == 'E'
        || c == 'ı' || c == 'I'
        || c == 'i' || c == 'İ'
        || c == 'o' || c == 'O'
        || c == 'ö' || c == 'Ö'
        || c == 'u' || c == 'U'
        || c == 'ü' || c == 'Ü';
}
    private static string GetIntroTitle(LessonSection resolvedSection)
    {
        switch (resolvedSection)
        {
            case LessonSection.HeadAndFaceBones:
                return "Baş ve Yüz Kemikleri";
            case LessonSection.TrunkBones:
                return "Gövde Kemikleri";
            case LessonSection.UpperExtremityBones:
                return "Üst Ekstremite Kemikleri";
            case LessonSection.LowerExtremityBones:
                return "Alt Ekstremite Kemikleri";
            case LessonSection.SkeletalMuscles:
                return "İskelet Kasları";
            case LessonSection.HeartStructure:
                return "Kalbin Yapısı";
            case LessonSection.Vessels:
                return "Damarlar";
            default:
                return "Bilinmeyen Bölüm";
        }
    }

    /// <summary>
    /// Ses eşleşmesi:
    ///   Female (0) + YoungFemale (2) → tr-TR-EmelNeural
    ///   Male   (1) + YoungMale   (3) → tr-TR-AhmetNeural
    /// </summary>
    private static bool IsMaleAvatarSelected()
    {
        SettingsManager.AvatarType type;

        if (SettingsManager.Instance != null)
        {
            type = SettingsManager.Instance.SelectedAvatarType;
        }
        else
        {
            int raw = PlayerPrefs.GetInt("AvatarType", (int)SettingsManager.AvatarType.Female);
            type = (SettingsManager.AvatarType)Mathf.Clamp(raw, 0, 3);
        }

        return type == SettingsManager.AvatarType.Male
            || type == SettingsManager.AvatarType.YoungMale;
    }
}