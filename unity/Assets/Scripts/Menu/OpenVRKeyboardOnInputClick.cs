using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class OpenVRKeyboardOnInputClick : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    [Header("Keyboard")]
    [SerializeField] private VRAnatomyVirtualKeyboard keyboard;

    [Header("Optional")]
    [Tooltip("Bu input aktifken klavyedeki GÖNDER tuşuna basılırsa bu buton çalışır. Boş bırakabilirsin.")]
    [SerializeField] private Button submitButtonForThisInput;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenKeyboard();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OpenKeyboard();
    }

    public void OpenKeyboard()
    {
        if (keyboard == null)
        {
            Debug.LogError("[OpenVRKeyboardOnInputClick] Keyboard Inspector'da atanmamış.");
            return;
        }

        keyboard.ShowForInput(inputField, submitButtonForThisInput);
    }
}