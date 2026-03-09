using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchingItemUI : MonoBehaviour
{
    public TMP_Text labelText;
    public Image backgroundImage;

    public void Setup(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }
}