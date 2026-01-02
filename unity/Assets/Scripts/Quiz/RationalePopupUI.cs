using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RationalePopupUI : MonoBehaviour
{
    public bool isPopupOpen = false;
    [Header("UI References")]
    [SerializeField] private TMP_Text rationaleText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        // Ensure popup starts hidden
        gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

 
    /// Shows the rationale popup with given explanation text
  
    public void Show(string rationale)
    {
        isPopupOpen = true;
        rationaleText.text = rationale;
        gameObject.SetActive(true);
    }

    /// Hides the popup

    public void Hide()
    {
        isPopupOpen = false;
        gameObject.SetActive(false);
    }
}
