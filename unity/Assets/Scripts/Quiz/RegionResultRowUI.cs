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
            countsText.text = $"Doğru: {data.correctCount}   Yanlış: {data.wrongCount}   Boş: {data.unansweredCount}";

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
            return "Bilinmiyor";

        switch (rawName)
        {
            case "Head_Face":
                return "Baş Kemikleri";

            case "Muscle":
                return "Kaslar";

            case "Joints":
                return "Eklemler";

            case "Trunk":
                return "Gövde Kemikleri";

            case "Upper_Extremity":
                return "Üst Ekstremite Kemikleri";

            case "Lower_Extremity":
                return "Alt Ekstremite Kemikleri";

            default:
                return rawName.Replace("_", " ");
        }
    }
}