using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingLabel : MonoBehaviour
{
    Building _building;
    Camera _cam;
    Transform _labelRoot;
    TextMeshProUGUI _nameText;
    TextMeshProUGUI _levelText;
    TextMeshProUGUI _rateText;
    Image _dot;

    const float LabelHeight  = 2.3f;
    const float CanvasScale  = 0.010f;

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
        cr.sizeDelta = new Vector2(200, 58);

        // Background
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.01f, 0.02f, 0.10f, 0.96f);

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
        _nameText.fontSize  = 14;
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
        _levelText.fontSize  = 11;
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
        _rateText.fontSize  = 10;
        _rateText.alignment = TextAlignmentOptions.Left;
        _rateText.color     = new Color(0.35f, 1f, 0.60f);
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

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_labelRoot != null)
            _labelRoot.rotation = _cam.transform.rotation;
    }
}
