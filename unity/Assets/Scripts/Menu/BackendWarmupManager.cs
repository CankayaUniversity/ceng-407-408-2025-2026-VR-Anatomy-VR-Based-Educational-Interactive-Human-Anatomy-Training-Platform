using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BackendWarmupManager : MonoBehaviour
{
    public static BackendWarmupManager Instance { get; private set; }

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "https://vr-anatomy-backend.onrender.com";
    [SerializeField] private float timeoutSeconds = 15f;

    public bool IsBackendReady { get; private set; }
    public bool IsWarmingUp { get; private set; }
    public string LastStatusMessage { get; private set; } = "Backend not checked yet.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(WarmupBackend());
    }

    public IEnumerator WarmupBackend()
    {
        if (IsWarmingUp)
            yield break;

        IsWarmingUp = true;
        IsBackendReady = false;
        LastStatusMessage = "Backend warming up...";

        string url = $"{backendBaseUrl}/warmup";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = Mathf.RoundToInt(timeoutSeconds);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            IsBackendReady = true;
            LastStatusMessage = "Backend ready.";
            Debug.Log("Backend warmup successful: " + request.downloadHandler.text);
        }
        else
        {
            IsBackendReady = false;
            LastStatusMessage = "Backend warmup failed: " + request.error;
            Debug.LogWarning("Backend warmup failed: " + request.error);
        }

        IsWarmingUp = false;
    }
}