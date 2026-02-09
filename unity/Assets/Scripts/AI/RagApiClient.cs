using System;
using System.Collections;
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
    [SerializeField] private string apiUrl = "http://127.0.0.1:8000/ask";

    [Serializable]
    private class AskRequest
    {
        public string question;
    }

    [Serializable]
    private class AskResponse
    {
        public string answer;
    }

    private void Awake()
    {
        askButton.onClick.AddListener(OnAskClicked);
    }

    private void OnDestroy()
    {
        askButton.onClick.RemoveListener(OnAskClicked);
    }

    private void OnAskClicked()
    {
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
        // JSON body hazırla
        var payload = new AskRequest { question = question };
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(apiUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                answerText.text =
                    $"İstek başarısız ❌\n" +
                    $"Hata: {req.error}\n" +
                    $"HTTP: {req.responseCode}\n" +
                    $"URL: {apiUrl}";
                yield break;
            }

            string responseJson = req.downloadHandler.text;

            // {"answer":"..."} bekliyoruz
            try
            {
                var resp = JsonUtility.FromJson<AskResponse>(responseJson);
                answerText.text = string.IsNullOrEmpty(resp?.answer)
                    ? "Cevap boş geldi 🫥"
                    : resp.answer;
            }
            catch
            {
                answerText.text = "JSON parse edemedim 😵\nRaw:\n" + responseJson;
            }
        }
    }
}
