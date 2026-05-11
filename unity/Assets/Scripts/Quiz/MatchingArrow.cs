using UnityEngine;
using UnityEngine.UI;

public class MatchingArrow : MonoBehaviour
{
    [Header("Line Body")]
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private Image lineImage;

    [Header("Arrow Head")]
    [SerializeField] private RectTransform arrowHeadRect;
    [SerializeField] private Image arrowHeadImage;

    [Header("Settings")]
    [SerializeField] private float arrowHeadSize = 30f;
    [SerializeField] private float arrowHeadBackOffset = 10f;

    private float currentThickness = 8f;

    private static Sprite generatedLineSprite;
    private static Sprite generatedArrowHeadSprite;

    private void Awake()
    {
        AutoFindMissingReferences();
        PrepareLineImage();
        PrepareArrowHeadImage();
    }

    private void AutoFindMissingReferences()
    {
        if (lineRect == null)
        {
            Transform lineTransform = transform.Find("Line");

            if (lineTransform != null)
                lineRect = lineTransform.GetComponent<RectTransform>();
        }

        if (lineImage == null && lineRect != null)
            lineImage = lineRect.GetComponent<Image>();

        if (arrowHeadRect == null)
        {
            Transform arrowHeadTransform = transform.Find("ArrowHead");

            if (arrowHeadTransform != null)
                arrowHeadRect = arrowHeadTransform.GetComponent<RectTransform>();
        }

        if (arrowHeadImage == null && arrowHeadRect != null)
            arrowHeadImage = arrowHeadRect.GetComponent<Image>();
    }

    private void PrepareLineImage()
    {
        if (lineRect == null)
        {
            Debug.LogWarning("MatchingArrow: Line Rect bulunamadı. Prefab içinde 'Line' objesi var mı?");
            return;
        }

        if (lineImage == null)
            lineImage = lineRect.gameObject.AddComponent<Image>();

        lineRect.gameObject.SetActive(true);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);

        lineImage.sprite = GetGeneratedLineSprite();
        lineImage.type = Image.Type.Simple;
        lineImage.raycastTarget = false;
    }

    private void PrepareArrowHeadImage()
    {
        if (arrowHeadRect == null)
        {
            Debug.LogWarning("MatchingArrow: ArrowHead Rect bulunamadı. Prefab içinde 'ArrowHead' objesi var mı?");
            return;
        }

        if (arrowHeadImage == null)
            arrowHeadImage = arrowHeadRect.gameObject.AddComponent<Image>();

        arrowHeadRect.gameObject.SetActive(true);
        arrowHeadRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowHeadRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowHeadRect.pivot = new Vector2(0.5f, 0.5f);

        arrowHeadImage.sprite = GetGeneratedArrowHeadSprite();
        arrowHeadImage.type = Image.Type.Simple;
        arrowHeadImage.raycastTarget = false;
    }

    public void SetPoints(Vector2 startPoint, Vector2 endPoint)
    {
        if (lineRect == null || arrowHeadRect == null)
        {
            Debug.LogWarning("MatchingArrow: Line veya ArrowHead bağlı değil.");
            return;
        }

        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 normalizedDirection = direction.normalized;

        float lineLength = Mathf.Max(0f, distance - arrowHeadSize * 0.6f);

        lineRect.gameObject.SetActive(true);
        lineRect.anchoredPosition = startPoint;
        lineRect.sizeDelta = new Vector2(lineLength, currentThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        arrowHeadRect.gameObject.SetActive(true);
        arrowHeadRect.anchoredPosition = endPoint - normalizedDirection * arrowHeadBackOffset;
        arrowHeadRect.sizeDelta = new Vector2(arrowHeadSize, arrowHeadSize);
        arrowHeadRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetVisual(Color color, float thickness)
    {
        currentThickness = Mathf.Max(4f, thickness);

        if (lineImage != null)
        {
            lineImage.gameObject.SetActive(true);
            lineImage.sprite = GetGeneratedLineSprite();
            lineImage.color = color;
            lineImage.raycastTarget = false;
        }

        if (arrowHeadImage != null)
        {
            arrowHeadImage.gameObject.SetActive(true);
            arrowHeadImage.sprite = GetGeneratedArrowHeadSprite();
            arrowHeadImage.color = color;
            arrowHeadImage.raycastTarget = false;
        }

        if (lineRect != null)
            lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, currentThickness);
    }

    private static Sprite GetGeneratedLineSprite()
    {
        if (generatedLineSprite != null)
            return generatedLineSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        generatedLineSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f)
        );

        return generatedLineSprite;
    }

    private static Sprite GetGeneratedArrowHeadSprite()
    {
        if (generatedArrowHeadSprite != null)
            return generatedArrowHeadSprite;

        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        int baseX = 10;
        int tipX = size - 6;
        int centerY = size / 2;
        int maxHalfHeight = 24;

        for (int x = baseX; x <= tipX; x++)
        {
            float t = (float)(x - baseX) / (tipX - baseX);
            int halfHeight = Mathf.RoundToInt(maxHalfHeight * (1f - t));

            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
            {
                if (y >= 0 && y < size)
                    texture.SetPixel(x, y, white);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        generatedArrowHeadSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );

        return generatedArrowHeadSprite;
    }
}