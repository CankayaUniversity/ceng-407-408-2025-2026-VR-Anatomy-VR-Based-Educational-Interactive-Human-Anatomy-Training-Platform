using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class MatchingItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler
{
    [Header("UI")]
    public TMP_Text labelText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color draggingColor = new Color(0.8f, 0.8f, 0.8f, 1f);       // gri
    public Color matchedColor = new Color(1f, 0.75f, 0.35f, 1f);        // turuncumsu
    public Color correctColor = new Color(0.3f, 0.9f, 0.3f, 1f);        // yeşil
    public Color wrongColor = new Color(0.9f, 0.3f, 0.3f, 1f);          // kırmızı

    private QuizUIController controller;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;

    private Vector2 originalAnchoredPosition;
    private int itemIndex;
    private bool isLeftSide;

    public int ItemIndex => itemIndex;
    public bool IsLeftSide => isLeftSide;
    private bool isMatched = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Pointer hedefin üstüne geldi: {labelText.text}");
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        canvasGroup.blocksRaycasts = true;
    }

    public void Setup(string text, int index, bool isLeft, QuizUIController quizUIController)
    {
        if (labelText != null)
            labelText.text = text;

        itemIndex = index;
        isLeftSide = isLeft;
        controller = quizUIController;

        ResetVisual();

        Debug.Log($"Setup tamamlandı: {text} | index={index} | isLeft={isLeft}");
    }

    public void ResetVisual()
    {
        isMatched = false;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public void SetMatched(bool matched)
    {
        isMatched = matched;

        if (backgroundImage == null)
            return;

        backgroundImage.color = matched ? matchedColor : normalColor;
    }

    public void SetCorrect()
    {
        if (backgroundImage != null)
            backgroundImage.color = correctColor;
    }

    public void SetWrong()
    {
        if (backgroundImage != null)
            backgroundImage.color = wrongColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"OnBeginDrag: {labelText.text}");

        if (controller == null || controller.IsMatchingSubmitted())
            return;

        originalAnchoredPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        if (backgroundImage != null)
            backgroundImage.color = draggingColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller == null || controller.IsMatchingSubmitted())
            return;

        if (parentCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"OnEndDrag: {labelText.text}");

        if (controller == null || controller.IsMatchingSubmitted())
            return;

        rectTransform.anchoredPosition = originalAnchoredPosition;
        canvasGroup.blocksRaycasts = true;
        if (backgroundImage != null)
            backgroundImage.color = isMatched ? matchedColor : normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"OnDrop tetiklendi: hedef={labelText.text}");

        if (controller == null || controller.IsMatchingSubmitted())
            return;

        if (eventData.pointerDrag == null)
        {
            Debug.Log("OnDrop: pointerDrag null");
            return;
        }

        MatchingItemUI draggedItem = eventData.pointerDrag.GetComponent<MatchingItemUI>();

        if (draggedItem == null)
        {
            Debug.Log("OnDrop: draggedItem null");
            return;
        }

        Debug.Log($"Sürüklenen item: {draggedItem.labelText.text} -> Hedef item: {labelText.text}");

        if (draggedItem == this)
        {
            Debug.Log("OnDrop: item kendisine bırakıldı");
            return;
        }

        if (draggedItem.IsLeftSide == this.IsLeftSide)
        {
            Debug.Log("OnDrop: aynı taraftaki iteme bırakıldı, eşleşme yapılmaz");
            return;
        }

        int leftIndex;
        int rightIndex;

        if (draggedItem.IsLeftSide && !this.IsLeftSide)
        {
            leftIndex = draggedItem.ItemIndex;
            rightIndex = this.ItemIndex;
        }
        else if (!draggedItem.IsLeftSide && this.IsLeftSide)
        {
            leftIndex = this.ItemIndex;
            rightIndex = draggedItem.ItemIndex;
        }
        else
        {
            Debug.Log("OnDrop: geçersiz drop kombinasyonu");
            return;
        }

        Debug.Log($"RegisterMatch çağrılıyor: Left {leftIndex} -> Right {rightIndex}");
        controller.RegisterMatch(leftIndex, rightIndex);
    }
}