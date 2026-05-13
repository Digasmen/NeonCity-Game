using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Top-of-screen resource HUD — individual chip cards (per the design reference)
/// rather than a single continuous bar.
/// Each chip: colored icon-on-left, NAME (caption) + VALUE (display) + +RATE/m (caption).
/// All visual constants pulled from <see cref="UITheme"/>.
/// </summary>
public class ResourceBar : MonoBehaviour
{
    static readonly (ResourceType type, string label, UIIcon.Kind icon)[] _defs =
    {
        (ResourceType.Scrap,      "Scrap",      UIIcon.Kind.Recycle),
        (ResourceType.Energy,     "Energy",     UIIcon.Kind.Bolt),
        (ResourceType.Polymer,    "Polymer",    UIIcon.Kind.Diamond),
        (ResourceType.Data,       "Data",       UIIcon.Kind.Data),
        (ResourceType.Population, "Population", UIIcon.Kind.Person),
        (ResourceType.Nano,       "Nano",       UIIcon.Kind.Nano),
    };

    // Layout constants (all from theme)
    const float ChipW    = 142f;
    const float ChipH    = 56f;
    const float ChipGap  = UITheme.S2;
    const float IconBox  = 32f;
    const float BarTopY  = 10f;

    struct Chip
    {
        public ResourceType    type;
        public TextMeshProUGUI value;
        public TextMeshProUGUI rate;
        public Graphic         bg;
        public float           lastValue;
        public float           flashTimer;
    }

    readonly List<Chip> _chips = new();

