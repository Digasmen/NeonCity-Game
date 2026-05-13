using UnityEngine;

/// <summary>
/// Central design tokens.  EVERY UI file should pull colors / spacing /
/// radius / font sizes from here, never inline magic numbers.
/// One change here = changes everywhere.
///
/// Direction: <b>Neon-Cyberpunk</b> (Option B) — dark near-black panels with
/// subtle 1px borders, accent colors restricted to icons and key data points.
/// Glow only on actively important elements.
/// </summary>
public static class UITheme
{
    // ── Palette ────────────────────────────────────────────────────────────
    // Backgrounds — darker = deeper layer
    public static readonly Color Bg900      = new(0.025f, 0.030f, 0.050f, 1.00f);  // deepest (world bg)
    public static readonly Color Bg800      = new(0.040f, 0.050f, 0.075f, 0.95f);  // panel default
    public static readonly Color Bg700      = new(0.055f, 0.070f, 0.105f, 1.00f);  // panel header
    public static readonly Color Bg600      = new(0.075f, 0.095f, 0.135f, 1.00f);  // card / chip
    public static readonly Color Bg500      = new(0.105f, 0.130f, 0.180f, 1.00f);  // card hover
    public static readonly Color Bg400      = new(0.140f, 0.170f, 0.225f, 1.00f);  // input / pressed

    // Borders — subtle 1px definition
    public static readonly Color Border     = new(1f, 1f, 1f, 0.05f);
    public static readonly Color BorderHi   = new(0.10f, 0.82f, 1.00f, 0.20f);     // accent border

    // Text
    public static readonly Color TextHi     = new(0.96f, 0.98f, 1.00f, 1.00f);     // values, titles
    public static readonly Color TextMid    = new(0.70f, 0.76f, 0.86f, 1.00f);     // body
    public static readonly Color TextLow    = new(0.45f, 0.52f, 0.65f, 1.00f);     // captions, hints
    public static readonly Color TextMuted  = new(0.32f, 0.38f, 0.48f, 1.00f);     // disabled

    // Accents — used sparingly, only on data points
    public static readonly Color Cyan       = new(0.20f, 0.82f, 1.00f, 1.00f);     // primary accent
    public static readonly Color CyanDim    = new(0.20f, 0.82f, 1.00f, 0.30f);
    public static readonly Color Green      = new(0.30f, 1.00f, 0.55f, 1.00f);     // gain / success
    public static readonly Color Amber      = new(1.00f, 0.75f, 0.15f, 1.00f);     // warning / building
    public static readonly Color Red        = new(1.00f, 0.32f, 0.32f, 1.00f);     // loss / danger
    public static readonly Color Magenta    = new(0.92f, 0.35f, 0.92f, 1.00f);     // special / population
    public static readonly Color Violet     = new(0.68f, 0.40f, 1.00f, 1.00f);     // polymer / research

    // Per-resource glyph colors (matches reference style)
    public static Color ResourceColor(ResourceType type) => type switch
    {
        ResourceType.Scrap      => new Color(0.95f, 0.72f, 0.30f),   // amber-orange
        ResourceType.Energy     => new Color(1.00f, 0.82f, 0.10f),   // yellow-gold
        ResourceType.Polymer    => Violet,
        ResourceType.Data       => Cyan,
        ResourceType.Population => Magenta,
        ResourceType.Nano       => Green,
        _                       => TextMid,
    };

    // ── Spacing scale (4px grid) ──────────────────────────────────────────
    public const float S1 = 4f;
    public const float S2 = 8f;
    public const float S3 = 12f;
    public const float S4 = 16f;
    public const float S5 = 24f;
    public const float S6 = 32f;
    public const float S7 = 48f;

    // ── Corner radius scale ───────────────────────────────────────────────
    public const int Rsm = 6;     // chips, small buttons
    public const int Rmd = 10;    // cards, panels
    public const int Rlg = 14;    // big panels (pause overlay, victory)
    public const int Rxl = 18;    // hero panels

    // ── Font scale ────────────────────────────────────────────────────────
    public const float FCaption = 8.5f;   // ALL CAPS captions, resource names
    public const float FBody    = 10.5f;  // standard body
    public const float FBodyB   = 11.5f;  // emphasized body
    public const float FHeader  = 13f;    // panel headers
    public const float FTitle   = 17f;    // overlay titles
    public const float FDisplay = 22f;    // hero numbers, big values

    // ── Stroke widths ─────────────────────────────────────────────────────
    public const float StrokeThin  = 1f;
    public const float StrokeAccent = 1.5f;

    // ── Animation timings ─────────────────────────────────────────────────
    public const float TQuick = 0.12f;   // hover, tap feedback
    public const float TStd   = 0.22f;   // panel transitions
    public const float TSlow  = 0.40f;   // entrance animations
}
