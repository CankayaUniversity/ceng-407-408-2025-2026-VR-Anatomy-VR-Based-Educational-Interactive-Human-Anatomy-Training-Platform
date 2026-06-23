using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SimpleBoneExplanationRequest
{
    public string bone_name;
    public string unit_name;
    public string original_text;
}

[Serializable]
public class SimpleBoneExplanationResponse
{
    public string bone_name;
    public string unit_name;
    public string simple_explanation;
    public string[] key_points;
    public string speech_text;
}

public class SimpleBoneExplanationClient : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://vr-anatomy-backend2.onrender.com/learning/simple-bone-explanation";
    [SerializeField] private int timeoutSeconds = 30;

    [Header("Lesson UI")]
    [SerializeField] private TMP_Text infoTitleText;
    [SerializeField] private TMP_Text infoBodyText;

    [Header("Speech")]
    [SerializeField] private bool stopCurrentSpeechBeforeRequest = true;
    [SerializeField] private bool speakGeneratedExplanation = true;
    [SerializeField] private bool speakFallbackOriginalText = false;

    private const string LoadingMessage = "Daha basit anlatım hazırlanıyor...";

    private Coroutine _requestRoutine;
    private int _requestId;

    public void Initialize(TMP_Text titleText, TMP_Text bodyText, LessonUIReader reader = null)
    {
        if (infoTitleText == null)
            infoTitleText = titleText;

        if (infoBodyText == null)
            infoBodyText = bodyText;
    }

    public void RequestCurrentBoneSimpleExplanation()
    {
        LessonManager lessonManager = LessonManager.Instance;

        if (lessonManager == null)
            lessonManager = FindFirstObjectByType<LessonManager>();

        if (lessonManager == null)
        {
            Debug.LogError("[SimpleBoneExplanationClient] Aktif LessonManager bulunamadı; basit anlatım isteği gönderilemedi.", this);
            return;
        }

        Initialize(lessonManager.titleText, lessonManager.infoText);

        if (infoTitleText == null || infoBodyText == null)
        {
            Debug.LogError("[SimpleBoneExplanationClient] Title veya body text referansı eksik.", this);
            return;
        }

        string boneName = CleanBoneTitle(infoTitleText.text);
        string unitName = "";
        string originalText = SafeTrim(infoBodyText.text);

        if (string.IsNullOrWhiteSpace(boneName))
        {
            Debug.LogError("[SimpleBoneExplanationClient] Kemik adı bulunamadı.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(originalText))
        {
            Debug.LogError("[SimpleBoneExplanationClient] Açıklama metni bulunamadı.", this);
            return;
        }

        if (originalText == LoadingMessage)
        {
            Debug.LogWarning("[SimpleBoneExplanationClient] Zaten basit anlatım hazırlanıyor.", this);
            return;
        }

        RequestSimpleExplanation(boneName, unitName, originalText);
    }

    public void RequestSimpleExplanation(string boneName, string unitName, string originalText)
    {
        if (_requestRoutine != null)
        {
            StopCoroutine(_requestRoutine);
            _requestRoutine = null;
        }

        int requestId = ++_requestId;
        _requestRoutine = StartCoroutine(RequestSimpleExplanationRoutine(requestId, boneName, unitName, originalText));
    }

    private IEnumerator RequestSimpleExplanationRoutine(int requestId, string boneName, string unitName, string originalText)
    {
        boneName = SafeTrim(boneName);
        unitName = SafeTrim(unitName);
        originalText = SafeTrim(originalText);

        if (infoTitleText != null)
            infoTitleText.text = boneName;

        if (infoBodyText != null)
            infoBodyText.text = LoadingMessage;

        if (stopCurrentSpeechBeforeRequest && TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
        }

        SimpleBoneExplanationRequest payload = new SimpleBoneExplanationRequest
        {
            bone_name = boneName,
            unit_name = unitName,
            original_text = originalText
        };

        string requestJson = JsonUtility.ToJson(payload);
        Debug.Log($"[SimpleBoneExplanationClient] Request JSON: {requestJson}", this);

        using (UnityWebRequest request = new UnityWebRequest(backendUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            if (requestId != _requestId)
            {
                Debug.Log("[SimpleBoneExplanationClient] Eski basit anlatım isteği iptal edildi.", this);
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success || request.responseCode < 200 || request.responseCode >= 300)
            {
                ApplyFallback(
                    boneName,
                    originalText,
                    $"Gemini basit anlatım alınamadı: {request.error} | HTTP {request.responseCode}"
                );

                yield break;
            }

            string responseJson = request.downloadHandler != null ? request.downloadHandler.text : "";
            Debug.Log($"[SimpleBoneExplanationClient] Response JSON: {responseJson}", this);

            SimpleBoneExplanationResponse response;

            try
            {
                response = JsonUtility.FromJson<SimpleBoneExplanationResponse>(responseJson);
            }
            catch (Exception ex)
            {
                ApplyFallback(boneName, originalText, $"Gemini basit anlatım JSON parse hatası: {ex.Message}");
                yield break;
            }

            if (!IsValidResponse(response))
            {
                ApplyFallback(boneName, originalText, "Gemini basit anlatım response eksik veya geçersiz.");
                yield break;
            }

            string responseBoneName = string.IsNullOrWhiteSpace(response.bone_name)
                ? boneName
                : response.bone_name.Trim();

            string simpleExplanation = response.simple_explanation.Trim();

            if (infoTitleText != null)
                infoTitleText.text = responseBoneName + " - Basit Anlatım";

            if (infoBodyText != null)
                infoBodyText.text = simpleExplanation;

            if (speakGeneratedExplanation && TTSClient.Instance != null)
            {
                string speechText = ResolveSimpleExplanationSpeechText(response);
                TTSClient.Instance.Speak(speechText);
            }

            _requestRoutine = null;
        }
    }

    private void ApplyFallback(string boneName, string originalText, string errorMessage)
    {
        Debug.LogError($"[SimpleBoneExplanationClient] {errorMessage}", this);

        if (infoTitleText != null)
            infoTitleText.text = boneName;

        if (infoBodyText != null)
            infoBodyText.text = originalText;

        if (speakFallbackOriginalText && TTSClient.Instance != null)
        {
            TTSClient.Instance.Speak(originalText);
        }

        _requestRoutine = null;
    }

    private static bool IsValidResponse(SimpleBoneExplanationResponse response)
    {
        return response != null
               && !string.IsNullOrWhiteSpace(response.simple_explanation);
    }

    private static string ResolveSimpleExplanationSpeechText(SimpleBoneExplanationResponse response)
    {
        string speechText = response != null ? SafeTrim(response.speech_text) : "";

        if (!string.IsNullOrEmpty(speechText))
            return speechText;

        return response != null ? SafeTrim(response.simple_explanation) : "";
    }

    private static string CleanBoneTitle(string title)
    {
        title = SafeTrim(title);

        if (string.IsNullOrEmpty(title))
            return "";

        title = title.Replace(" - Basit Anlatım", "");
        title = title.Replace("- Basit Anlatım", "");
        title = title.Replace("Basit Anlatım", "");

        return title.Trim();
    }

    private static string SafeTrim(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}