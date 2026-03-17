using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class QuizCountdown : MonoBehaviour
{
    TextMeshProUGUI countdownText;
    GameObject countdownObj;
    readonly List<GameObject> hidden = new List<GameObject>();

    public event Action OnCountdownComplete;

    public void StartCountdown(Transform parent)
    {
        BuildText(parent);
        CopyFont(parent);
        HideContent(parent);
        StartCoroutine(Run());
    }

    void BuildText(Transform parent)
    {
        countdownObj = new GameObject("CountdownText");
        countdownObj.transform.SetParent(parent, false);
        countdownObj.transform.SetAsLastSibling();

        var r = countdownObj.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;

        countdownText = countdownObj.AddComponent<TextMeshProUGUI>();
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 100;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.color = new Color(0f, 0.9f, 1f, 0f);
        countdownText.enableAutoSizing = false;
        countdownText.raycastTarget = false;
        countdownText.text = "";
    }

    void CopyFont(Transform parent)
    {
        var src = parent.Find("IntroText");
        if (src == null) return;

        var tmp = src.GetComponent<TextMeshProUGUI>();
        if (tmp == null || tmp.font == null) return;

        countdownText.font = tmp.font;

        var mat = new Material(tmp.fontSharedMaterial);
        mat.EnableKeyword("GLOW_ON");
        mat.SetFloat("_GlowOffset", 0.45f);
        mat.SetFloat("_GlowOuter", 0.40f);
        mat.SetFloat("_GlowInner", 0.15f);
        mat.SetFloat("_GlowPower", 0.70f);
        mat.SetColor("_GlowColor", new Color(0f, 0.9f, 1f, 0.7f));
        countdownText.fontMaterial = mat;
    }

    void HideContent(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            if (child == countdownObj) continue;
            if (!child.activeSelf) continue;
            child.SetActive(false);
            hidden.Add(child);
        }
    }

    IEnumerator Run()
    {
        yield return Step("3", 1f);
        yield return Step("2", 1f);
        yield return FinalStep("1", 0.85f);

        OnCountdownComplete?.Invoke();

        if (countdownObj != null)
            Destroy(countdownObj);
    }

    IEnumerator Step(string text, float duration)
    {
        countdownText.text = text;
        Color cyan = new Color(0f, 0.9f, 1f, 1f);

        float grow   = duration * 0.30f;
        float hold   = duration * 0.50f;
        float shrink = duration * 0.20f;

        float t = 0f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / grow);
            countdownText.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.05f, EaseOutBack(p));
            countdownText.color = new Color(cyan.r, cyan.g, cyan.b, Mathf.Clamp01(p * 2.5f));
            yield return null;
        }

        countdownText.transform.localScale = Vector3.one;
        countdownText.color = cyan;
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < shrink)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / shrink);
            countdownText.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, p);
            countdownText.color = new Color(cyan.r, cyan.g, cyan.b, 1f - p);
            yield return null;
        }
    }

    IEnumerator FinalStep(string text, float duration)
    {
        countdownText.text = text;
        Color cyan = new Color(0f, 0.9f, 1f, 1f);

        float grow = duration * 0.35f;
        float hold = duration * 0.65f;

        float t = 0f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / grow);
            countdownText.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.05f, EaseOutBack(p));
            countdownText.color = new Color(cyan.r, cyan.g, cyan.b, Mathf.Clamp01(p * 2.5f));
            yield return null;
        }

        countdownText.transform.localScale = Vector3.one;
        countdownText.color = cyan;
        yield return new WaitForSeconds(hold);
    }

    static float EaseOutBack(float x)
    {
        const float c = 1.70158f;
        return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3) + c * Mathf.Pow(x - 1f, 2);
    }
}
