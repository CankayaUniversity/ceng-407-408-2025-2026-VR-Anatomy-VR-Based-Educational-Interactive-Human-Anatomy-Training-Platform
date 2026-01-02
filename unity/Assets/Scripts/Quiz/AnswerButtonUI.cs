using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnswerButtonUI : MonoBehaviour
{
    public TMP_Text labelText;
    public Button button;
    public Image background;

    public string OptionKey { get; private set; }

    private QuizUIController ui;

    public void Setup(string key, string text, QuizUIController controller)
    {
        OptionKey = key;
        labelText.text = text;
        ui = controller;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        ResetState();
    }

    void OnClick()
    {
        ui.OnAnswerSelected(OptionKey);
    }

    public void SetCorrect()
    {
        background.color = Color.green;
    }

    public void SetWrong()
    {
        background.color = Color.red;
    }

    public void Disable()
    {
        button.interactable = false;
    }

    void ResetState()
    {
        background.color = Color.white;
        button.interactable = true;
    }
}
