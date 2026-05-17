using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SimpleExplanationRequest
{
    public string bone_name;
    public string unit_name;
    public string original_text;
}

[Serializable]
public class SimpleExplanationResponse
{
    public string bone_name;
    public string unit_name;
    public string simple_explanation;
    public string[] key_points;
    public string speech_text;
}

public class SimpleExplanationClient : MonoBehaviour
{
    [Header("Backend Server")]
    [Tooltip("Check if this port matches your functional TTS client port!")]
    [SerializeField] private string backendUrl = "http://127.0.0.1:8000/learning/simple-bone-explanation";
    [SerializeField] private int timeoutSeconds = 30;

    [Header("UI Source Reference")]
    [SerializeField] private TMP_Text infoBodyText;

    [Header("UI Destination Reference")]
    [SerializeField] private TMP_Text simpleDescriptionText;

    private const string LoadingMessage = "Daha basit anlatým hazýrlanýyor...";
    private Coroutine _requestRoutine;
    private int _requestId;

    public void FetchSimpleExplanation()
    {
        Debug.LogError("[SIMPLE_ANLAT] FetchSimpleExplanation() triggered via button click.");

        if (TTSClient.Instance != null)
        {
            TTSClient.Instance.Stop();
            Debug.LogError("[SIMPLE_ANLAT] Global TTSClient instance found. Audio execution stopped.");
        }
        else
        {
            Debug.LogError("[SIMPLE_ANLAT] WARNING: Global TTSClient.Instance is NULL in this scene frame context.");
        }

        // Structural Parameter Validation Checks
        if (infoBodyText == null)
        {
            Debug.LogError("[SIMPLE_ANLAT] CRITICAL ERROR: 'Info Body Text' field slot is empty in Inspector layout properties!");
            return;
        }
        if (simpleDescriptionText == null)
        {
            Debug.LogError("[SIMPLE_ANLAT] CRITICAL ERROR: 'Simple Description Text' field slot is empty in Inspector layout properties!");
            return;
        }
        if (LessonManager.Instance == null)
        {
            Debug.LogError("[SIMPLE_ANLAT] CRITICAL ERROR: LessonManager.Instance is missing or deactivated in this environment.");
            return;
        }

        string currentBoneName = LessonManager.Instance.titleText != null ? LessonManager.Instance.titleText.text : "Bilinmeyen Kemik";
        string currentUnitName = LessonManager.Instance.gameObject.name;
        string sourceTextContext = infoBodyText.text;

        Debug.LogError($"[SIMPLE_ANLAT] Context verification data package compiled successfully.\n" +
                       $"-> Target Bone: '{currentBoneName}'\n" +
                       $"-> Target Unit: '{currentUnitName}'\n" +
                       $"-> Source Text Length: {sourceTextContext.Length} characters.");

        if (string.IsNullOrWhiteSpace(sourceTextContext))
        {
            Debug.LogError("[SIMPLE_ANLAT] WARNING: The text extracted from infoBodyText container layout is completely empty.");
        }

        if (_requestRoutine != null)
        {
            Debug.LogError("[SIMPLE_ANLAT] Stale ongoing coroutine network loop detected. Resetting running request pipeline frames.");
            StopCoroutine(_requestRoutine);
        }

        _requestId++;
        Debug.LogError($"[SIMPLE_ANLAT] Starting routine loop sequence initialization. Request ID token: #{_requestId}");
        _requestRoutine = StartCoroutine(RequestSimpleExplanationRoutine(_requestId, currentBoneName, currentUnitName, sourceTextContext));
    }

