using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RegionResultRowUI : MonoBehaviour
{
    public TMP_Text regionNameText;
    public TMP_Text countsText;
    public TMP_Text successText;
    public Image fillImage;

    public void Setup(RegionAnalysisResult data)
    {
        if (regionNameText != null)
            regionNameText.text = FormatRegionName(data.region);

        if (countsText != null)
            countsText.text = $"Correct: {data.correctCount}   Wrong: {data.wrongCount}";

        if (successText != null)
            successText.text = $"%{data.averageScoreRatio * 100f:0}";

        if (fillImage != null)
        {
            float displayFill = data.averageScoreRatio;

            if (displayFill <= 0f)
                displayFill = 0.03f;

            fillImage.fillAmount = displayFill;
            fillImage.color = GetStatusColor(data.status);
        }
    }

    private Color GetStatusColor(RegionPerformanceStatus status)
    {
        switch (status)
        {
            case RegionPerformanceStatus.Red:
                return Color.red;

            case RegionPerformanceStatus.Yellow:
                return Color.yellow;

            case RegionPerformanceStatus.Green:
                return Color.green;

            default:
                return Color.gray;
        }
    }

    private string FormatRegionName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "Unknown";

        return rawName.Replace("_", " ");
    }
}