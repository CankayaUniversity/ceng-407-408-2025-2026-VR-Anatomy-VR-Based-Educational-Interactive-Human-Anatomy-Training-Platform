using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class MatchingItemUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    public TMP_Text labelText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color draggingColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color matchedColor = new Color(1f, 0.75f, 0.35f, 1f);
    public Color correctColor = new Color(0.3f, 0.9f, 0.3f, 1f);
    public Color wrongColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    [Header("Arrow System")]
    [SerializeField] private MatchingArrowManager arrowManager;

    private QuizUIController controller;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;

    private Vector2 originalAnchoredPosition;
    private int itemIndex;
    private bool isLeftSide;
    private bool isMatched = false;

    public int ItemIndex => itemIndex;
    public bool IsLeftSide => isLeftSide;
    public RectTransform RectTransform => rectTransform;

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

    public void SetArrowManager(MatchingArrowManager manager)
    {
        arrowManager = manager;
    }

    private MatchingArrowManager GetArrowManager(MatchingItemUI otherItem = null)
    {
        if (arrowManager != null)
            return arrowManager;

        if (otherItem != null && otherItem.arrowManager != null)
        {
            arrowManager = otherItem.arrowManager;
            return arrowManager;
        }

#if UNITY_2023_1_OR_NEWER
        arrowManager = FindFirstObjectByType<MatchingArrowManager>();
#else
        arrowManager = FindObjectOfType<MatchingArrowManager>();
#endif

        if (arrowManager == null)
            Debug.LogWarning("Sahnede MatchingArrowManager bulunamadı! ArrowLayer üzerinde MatchingArrowManager component'i var mı kontrol et.");

        return arrowManager;
    }

    public void ResetVisual()
    {
        isMatched = false;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        MatchingArrowManager manager = GetArrowManager();

        if (manager != null)
            manager.HidePreview();
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

        MatchingArrowManager manager = GetArrowManager();

        if (manager != null)
            manager.HidePreview();
        else
            Debug.LogWarning("ArrowManager bulunamadı! Preview temizlenemedi.");
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

        rectTransform.anchoredPosition = originalAnchoredPosition;
        canvasGroup.blocksRaycasts = true;

        if (controller == null || controller.IsMatchingSubmitted())
            return;

        if (backgroundImage != null)
            backgroundImage.color = isMatched ? matchedColor : normalColor;

        MatchingArrowManager manager = GetArrowManager();

        if (manager != null)
            manager.HidePreview();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Pointer hedefin üstüne geldi: {labelText.text}");

        if (controller == null || controller.IsMatchingSubmitted())
            return;

        if (eventData.pointerDrag == null)
            return;

        MatchingItemUI draggedItem = eventData.pointerDrag.GetComponent<MatchingItemUI>();

        if (draggedItem == null)
            return;

        if (!CanMatchWith(draggedItem))
            return;

        MatchingArrowManager manager = GetArrowManager(draggedItem);

        if (manager == null)
        {
            Debug.LogWarning("Preview ok gösterilemedi çünkü ArrowManager bulunamadı.");
            return;
        }

        GetLeftAndRightRects(draggedItem, this, out RectTransform leftRect, out RectTransform rightRect);

        Debug.Log($"Preview ok gösteriliyor: {leftRect.name} -> {rightRect.name}");
        manager.ShowPreview(leftRect, rightRect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MatchingArrowManager manager = GetArrowManager();

        if (manager != null)
            manager.HidePreview();
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

        if (!CanMatchWith(draggedItem))
            return;

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

        MatchingArrowManager manager = GetArrowManager(draggedItem);

        if (manager != null)
        {
            GetLeftAndRightRects(draggedItem, this, out RectTransform leftRect, out RectTransform rightRect);

            Debug.Log($"Kalıcı ok çiziliyor: {leftRect.name} -> {rightRect.name}");
            manager.ConfirmMatch(leftRect, rightRect);
        }
        else
        {
            Debug.LogWarning("Eşleşme yapıldı ama ArrowManager bulunamadığı için ok çizilemedi.");
        }
    }

    private bool CanMatchWith(MatchingItemUI otherItem)
    {
        if (otherItem == null)
            return false;

        if (otherItem == this)
        {
            Debug.Log("Eşleşme yapılmadı: item kendisine bırakıldı.");
            return false;
        }

        if (otherItem.IsLeftSide == this.IsLeftSide)
        {
            Debug.Log("Eşleşme yapılmadı: aynı taraftaki iteme bırakıldı.");
            return false;
        }

        return true;
    }

    private void GetLeftAndRightRects(
        MatchingItemUI firstItem,
        MatchingItemUI secondItem,
        out RectTransform leftRect,
        out RectTransform rightRect)
    {
        if (firstItem.IsLeftSide)
        {
            leftRect = firstItem.RectTransform;
            rightRect = secondItem.RectTransform;
        }
        else
        {
            leftRect = secondItem.RectTransform;
            rightRect = firstItem.RectTransform;
        }
    }
}