    private IEnumerator RequestSimpleExplanationRoutine(int requestId, string boneName, string unitName, string textToSimplify)
    {
        Debug.LogError($"[SIMPLE_ANLAT] Coroutine execution frame open for Request ID: #{requestId}");
        simpleDescriptionText.text = LoadingMessage;

        SimpleExplanationRequest payload = new SimpleExplanationRequest
        {
            bone_name = boneName,
            unit_name = unitName,
            original_text = textToSimplify
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        Debug.LogError($"[SIMPLE_ANLAT] Outgoing JSON Data serialized payload payload layout string:\n{jsonPayload}");

        byte[] rawBody = Encoding.UTF8.GetBytes(jsonPayload);

        Debug.LogError($"[SIMPLE_ANLAT] Dispatching WebRequest asset data stream to target connection endpoint address: {backendUrl}");
        using (UnityWebRequest req = new UnityWebRequest(backendUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(rawBody);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            yield return req.SendWebRequest();

            // Request synchronization lifecycle check
            if (requestId != _requestId)
            {
                Debug.LogError($"[SIMPLE_ANLAT] Request ID mismatch frame break triggered. Loop identity belongs to data index token context #{requestId}, but execution index is at context #{_requestId}. Breaking loop execution safely.");
                yield break;
            }

            // Connection success check status verification
            if (req.result != UnityWebRequest.Result.Success)
            {
                ApplyErrorFallback($"Network Interaction Pipeline Failed!\n" +
                                   $"Error message trace info: {req.error}\n" +
                                   $"Server Response status numerical data index: {req.responseCode}\n" +
                                   $"Raw received stream output trace:\n{req.downloadHandler.text}");
                yield break;
            }

            string responseJson = req.downloadHandler.text;
            Debug.LogError($"[SIMPLE_ANLAT] Network response payload packet stream received successfully from server endpoint layout:\n{responseJson}");

            SimpleExplanationResponse response = null;
            try
            {
                response = JsonUtility.FromJson<SimpleExplanationResponse>(responseJson);
                Debug.LogError("[SIMPLE_ANLAT] Text parsing action executed correctly. Mapping JSON fields straight onto memory allocation handles.");
            }
            catch (Exception ex)
            {
                ApplyErrorFallback($"CRITICAL STRUCTURAL PARSING FAILURE: System structural framework failed to format JSON content into target script classes data templates. Reasoning trace:\n{ex.Message}");
                yield break;
            }

            if (response == null)
            {
                ApplyErrorFallback("Data Mapping returned an empty or completely uninstantiated class pointer model object instance reference.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(response.simple_explanation))
            {
                ApplyErrorFallback("API parsing sequence processed request, but returned an empty value or white spaces matching the internal 'simple_explanation' node data parameter layout strings keys.");
                yield break;
            }

            // SUCCESS FLOW DATA HANDSHAKE PATTERNS
            string finalSimpleText = response.simple_explanation.Trim();
            Debug.LogError($"[SIMPLE_ANLAT] Handshake complete context evaluation frame success parameters!\n" +
                           $"-> Received text string length: {finalSimpleText.Length} characters.\n" +
                           $"-> Forwarding output data packet straight onto UI destination panel view container object.");

            simpleDescriptionText.text = finalSimpleText;

            if (TTSClient.Instance != null)
            {
                Debug.LogError($"[SIMPLE_ANLAT] Invoking dynamic playback audio thread generation on centralized global TTS system for output payload text details.");
                TTSClient.Instance.Speak(finalSimpleText);
            }
            else
            {
                Debug.LogError("[SIMPLE_ANLAT] WARNING: Dynamic speech frame execution skipped because fallback state validation on global TTSClient instance is NULL.");
            }

            _requestRoutine = null;
        }
    }

    private void ApplyErrorFallback(string debugErrorMessage)
    {
        Debug.LogError($"[SIMPLE_ANLAT] ERROR TERMINATION LOG RECEIVED inside local execution state container context loop blocks:\n{debugErrorMessage}", this);

        simpleDescriptionText.text = "Basit anlatým yüklenemedi. Lütfen orijinal açýklamayý inceleyin veya að sunucusunu kontrol edin.";
        _requestRoutine = null;
    }
}