using System.Collections.Generic;
using UnityEngine;

public class MatchingArrowManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform arrowLayer;

    [Header("Prefab")]
    [SerializeField] private MatchingArrow arrowPrefab;

    [Header("Preview Arrow")]
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float previewThickness = 7f;

    [Header("Confirmed Arrow")]
    [SerializeField] private Color confirmedColor = new Color(1f, 0.45f, 0f, 1f);
    [SerializeField] private float confirmedThickness = 8f;

    private MatchingArrow previewArrow;
    private RectTransform previewFromBox;
    private RectTransform previewToBox;

    private readonly Dictionary<RectTransform, RectTransform> confirmedTargets = new();
    private readonly Dictionary<RectTransform, MatchingArrow> confirmedArrows = new();

    private readonly Dictionary<RectTransform, RectTransform> leftBoxByRightBox = new();

    public void ShowPreview(RectTransform fromBox, RectTransform toBox)
    {
        if (fromBox == null || toBox == null)
            return;

        previewFromBox = fromBox;
        previewToBox = toBox;

        if (previewArrow == null)
        {
            previewArrow = Instantiate(arrowPrefab, arrowLayer);
        }

        previewArrow.gameObject.SetActive(true);
        previewArrow.SetVisual(previewColor, previewThickness);
        UpdateArrow(previewArrow, fromBox, toBox);
    }

    public void HidePreview()
    {
        previewFromBox = null;
        previewToBox = null;

        if (previewArrow != null)
            previewArrow.gameObject.SetActive(false);
    }

    public void ConfirmMatch(RectTransform fromBox, RectTransform toBox)
    {
        if (fromBox == null || toBox == null)
            return;

        // Eğer sağ kutu daha önce başka sol kutuyla eşleştiyse eski eşleşmeyi kaldır.
        if (leftBoxByRightBox.TryGetValue(toBox, out RectTransform previousLeftBox))
        {
            RemoveMatch(previousLeftBox);
        }

        // Eğer bu sol kutunun eski eşleşmesi varsa onu kaldır.
        RemoveMatch(fromBox);

        MatchingArrow confirmedArrow = Instantiate(arrowPrefab, arrowLayer);
        confirmedArrow.SetVisual(confirmedColor, confirmedThickness);

        confirmedTargets[fromBox] = toBox;
        confirmedArrows[fromBox] = confirmedArrow;
        leftBoxByRightBox[toBox] = fromBox;

        UpdateArrow(confirmedArrow, fromBox, toBox);

        HidePreview();
    }

    public void RemoveMatch(RectTransform fromBox)
    {
        if (fromBox == null)
            return;

        if (confirmedTargets.TryGetValue(fromBox, out RectTransform oldRightBox))
        {
            confirmedTargets.Remove(fromBox);

            if (leftBoxByRightBox.ContainsKey(oldRightBox))
                leftBoxByRightBox.Remove(oldRightBox);
        }

        if (confirmedArrows.TryGetValue(fromBox, out MatchingArrow oldArrow))
        {
            if (oldArrow != null)
                Destroy(oldArrow.gameObject);

            confirmedArrows.Remove(fromBox);
        }
    }

    public void ClearAll()
    {
        HidePreview();

        foreach (MatchingArrow arrow in confirmedArrows.Values)
        {
            if (arrow != null)
                Destroy(arrow.gameObject);
        }

        confirmedTargets.Clear();
        confirmedArrows.Clear();
        leftBoxByRightBox.Clear();
    }

    private void LateUpdate()
    {
        if (previewArrow != null && previewArrow.gameObject.activeSelf)
        {
            if (previewFromBox != null && previewToBox != null)
                UpdateArrow(previewArrow, previewFromBox, previewToBox);
        }

        foreach (var pair in confirmedTargets)
        {
            RectTransform fromBox = pair.Key;
            RectTransform toBox = pair.Value;

            if (fromBox == null || toBox == null)
                continue;

            if (confirmedArrows.TryGetValue(fromBox, out MatchingArrow arrow))
            {
                if (arrow != null)
                    UpdateArrow(arrow, fromBox, toBox);
            }
        }
    }

    private void UpdateArrow(MatchingArrow arrow, RectTransform fromBox, RectTransform toBox)
    {
        Vector2 startPoint = GetLocalSidePoint(fromBox, true);
        Vector2 endPoint = GetLocalSidePoint(toBox, false);

        arrow.SetPoints(startPoint, endPoint);
    }

    private Vector2 GetLocalSidePoint(RectTransform target, bool useRightSide)
    {
        Vector3 worldPoint = GetWorldSidePoint(target, useRightSide);

        Camera cameraToUse = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraToUse = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cameraToUse, worldPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            arrowLayer,
            screenPoint,
            cameraToUse,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector3 GetWorldSidePoint(RectTransform rectTransform, bool useRightSide)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // corners:
        // 0 = bottom left
        // 1 = top left
        // 2 = top right
        // 3 = bottom right

        if (useRightSide)
            return (corners[2] + corners[3]) / 2f;

        return (corners[0] + corners[1]) / 2f;
    }
}