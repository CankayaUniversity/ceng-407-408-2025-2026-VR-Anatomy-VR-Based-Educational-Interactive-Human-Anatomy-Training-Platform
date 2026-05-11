using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerDebug : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"{name} → POINTER ENTER ✅");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"{name} → POINTER EXIT");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"{name} → POINTER DOWN ✅");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"{name} → POINTER UP ✅");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"{name} → POINTER CLICK ✅");
    }
}