using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared UI utilities: 9-sliceable rounded-corner sprites, cyberpunk colour palette,
/// and RectTransform/Text construction helpers.
/// All colours are keyed to the dark-navy + neon-accent reference style.
/// </summary>
public static class UIUtils
{
    // ── Palette ────────────────────────────────────────────────────────────────

    public static readonly Color PanelBg    = new(0.04f, 0.06f, 0.14f, 0.97f);  // deep navy
    public static readonly Color PanelHdrBg = new(0.05f, 0.11f, 0.25f, 1.00f);  // slightly lighter navy
    public static readonly Color CardBg     = new(0.06f, 0.10f, 0.20f, 1.00f);  // card dark
    public static readonly Color Border     = new(0.08f, 0.22f, 0.48f, 0.75f);  // subtle blue border
    public static readonly Color TextMain   = new(0.90f, 0.94f, 1.00f, 1.00f);  // near-white + blue tint
    public static readonly Color TextSub    = new(0.45f, 0.60f, 0.78f, 1.00f);  // muted blue-white
    public static readonly Color Cyan       = new(0.10f, 0.82f, 1.00f, 1.00f);
    public static readonly Color Green      = new(0.00f, 1.00f, 0.53f, 1.00f);
    public static readonly Color Amber      = new(1.00f, 0.70f, 0.10f, 1.00f);
    public static readonly Color Red        = new(1.00f, 0.22f, 0.12f, 1.00f);

    // ── Rounded-sprite cache ──────────────────────────────────────────────────

    static readonly Dictionary<int, Sprite> _cache = new();

    /// <summary>
    /// Returns a white 9-sliceable rounded-rect Sprite.
    /// Tint the resulting <c>Image.color</c> to any colour you want.
    /// The sprite is cached per radius so it is generated only once per radius value.
    /// </summary>
    public static Sprite RoundedSprite(int radius = 10)
    {
        radius = Mathf.Max(radius, 2);   // guard: 0 or 1 would produce an invalid texture
        if (_cache.TryGetValue(radius, out var s)) return s;

        int sz  = radius * 4;          // e.g. r=10 → 40×40 px texture
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var px = new Color32[sz * sz];
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            byte a = (byte)(Mathf.Clamp01(RndAlpha(x + 0.5f, y + 0.5f, sz, sz, radius)) * 255f);
            px[y * sz + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();

        // 9-slice borders = radius in every direction
        s = Sprite.Create(tex, new Rect(0, 0, sz, sz),
            new Vector2(0.5f, 0.5f), 1f, 0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));

        return _cache[radius] = s;
    }

    // 2-pixel anti-aliased signed-distance field for a rounded rectangle
    static float RndAlpha(float px, float py, float w, float h, float r)
    {
        float cx = Mathf.Clamp(px, r, w - r);
        float cy = Mathf.Clamp(py, r, h - r);
        float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        return 1f - Mathf.Clamp01(d - r + 1f);
    }

    // ── Image helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Adds (or reuses) a <see cref="RoundedRect"/> Graphic with the given colour and corner radius.
    /// Returns the component as <see cref="Graphic"/> so callers can drive <c>.color</c> and
    /// <c>.rectTransform</c> without caring about the concrete type.
    /// </summary>
    public static Graphic Rounded(GameObject go, Color color, int radius = 10, bool raycastable = true)
    {
        var rr = go.GetComponent<RoundedRect>() ?? go.AddComponent<RoundedRect>();
        rr.cornerRadius   = radius;
        rr.cornerSegments = 8;
        rr.color          = color;
        rr.raycastTarget  = raycastable;
        return rr;
    }

    /// <summary>Marks a Graphic (and all child Graphics) as purely decorative — no click blocking.
    /// Use on tooltips, borders, glow effects, dim overlays that should let clicks pass through.</summary>
    public static void NoRaycast(GameObject go)
    {
        foreach (var g in go.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
    }

    // ── Text helper ───────────────────────────────────────────────────────────

    public static TextMeshProUGUI Label(
        Transform parent, string name, string text,
        float size, Color color,
        FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    // ── RectTransform helpers ─────────────────────────────────────────────────

    /// <summary>Stretch-fill parent with optional per-side pixel insets.</summary>
    public static void Fill(RectTransform rt, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    /// <summary>
    /// Pin to the TOP of the parent: full width, fixed height.
    /// <paramref name="yOffset"/> = pixels from top (positive = downward).
    /// Optional left/right padding shrinks the element horizontally.
    /// </summary>
    public static void PinTop(RectTransform rt, float yOffset, float height,
                               float padL = 0f, float padR = 0f)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(padL, -yOffset);
        rt.sizeDelta        = new Vector2(-(padL + padR), height);
    }

    /// <summary>
    /// Pin to the BOTTOM of the parent: full width, fixed height.
    /// <paramref name="yOffset"/> = pixels from bottom (positive = upward).
    /// </summary>
    public static void PinBottom(RectTransform rt, float yOffset, float height,
                                  float padL = 0f, float padR = 0f)
    {
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(padL, yOffset);
        rt.sizeDelta        = new Vector2(-(padL + padR), height);
    }

    /// <summary>
    /// Place at an absolute (x, y) position from the parent's top-left corner,
    /// with an explicit width and height.
    /// </summary>
    public static void PlaceAt(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // ── Battery colour  (green → amber → red) ─────────────────────────────────

    /// <param name="t">Normalised battery level 0..1.</param>
    public static Color BatteryColor(float t) =>
        t > 0.55f
            ? Color.Lerp(Amber, Green,   (t - 0.55f) / 0.45f)
            : Color.Lerp(Red,   Amber,    t           / 0.55f);
}
