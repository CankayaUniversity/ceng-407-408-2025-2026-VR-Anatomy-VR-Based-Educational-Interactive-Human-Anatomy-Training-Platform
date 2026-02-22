using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonTest : MonoBehaviour
{
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Button askButton;
    [SerializeField] private TMP_Text answerText;

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

        answerText.text = $"Soru geldi ✅\n{q}";
    }
}
