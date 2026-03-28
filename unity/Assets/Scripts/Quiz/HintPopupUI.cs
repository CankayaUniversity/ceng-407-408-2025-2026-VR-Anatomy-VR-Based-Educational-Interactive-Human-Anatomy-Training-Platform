using TMPro;
using UnityEngine;

public class HintPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text hintText;

    public void Show(string text)
    {
        if (hintText != null)
            hintText.text = text;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public bool IsOpen()
    {
        return gameObject.activeSelf;
    }
}