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
    [SerializeField] private string backendUrl = "http://127.0.0.1:8000/learning/simple-bone-explanation";
    [SerializeField] private int timeoutSeconds = 30;

    [Header("Lesson UI")]
    [SerializeField] private TMP_Text infoTitleText;
    [SerializeField] private TMP_Text infoBodyText;

    [Header("Speech")]
    [SerializeField] private LessonUIReader lessonUIReader;

    private const string LoadingMessage = "Daha basit anlatım hazırlanıyor...";

    private Coroutine _requestRoutine;
    private int _requestId;

    public void Initialize(TMP_Text titleText, TMP_Text bodyText, LessonUIReader reader)
    {
        if (infoTitleText == null)
            infoTitleText = titleText;

        if (infoBodyText == null)
            infoBodyText = bodyText;

        if (lessonUIReader == null)
            lessonUIReader = reader;
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

        ResolveLessonUIReader();
        Initialize(lessonManager.titleText, lessonManager.infoText, lessonUIReader);

        if (!lessonManager.TryGetCurrentBonePayload(out string boneName, out string unitName, out string originalText))
        {
            Debug.LogError("[SimpleBoneExplanationClient] Aktif bilgi kartı verisi bulunamadı; basit anlatım isteği gönderilemedi.", this);
            return;
        }

        RequestSimpleExplanation(boneName, unitName, originalText);
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

        ResolveLessonUIReader();
        if (lessonUIReader != null)
            lessonUIReader.StopCurrentSpeech();

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
                    $"Gemini basit anlatım alınamadı: {request.error} | HTTP {request.responseCode}");
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

            string responseBoneName = string.IsNullOrWhiteSpace(response.bone_name) ? boneName : response.bone_name.Trim();

            if (infoTitleText != null)
                infoTitleText.text = responseBoneName + " - Basit Anlatım";

            if (infoBodyText != null)
                infoBodyText.text = response.simple_explanation.Trim();

            ResolveLessonUIReader();
            if (lessonUIReader != null)
                lessonUIReader.SpeakReviewText(response.simple_explanation.Trim());

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

        ResolveLessonUIReader();
        if (lessonUIReader != null)
            lessonUIReader.SpeakReviewText(originalText);

        _requestRoutine = null;
    }

    private void ResolveLessonUIReader()
    {
        if (lessonUIReader != null)
            return;

        lessonUIReader = FindFirstObjectByType<LessonUIReader>();
    }

    private static bool IsValidResponse(SimpleBoneExplanationResponse response)
{
    return response != null
           && !string.IsNullOrWhiteSpace(response.simple_explanation);
}

    private static string SafeTrim(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}
