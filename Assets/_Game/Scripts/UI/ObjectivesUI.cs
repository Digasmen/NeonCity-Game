using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectivesUI : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI progressText;
    private Graphic progressBar;
    private Image   leftBar;   // accent bar — pulses when near goal

    private CanvasGroup _contentGroup;
    private bool        _transitioning;

    void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        CreateUI(canvas.transform);
        MilestoneManager.Instance.OnMilestoneChanged += OnMilestoneChanged;
    }

    void CreateUI(Transform canvasTransform)
    {
        panel = new GameObject("ObjectivesPanel");
        panel.transform.SetParent(canvasTransform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0, 0.5f);
        rect.anchorMax        = new Vector2(0, 0.5f);
        rect.pivot            = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(10, 0);
        rect.sizeDelta        = new Vector2(215, 115);

        UIUtils.Rounded(panel, UIUtils.PanelBg, 12);

        // Top accent glow
        var topAccent = new GameObject("TopAccent"); topAccent.transform.SetParent(panel.transform, false);
        var tar = topAccent.AddComponent<RectTransform>();
        tar.anchorMin = new Vector2(0, 1); tar.anchorMax = new Vector2(1, 1);
        tar.pivot = new Vector2(0.5f, 1); tar.sizeDelta = new Vector2(0, 1);
        topAccent.AddComponent<Image>().color = new Color(0f, 0.8f, 1f, 0.5f);

        // Left accent bar
        var leftBarGO = new GameObject("LeftBar"); leftBarGO.transform.SetParent(panel.transform, false);
        var lbr = leftBarGO.AddComponent<RectTransform>();
        lbr.anchorMin = Vector2.zero; lbr.anchorMax = new Vector2(0, 1);
        lbr.pivot = new Vector2(0, 0.5f); lbr.sizeDelta = new Vector2(2, 0);
        leftBar = leftBarGO.AddComponent<Image>();
        leftBar.color = new Color(0f, 0.8f, 1f, 0.35f);

        // OBJECTIVES header
        CreateLabel(panel.transform, "OBJECTIVES", 11, FontStyles.Bold,
            new Vector2(0, 0.80f), new Vector2(1, 1f), new Color(0.3f, 0.78f, 1f));

        // Divider
        var div = new GameObject("Divider"); div.transform.SetParent(panel.transform, false);
        var dr = div.AddComponent<RectTransform>();
        dr.anchorMin = new Vector2(0.04f, 0.78f); dr.anchorMax = new Vector2(0.96f, 0.79f);
        dr.offsetMin = Vector2.zero; dr.offsetMax = Vector2.zero;
        div.AddComponent<Image>().color = new Color(0f, 0.7f, 1f, 0.2f);

        // Description
        descText = CreateLabel(panel.transform, "", 10.5f, FontStyles.Normal,
            new Vector2(0, 0.42f), new Vector2(1, 0.78f), new Color(0.9f, 0.9f, 1f));
        descText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Progress numbers
        progressText = CreateLabel(panel.transform, "", 9.5f, FontStyles.Normal,
            new Vector2(0, 0.26f), new Vector2(1, 0.42f), new Color(0.35f, 1f, 0.55f));

        // Progress bar background
        var barBg = new GameObject("BarBg"); barBg.transform.SetParent(panel.transform, false);
        var barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.04f, 0.07f);
        barBgRect.anchorMax = new Vector2(0.96f, 0.24f);
        barBgRect.offsetMin = Vector2.zero; barBgRect.offsetMax = Vector2.zero;
        UIUtils.Rounded(barBg, new Color(0.04f, 0.06f, 0.15f, 1f), 4);

        // Progress bar fill
        var barFill = new GameObject("BarFill"); barFill.transform.SetParent(barBg.transform, false);
        var fillRect = barFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        progressBar = UIUtils.Rounded(barFill, new Color(0.15f, 0.75f, 1f), 4);

        _contentGroup = panel.AddComponent<CanvasGroup>();
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, float size, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(8, 2); r.offsetMax = new Vector2(-6, -2);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    void Update()
    {
        if (_transitioning) return;
        if (MilestoneManager.Instance.Current == null)
        {
            descText.text    = "All objectives complete!";
            progressText.text = "";
            progressBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            return;
        }

        MilestoneData m = MilestoneManager.Instance.Current;
        float current  = ResourceManager.Instance.Get(m.resourceType);
        float progress = Mathf.Clamp01(current / m.targetAmount);

        if (m.hasSecondCondition)
        {
            float cur2  = ResourceManager.Instance.Get(m.secondResourceType);
            float prog2 = Mathf.Clamp01(cur2 / m.secondTargetAmount);
            progress = (progress + prog2) * 0.5f;
            progressText.text = $"{current:0}/{m.targetAmount:0}  ·  {cur2:0}/{m.secondTargetAmount:0}";
        }
        else
        {
            progressText.text = $"{current:0} / {m.targetAmount:0}";
        }

        progressBar.rectTransform.anchorMax = new Vector2(progress, 1f);

        // Color shifts cyan → green → gold as you approach goal
        Color barLow  = new Color(0.15f, 0.75f, 1.00f);
        Color barMid  = new Color(0.20f, 1.00f, 0.45f);
        Color barHigh = new Color(1.00f, 0.80f, 0.10f);
        progressBar.color = progress < 0.6f
            ? Color.Lerp(barLow, barMid,  progress / 0.6f)
            : Color.Lerp(barMid, barHigh, (progress - 0.6f) / 0.4f);

        // Near-goal pulse: bar and left accent bar flash brighter above 90 %
        if (progress >= 0.9f)
        {
            float pulse = (Mathf.Sin(Time.time * 5.5f) + 1f) * 0.5f;
            progressBar.color = Color.Lerp(barHigh, new Color(1f, 1f, 0.55f), pulse * 0.6f);
            if (leftBar != null)
                leftBar.color = Color.Lerp(
                    new Color(1f, 0.80f, 0.10f, 0.35f),
                    new Color(1f, 0.90f, 0.30f, 0.90f), pulse);
        }
        else if (leftBar != null)
        {
            leftBar.color = new Color(0f, 0.8f, 1f, 0.35f);
        }
    }

    void OnMilestoneChanged(MilestoneData milestone)
    {
        if (milestone == null) return;
        if (_transitioning) StopAllCoroutines();
        StartCoroutine(MilestoneFade(milestone.description));
    }

    IEnumerator MilestoneFade(string newDesc)
    {
        _transitioning = true;

        // Fade out
        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            _contentGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.2f);
            yield return null;
        }
        _contentGroup.alpha = 0f;
        descText.text = newDesc;

        // Fade in
        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            _contentGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.3f);
            yield return null;
        }
        _contentGroup.alpha = 1f;

        _transitioning = false;
    }
}