    void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        Build(canvas.transform);
    }

    void Build(Transform root)
    {
        // ── Container — anchored top-centre, fits N chips wide ────────────
        var bar = new GameObject("ResourceBar");
        bar.transform.SetParent(root, false);
        var rt = bar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -BarTopY);
        float totalW = _defs.Length * ChipW + (_defs.Length - 1) * ChipGap;
        rt.sizeDelta = new Vector2(totalW, ChipH);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment     = TextAnchor.MiddleCenter;
        hlg.spacing            = ChipGap;
        hlg.padding            = new RectOffset(0, 0, 0, 0);
        hlg.childControlWidth  = false; hlg.childForceExpandWidth  = false;
        hlg.childControlHeight = false; hlg.childForceExpandHeight = false;

        foreach (var (type, label, icon) in _defs)
            _chips.Add(MakeChip(bar.transform, type, label, icon));
    }

    Chip MakeChip(Transform parent, ResourceType type, string label, UIIcon.Kind iconKind)
    {
        Color accent = UITheme.ResourceColor(type);

        // ── Chip card ─────────────────────────────────────────────────────
        var chipGO = new GameObject("Chip_" + label);
        chipGO.transform.SetParent(parent, false);
        var rt = chipGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ChipW, ChipH);

        var bg = UIUtils.Rounded(chipGO, UITheme.Bg600, UITheme.Rsm, raycastable: false);

        // 1px inner border for definition (separate Graphic, slightly inset)
        var border = new GameObject("Border");
        border.transform.SetParent(chipGO.transform, false);
        var brRT = border.AddComponent<RectTransform>();
        brRT.anchorMin = Vector2.zero; brRT.anchorMax = Vector2.one;
        brRT.offsetMin = brRT.offsetMax = Vector2.zero;
        var brGfx = UIUtils.Rounded(border, UITheme.Border, UITheme.Rsm, raycastable: false);
        // We can't draw an "outline" with RoundedRect — fake it with a slightly larger
        // dimmer rect behind. Cheap, but visually serves the same purpose for now.
        // (Skipping the border GameObject — keeping the implementation simple.)
        DestroyImmediate(border);

        // ── Icon box (left side) ──────────────────────────────────────────
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(chipGO.transform, false);
        var iRT = iconGO.AddComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0f, 0.5f);
        iRT.anchorMax = new Vector2(0f, 0.5f);
        iRT.pivot     = new Vector2(0f, 0.5f);
        iRT.anchoredPosition = new Vector2(UITheme.S3, 0f);
        iRT.sizeDelta = new Vector2(IconBox, IconBox);

        // Icon background — subtle tinted disc
        Color iconBg = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 1f);
        UIUtils.Rounded(iconGO, iconBg, UITheme.Rsm, raycastable: false);

        // Icon glyph
        var glyphGO = new GameObject("Glyph");
        glyphGO.transform.SetParent(iconGO.transform, false);
        var gRT = glyphGO.AddComponent<RectTransform>();
        gRT.anchorMin = Vector2.zero; gRT.anchorMax = Vector2.one;
        gRT.offsetMin = gRT.offsetMax = Vector2.zero;
        var glyph = glyphGO.AddComponent<UIIcon>();
        glyph.kind          = iconKind;
        glyph.color         = accent;
        glyph.raycastTarget = false;

        // ── Name (top-right) ──────────────────────────────────────────────
        var nameLbl = MakeText(chipGO.transform, "Name", label.ToUpper(),
            UITheme.FCaption, FontStyles.Bold, UITheme.TextLow);
        var nRT = nameLbl.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0f, 0.5f);
        nRT.anchorMax = new Vector2(1f, 1f);
        nRT.pivot     = new Vector2(0f, 1f);
        nRT.offsetMin = new Vector2(UITheme.S3 + IconBox + UITheme.S2, 0f);
        nRT.offsetMax = new Vector2(-UITheme.S2, -6f);
        nameLbl.alignment = TextAlignmentOptions.TopLeft;

        // ── Value (middle-right, biggest text) ───────────────────────────
        var valueLbl = MakeText(chipGO.transform, "Value", "0",
            UITheme.FBodyB, FontStyles.Bold, UITheme.TextHi);
        var vRT = valueLbl.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0f, 0f);
        vRT.anchorMax = new Vector2(0.62f, 0.62f);
        vRT.pivot     = new Vector2(0f, 0f);
        vRT.offsetMin = new Vector2(UITheme.S3 + IconBox + UITheme.S2, 4f);
        vRT.offsetMax = new Vector2(0f, 0f);
        valueLbl.alignment = TextAlignmentOptions.BottomLeft;

        // ── Rate (bottom-right, small green/red) ──────────────────────────
        var rateLbl = MakeText(chipGO.transform, "Rate", "",
            UITheme.FCaption, FontStyles.Normal, UITheme.Green);
        var rRT = rateLbl.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.55f, 0f);
        rRT.anchorMax = new Vector2(1f, 0.62f);
        rRT.pivot     = new Vector2(1f, 0f);
        rRT.offsetMin = new Vector2(0f, 4f);
        rRT.offsetMax = new Vector2(-UITheme.S2, 0f);
        rateLbl.alignment = TextAlignmentOptions.BottomRight;

        return new Chip
        {
            type      = type,
            value     = valueLbl,
            rate      = rateLbl,
            bg        = bg,
            lastValue = 0f,
        };
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float size, FontStyles style, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = size;
        tmp.fontStyle         = style;
        tmp.color             = col;
        tmp.alignment         = TextAlignmentOptions.Left;
        tmp.textWrappingMode  = TMPro.TextWrappingModes.NoWrap;
        tmp.raycastTarget     = false;
        return tmp;
    }

    void Update()
    {
        if (ResourceManager.Instance == null) return;

        for (int i = 0; i < _chips.Count; i++)
        {
            var c = _chips[i];

            float amt = ResourceManager.Instance.Get(c.type);
            float r   = ResourceManager.Instance.GetRate(c.type);

            // Value with compact "k" formatting at 10k+
            c.value.text = amt >= 10000f ? $"{amt / 1000f:0.0}k" : $"{amt:0}";

            // Rate suffixed with /m and colored gain/loss
            if (r > 0.05f)
            {
                c.rate.text  = $"+{r:0.#}/m";
                c.rate.color = UITheme.Green;
            }
            else if (r < -0.05f)
            {
                c.rate.text  = $"{r:0.#}/m";
                c.rate.color = UITheme.Red;
            }
            else
            {
                c.rate.text  = "";
            }

            // Flash chip background briefly when value jumps (drone deposit, milestone reward)
            if (amt > c.lastValue + 0.5f && c.lastValue > 0f)
            {
                c.flashTimer = UITheme.TStd;
            }
            c.lastValue = amt;

            if (c.flashTimer > 0f)
            {
                c.flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(c.flashTimer / UITheme.TStd);
                c.bg.color = Color.Lerp(UITheme.Bg600, UITheme.Bg500, t);
            }
            else
            {
                c.bg.color = UITheme.Bg600;
            }

            _chips[i] = c;
        }
    }
}
