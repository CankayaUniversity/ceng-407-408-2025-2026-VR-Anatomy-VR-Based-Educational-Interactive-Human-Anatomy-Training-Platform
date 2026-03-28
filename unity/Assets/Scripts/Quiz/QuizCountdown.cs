using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class QuizCountdown : MonoBehaviour
{
    public event Action OnCountdownComplete;

    static readonly Color TextColor = new Color(0f, 0.85f, 1f, 1f);
    static readonly Color GlowColor = new Color(0f, 0.75f, 1f, 0.5f);

    const int CountFrom = 3;
    const float StepDuration = 0.85f;
    const float ScalePunch = 1.2f;

    public void StartCountdown(Transform parent)
    {
        StartCoroutine(RunCountdown(parent));
    }

    private IEnumerator RunCountdown(Transform parent)
    {
        var obj = new GameObject("CountdownText");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.3f);
        rect.anchorMax = new Vector2(0.7f, 0.7f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 35;
        tmp.fontSizeMax = 70;
        tmp.color = TextColor;
        tmp.fontStyle = FontStyles.Bold;

        if (tmp.fontSharedMaterial != null)
        {
            var mat = new Material(tmp.fontSharedMaterial);
            mat.EnableKeyword("GLOW_ON");
            mat.SetFloat("_GlowOffset", 0.4f);
            mat.SetFloat("_GlowOuter", 0.35f);
            mat.SetFloat("_GlowInner", 0.1f);
            mat.SetFloat("_GlowPower", 0.6f);
            mat.SetColor("_GlowColor", GlowColor);
            tmp.fontMaterial = mat;
        }

        for (int i = CountFrom; i >= 1; i--)
        {
            tmp.text = i.ToString();
            tmp.alpha = 1f;
            rect.localScale = Vector3.one * ScalePunch;

            float elapsed = 0f;
            while (elapsed < StepDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / StepDuration;
                float scale = Mathf.Lerp(ScalePunch, 1f, t);
                float alpha = Mathf.Lerp(1f, 0.3f, t * t);
                rect.localScale = Vector3.one * scale;
                tmp.alpha = alpha;
                yield return null;
            }
        }

        Destroy(obj);
        OnCountdownComplete?.Invoke();
        Destroy(this);
    }
}
