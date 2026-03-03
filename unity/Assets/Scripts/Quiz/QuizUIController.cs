using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuizUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text timerText;
    public Transform answerContainer;
    public Button answerButtonPrefab;
    public Button nextButton;
    public RationalePopupUI rationalePopup;

    [Header("Manager")]
    public QuizManager quizManager;

    private List<AnswerButtonUI> spawnedButtons = new List<AnswerButtonUI>();

    private void Start()
    {
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    public void ShowQuestion(Question q)
    {
        questionText.text = q.body;

        nextButton.gameObject.SetActive(false);

        // ✅ Her yeni soruda popup kapalı
        rationalePopup.Hide();

        ClearButtons();

        CreateButton("A", q.A);
        CreateButton("B", q.B);
        CreateButton("C", q.C);
        CreateButton("D", q.D);
    }

    void CreateButton(string key, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Button btn = Instantiate(answerButtonPrefab, answerContainer);
        AnswerButtonUI buttonUI = btn.GetComponent<AnswerButtonUI>();

        buttonUI.Setup(key, text, this);
        spawnedButtons.Add(buttonUI);
    }

    public void OnAnswerSelected(string optionKey)
    {
        quizManager.SubmitAnswer(optionKey);
    }

    public void ShowAnswerResult(string correctOption, string selectedOption, string rationale)
    {
        bool isCorrect = selectedOption == correctOption;

        foreach (var btn in spawnedButtons)
        {
            btn.Disable();

            if (btn.OptionKey == correctOption)
                btn.SetCorrect();
            else if (btn.OptionKey == selectedOption)
                btn.SetWrong();
        }

        // SHOW POPUP ONLY IF ANSWER IS WRONG
        if (!isCorrect && !string.IsNullOrEmpty(rationale))
        {
            rationalePopup.Show(rationale);
        }

        nextButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// ✅ QuizManager time-up olunca burayı çağırıyor.
    /// İstek: Popup çıkmasın, seçenekler görünmesin, sadece mesaj + Devam kalsın.
    /// </summary>
    public void ShowTimeUpResult(string message)
    {
        // Mesaj
        questionText.text = message;

        // ✅ Popup istemiyoruz (kesin kapat)
        rationalePopup.Hide();

        // ✅ Seçenekler görünmesin (tamamen sil)
        ClearButtons();

        // ✅ Devam butonu görünsün
        nextButton.gameObject.SetActive(true);
    }

    private void OnNextButtonPressed()
    {
        // ✅ Popup kullanmayacağız dediğin için bu kontrolü kaldırmak en temiz çözüm.
        // (Zaten time-up'ta popup yok; yanlış cevapta popup varsa da kullanıcı popup'ı kapatmadan geçemesin
        // istiyorsan aşağıdaki satırı geri ekleyebilirsin.)
        quizManager.NextQuestion();

        // Eğer "popup açıkken next çalışmasın" istiyorsan şu şekilde kullan:
        // if (rationalePopup.isPopupOpen == false) quizManager.NextQuestion();
    }

    public void UpdateTimer(float time)
    {
        // time < 0 => süresiz soru
        if (time < 0f)
        {
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            return;
        }

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        timerText.text = Mathf.CeilToInt(time).ToString();
    }

    /// <summary>
    /// ✅ Bu artık "gerçek quiz bitti" ekranı.
    /// </summary>
    public void ShowQuizFinished()
    {
        questionText.text = "Quiz tamamlandı!";
        ClearButtons();
        nextButton.gameObject.SetActive(false);
        rationalePopup.Hide();
    }

    void ClearButtons()
    {
        foreach (Transform child in answerContainer)
            Destroy(child.gameObject);

        spawnedButtons.Clear();
    }
}