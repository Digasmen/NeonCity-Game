using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingLabel : MonoBehaviour
{
    Building _building;
    Camera _cam;
    Transform _labelRoot;
    CanvasGroup _labelCG;
    TextMeshProUGUI _nameText;
    TextMeshProUGUI _levelText;
    TextMeshProUGUI _rateText;
    Image _dot;
    bool _isVisible = false;

    const float LabelHeight  = 2.3f;
    const float CanvasScale  = 0.012f;

    public void Setup(Building building)
    {
        _building = building;
        _cam = Camera.main;
        Build();
    }

    public void Refresh()
    {
        if (_building == null) return;
        _levelText.text = $"Lv.{_building.level}";
        UpdateRate();
    }

    void Build()
    {
        GameObject root = new GameObject("LabelRoot");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0, LabelHeight, 0);
        root.transform.localScale    = Vector3.one * CanvasScale;
        _labelRoot = root.transform;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform cr = root.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(220, 66);
        _labelCG = root.AddComponent<CanvasGroup>();
        _labelCG.blocksRaycasts = false;
        _labelCG.alpha = 0f;  // hidden by default; shown on click via BuildingPopup

        // Background
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.14f, 0.97f);

        // Accent line at bottom (building glow color)
        var pb = GetComponent<ProceduralBuilding>();
        Color glowCol = pb != null ? pb.GlowColor
                      : (_building.data != null ? _building.data.glowColor : Color.cyan);
        GameObject line = new GameObject("Line");
        line.transform.SetParent(root.transform, false);
        RectTransform lr = line.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(1, 0);
        lr.pivot = new Vector2(0.5f, 0);
        lr.sizeDelta = new Vector2(0, 2);
        line.AddComponent<Image>().color = glowCol;

        // Top accent line (dimmer)
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(root.transform, false);
        RectTransform tlr = topLine.AddComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1);
        tlr.sizeDelta = new Vector2(0, 1);
        topLine.AddComponent<Image>().color = new Color(glowCol.r, glowCol.g, glowCol.b, 0.35f);

        // Colored dot
        GameObject dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(root.transform, false);
        RectTransform dr = dotGO.AddComponent<RectTransform>();
        dr.anchorMin = new Vector2(0, 0.5f); dr.anchorMax = new Vector2(0, 0.5f);
        dr.pivot = new Vector2(0, 0.5f);
        dr.anchoredPosition = new Vector2(8, 2);
        dr.sizeDelta = new Vector2(9, 9);
        _dot = dotGO.AddComponent<Image>();
        _dot.color = glowCol;

        // Building name
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(root.transform, false);
        RectTransform nr = nameGO.AddComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0.44f); nr.anchorMax = new Vector2(0.72f, 1f);
        nr.offsetMin = new Vector2(22, 3); nr.offsetMax = new Vector2(-2, -4);
        _nameText = nameGO.AddComponent<TextMeshProUGUI>();
        _nameText.text      = _building.data.buildingName.ToUpper();
        _nameText.fontSize  = 16;
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.alignment = TextAlignmentOptions.Left;
        _nameText.color     = Color.white;
        _nameText.textWrappingMode  = TMPro.TextWrappingModes.NoWrap;
        _nameText.overflowMode      = TextOverflowModes.Truncate;

        // Level badge background
        GameObject lvlBg = new GameObject("LvlBg");
        lvlBg.transform.SetParent(root.transform, false);
        RectTransform lbr = lvlBg.AddComponent<RectTransform>();
        lbr.anchorMin = new Vector2(0.73f, 0.28f); lbr.anchorMax = new Vector2(0.99f, 0.92f);
        lbr.offsetMin = Vector2.zero; lbr.offsetMax = Vector2.zero;
        lvlBg.AddComponent<Image>().color = new Color(0f, 0.42f, 0.78f, 1f);

        // Level text
        GameObject lvlGO = new GameObject("Level");
        lvlGO.transform.SetParent(root.transform, false);
        RectTransform lvr = lvlGO.AddComponent<RectTransform>();
        lvr.anchorMin = new Vector2(0.73f, 0.28f); lvr.anchorMax = new Vector2(0.99f, 0.92f);
        lvr.offsetMin = Vector2.zero; lvr.offsetMax = Vector2.zero;
        _levelText = lvlGO.AddComponent<TextMeshProUGUI>();
        _levelText.text      = $"Lv.{_building.level}";
        _levelText.fontSize  = 13;
        _levelText.fontStyle = FontStyles.Bold;
        _levelText.alignment = TextAlignmentOptions.Center;
        _levelText.color     = Color.white;

        // Rate line
        GameObject rateGO = new GameObject("Rate");
        rateGO.transform.SetParent(root.transform, false);
        RectTransform rr = rateGO.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 0f); rr.anchorMax = new Vector2(0.72f, 0.46f);
        rr.offsetMin = new Vector2(22, 4); rr.offsetMax = new Vector2(-2, -2);
        _rateText = rateGO.AddComponent<TextMeshProUGUI>();
        _rateText.fontSize  = 12;
        _rateText.alignment = TextAlignmentOptions.Left;
        _rateText.color     = new Color(0.5f, 1f, 0.72f);
        _rateText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        UpdateRate();
    }

    void UpdateRate()
    {
        if (_rateText == null || _building == null) return;
        BuildingData d = _building.data;
        if (d.passiveRatePerMinute > 0f)
        {
            float mult = (d.rateMultipliers != null && d.rateMultipliers.Length >= _building.level)
                ? d.rateMultipliers[_building.level - 1] : 1f;
            float rate = d.passiveRatePerMinute * mult;
            _rateText.text = $"+{rate:0}/m  {d.passiveResourceType}";
        }
        else
        {
            _rateText.text = d.droneData != null ? "Drone active" : "";
        }
    }

    /// <summary>Show this label (called by BuildingPopup when the building is clicked).</summary>
    public void Show()
    {
        _isVisible = true;
    }

    /// <summary>Hide this label (called by BuildingPopup on deselect / click-away).</summary>
    public void Hide()
    {
        _isVisible = false;
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;

        // Always face the camera
        if (_labelRoot != null)
            _labelRoot.rotation = _cam.transform.rotation;

        // Smooth fade: visible → full opacity (boosted at night), hidden → 0
        if (_labelCG != null)
        {
            float night = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightAmount : 0f;
            float targetAlpha = _isVisible ? Mathf.Lerp(0.88f, 1f, night) : 0f;
            _labelCG.alpha = Mathf.Lerp(_labelCG.alpha, targetAlpha, Time.deltaTime * 12f);
        }
    }
}
