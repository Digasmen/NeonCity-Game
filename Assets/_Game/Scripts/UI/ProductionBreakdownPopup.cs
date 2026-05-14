using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space overlay that shows which buildings produce / consume a resource.
/// Toggled by tapping a ResourceBar chip.  Auto-closes when another chip is tapped
/// or the user taps outside the popup.
/// </summary>
public class ProductionBreakdownPopup : MonoBehaviour
{
    public static ProductionBreakdownPopup Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<ProductionBreakdownPopup>() == null)
            new GameObject("_ProductionBreakdownPopup").AddComponent<ProductionBreakdownPopup>();
    }

    // ── Layout constants ──────────────────────────────────────────────────
    const float PopupW   = 268f;
    const float RowH     = 26f;
    const float HeaderH  = 30f;
    const float FooterH  = 30f;
    const float PadV     = 6f;
    const float PadH     = 10f;
    const int   MaxRows  = 9;
    const float GapBelow = 6f;   // pixels gap between chip bottom and popup top

    // ── State ─────────────────────────────────────────────────────────────
    ResourceType _currentType;
    bool         _isOpen  = false;
    GameObject   _popup;
    GameObject   _blocker;
    Canvas       _canvas;

    void Awake() => Instance = this;

    void Start()
    {
        _canvas = FindFirstObjectByType<Canvas>();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Open for this resource type anchored below chipRT, or close if already showing it.</summary>
    public void Toggle(ResourceType type, RectTransform chipRT)
    {
        if (_isOpen && _currentType == type) { Close(); return; }
        Open(type, chipRT);
    }

    public void Close()
    {
        if (_popup   != null) { Destroy(_popup);   _popup   = null; }
        if (_blocker != null) { Destroy(_blocker); _blocker = null; }
        _isOpen = false;
    }

    // ── Build ─────────────────────────────────────────────────────────────

    void Open(ResourceType type, RectTransform chipRT)
    {
        Close();
        if (_canvas == null) return;

        _currentType = type;
        _isOpen      = true;

        var contribs = ResourceManager.Instance.GetContributions(type);
        int rows     = Mathf.Min(contribs.Count, MaxRows);
        float innerH = HeaderH + PadV + rows * RowH + PadV + 1f + PadV + FooterH + PadV;

        // ── Root panel ────────────────────────────────────────────────────
        _popup = new GameObject("BreakdownPopup");
        _popup.transform.SetParent(_canvas.transform, false);

        var rt       = _popup.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(PopupW, innerH);

        PositionBelow(rt, chipRT);
        UIUtils.Rounded(_popup, UIUtils.PanelBg, UITheme.Rmd);

        // Resource-colored glow line at top
        Color accent = UITheme.ResourceColor(type);
        {
            var glowGO  = new GameObject("GlowLine");
            glowGO.transform.SetParent(_popup.transform, false);
            var img     = glowGO.AddComponent<Image>();
            img.color   = new Color(accent.r, accent.g, accent.b, 0.55f);
            img.raycastTarget = false;
            var gRT     = img.rectTransform;
            gRT.anchorMin        = new Vector2(0f, 1f);
            gRT.anchorMax        = new Vector2(1f, 1f);
            gRT.pivot            = new Vector2(0.5f, 1f);
            gRT.anchoredPosition = Vector2.zero;
            gRT.sizeDelta        = new Vector2(0f, 1.5f);
        }

        // ── Header ────────────────────────────────────────────────────────
        {
            var hGO  = new GameObject("Header");
            hGO.transform.SetParent(_popup.transform, false);
            UIUtils.Rounded(hGO, UITheme.Bg700, UITheme.Rmd, raycastable: false);
            UIUtils.PinTop(hGO.AddComponent<RectTransform>(), 0f, HeaderH);

            var lbl  = UIUtils.Label(hGO.transform, "Title",
                $"{type.ToString().ToUpper()}  PRODUCTION",
                UITheme.FCaption, accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIUtils.Fill(lbl.GetComponent<RectTransform>(), PadH, 0f, PadH, 0f);
            lbl.raycastTarget = false;
        }

        // ── Contribution rows ─────────────────────────────────────────────
        float curY    = HeaderH + PadV;
        float netRate = 0f;
        int   shown   = 0;

        for (int i = 0; i < contribs.Count && shown < MaxRows; i++)
        {
            var (b, rate) = contribs[i];
            string name   = (b != null && b.data != null)
                            ? $"{b.data.buildingName}  Lv.{b.level}"
                            : "EVENTS";
            Color  col    = rate >= 0f ? UITheme.Green : UITheme.Red;
            string rStr   = rate >= 0f ? $"+{rate:0.#}/m" : $"{rate:0.#}/m";
            BuildRow(_popup.transform, curY, name, rStr, col);
            curY    += RowH;
            netRate += rate;
            shown++;
        }

        if (contribs.Count > MaxRows)
        {
            // Replace last row with "… and N more"
            curY -= RowH;
            BuildRow(_popup.transform, curY, $"… {contribs.Count - MaxRows + 1} more", "", UITheme.TextLow);
            curY += RowH;
        }

        // ── Divider ───────────────────────────────────────────────────────
        {
            var divGO  = new GameObject("Divider");
            divGO.transform.SetParent(_popup.transform, false);
            var img    = divGO.AddComponent<Image>();
            img.color  = UITheme.Border;
            img.raycastTarget = false;
            UIUtils.PinTop(img.rectTransform, curY, 1f, PadH, PadH);
        }
        curY += 1f + PadV;

        // ── NET total ─────────────────────────────────────────────────────
        {
            var footGO = new GameObject("Footer");
            footGO.transform.SetParent(_popup.transform, false);
            UIUtils.PinTop(footGO.AddComponent<RectTransform>(), curY, FooterH);

            Color  nc   = netRate >= 0f ? UITheme.Green : UITheme.Red;
            string nStr = netRate >= 0f ? $"+{netRate:0.#}/m" : $"{netRate:0.#}/m";

            var netLbl = UIUtils.Label(footGO.transform, "Net",
                "NET", UITheme.FCaption, UITheme.TextLow,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIUtils.Fill(netLbl.GetComponent<RectTransform>(), PadH, 0f, PopupW * 0.5f, 0f);
            netLbl.raycastTarget = false;

            var valLbl = UIUtils.Label(footGO.transform, "NetVal",
                nStr, UITheme.FBodyB, nc,
                FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UIUtils.Fill(valLbl.GetComponent<RectTransform>(), 0f, 0f, PadH, 0f);
            valLbl.raycastTarget = false;
        }

        // ── Click-outside dismissal blocker ───────────────────────────────
        // Full-screen transparent rect behind the popup. Any click not on the popup
        // goes through to the blocker and closes the panel.
        _blocker = new GameObject("BreakdownBlocker");
        _blocker.transform.SetParent(_canvas.transform, false);
        // Insert blocker below popup in the hierarchy so popup renders on top
        _blocker.transform.SetSiblingIndex(_popup.transform.GetSiblingIndex());

        UIUtils.Fill(_blocker.AddComponent<RectTransform>());
        var bImg = _blocker.AddComponent<Image>();
        bImg.color = Color.clear;
        var bBtn = _blocker.AddComponent<Button>();
        bBtn.transition = Selectable.Transition.None;
        bBtn.onClick.AddListener(Close);

        // Popup itself eats raycasts so they don't bubble to the blocker
        var popupBtn = _popup.AddComponent<Button>();
        popupBtn.transition = Selectable.Transition.None;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void BuildRow(Transform parent, float yFromTop, string nameText, string rateText, Color rateColor)
    {
        var rowGO = new GameObject("Row");
        rowGO.transform.SetParent(parent, false);
        UIUtils.PinTop(rowGO.AddComponent<RectTransform>(), yFromTop, RowH, PadH * 0.5f, PadH * 0.5f);

        var nLbl = UIUtils.Label(rowGO.transform, "Name",
            nameText, UITheme.FBody, UITheme.TextMid,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        var nRT = nLbl.GetComponent<RectTransform>();
        nRT.anchorMin = Vector2.zero;         nRT.anchorMax = new Vector2(0.70f, 1f);
        nRT.offsetMin = new Vector2(PadH * 0.5f, 0f);
        nRT.offsetMax = Vector2.zero;
        nLbl.raycastTarget = false;

        if (!string.IsNullOrEmpty(rateText))
        {
            var rLbl = UIUtils.Label(rowGO.transform, "Rate",
                rateText, UITheme.FBody, rateColor,
                FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            var rRT = rLbl.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0.70f, 0f); rRT.anchorMax = Vector2.one;
            rRT.offsetMin = Vector2.zero;
            rRT.offsetMax = new Vector2(-PadH * 0.5f, 0f);
            rLbl.raycastTarget = false;
        }
    }

    void PositionBelow(RectTransform popupRT, RectTransform chipRT)
    {
        var canvasRT = _canvas.GetComponent<RectTransform>();
        bool overlay = _canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        Camera cam   = overlay ? null : _canvas.worldCamera;

        // Chip bottom-center in screen pixels
        Vector3[] corners = new Vector3[4];
        chipRT.GetWorldCorners(corners);   // BL=0, TL=1, TR=2, BR=3
        Vector2 screenBottomCenter;
        if (overlay)
            screenBottomCenter = ((Vector2)corners[0] + (Vector2)corners[3]) * 0.5f;
        else
        {
            Vector3 worldCenter = (corners[0] + corners[3]) * 0.5f;
            screenBottomCenter  = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        }

        Vector2 localPt;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenBottomCenter, cam, out localPt);

        // Anchor pivot at top-center → anchoredPosition = top-center position
        popupRT.anchorMin = new Vector2(0.5f, 0f);
        popupRT.anchorMax = new Vector2(0.5f, 0f);
        popupRT.pivot     = new Vector2(0.5f, 1f);

        // Clamp X so popup never clips screen edge
        float halfW    = PopupW * 0.5f;
        float canHalfW = canvasRT.rect.width * 0.5f;
        float x        = Mathf.Clamp(localPt.x, -canHalfW + halfW + 4f, canHalfW - halfW - 4f);

        popupRT.anchoredPosition = new Vector2(x, localPt.y - GapBelow);
    }
}
