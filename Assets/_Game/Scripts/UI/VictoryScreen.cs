using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Full-screen victory overlay shown when all milestones are complete.
/// Fades in an overlay, then springs in a stat card with session data.
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    float _startTime;

    void Start()
    {
        _startTime = Time.realtimeSinceStartup;
        MilestoneManager.Instance.OnGameWon += Show;
    }

    void OnDestroy()
    {
        if (MilestoneManager.Instance != null)
            MilestoneManager.Instance.OnGameWon -= Show;
    }

    void Show() => StartCoroutine(ShowSequence());

    IEnumerator ShowSequence()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        // ── Dark overlay ──────────────────────────────────────────────────
        var overlay = new GameObject("VictoryOverlay");
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRT = overlay.AddComponent<RectTransform>();
        UIUtils.Fill(overlayRT);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0.04f, 0f);

        // Fade overlay in
        float e = 0f;
        while (e < 0.55f)
        {
            e += Time.unscaledDeltaTime;
            overlayImg.color = new Color(0f, 0f, 0.04f, Mathf.Lerp(0f, 0.90f, e / 0.55f));
            yield return null;
        }

        // ── Glow border (behind card) ─────────────────────────────────────
        var border    = new GameObject("Border");
        border.transform.SetParent(overlay.transform, false);
        var borderRT  = border.AddComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.5f, 0.5f); borderRT.anchorMax = new Vector2(0.5f, 0.5f);
        borderRT.pivot     = new Vector2(0.5f, 0.5f);
        borderRT.sizeDelta = new Vector2(510f, 376f);
        UIUtils.Rounded(border, new Color(0f, 0.85f, 1f, 0.40f), 18);

        // ── Card ──────────────────────────────────────────────────────────
        var card   = new GameObject("VictoryCard");
        card.transform.SetParent(overlay.transform, false);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f); cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(500f, 366f);
        UIUtils.Rounded(card, UIUtils.PanelBg, 16);

        var cardCG   = card.AddComponent<CanvasGroup>();
        var borderCG = border.AddComponent<CanvasGroup>();
        cardCG.alpha = borderCG.alpha = 0f;
        card.transform.localScale = border.transform.localScale = Vector3.one * 0.88f;

        // Spring card in
        e = 0f;
        while (e < 0.38f)
        {
            e += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / 0.38f), 3f);
            float s = Mathf.Lerp(0.88f, 1f, t);
            card.transform.localScale  = Vector3.one * s;
            border.transform.localScale = Vector3.one * s;
            cardCG.alpha = borderCG.alpha = t;
            yield return null;
        }
        card.transform.localScale = border.transform.localScale = Vector3.one;
        cardCG.alpha = borderCG.alpha = 1f;

        // ── Header ────────────────────────────────────────────────────────
        var hdr   = new GameObject("Header");
        hdr.transform.SetParent(card.transform, false);
        var hdrRT = hdr.AddComponent<RectTransform>();
        UIUtils.PinTop(hdrRT, 0f, 74f);
        UIUtils.Rounded(hdr, UIUtils.PanelHdrBg, 16);

        // Top glow line
        var hg    = new GameObject("HdrGlow"); hg.transform.SetParent(hdr.transform, false);
        var hgImg = hg.AddComponent<Image>();
        hgImg.color = new Color(0f, 0.85f, 1f, 0.55f);
        hgImg.rectTransform.anchorMin        = new Vector2(0f, 1f);
        hgImg.rectTransform.anchorMax        = new Vector2(1f, 1f);
        hgImg.rectTransform.pivot            = new Vector2(0.5f, 1f);
        hgImg.rectTransform.anchoredPosition = Vector2.zero;
        hgImg.rectTransform.sizeDelta        = new Vector2(0f, 1f);

        var titleLbl = UIUtils.Label(hdr.transform, "Title",
            "NEON CITY  ESTABLISHED", 21f,
            new Color(0f, 1f, 0.88f), FontStyles.Bold, TextAlignmentOptions.Center);
        var tRT = titleLbl.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.45f); tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12f, 0f);   tRT.offsetMax = new Vector2(-12f, -8f);

        var subLbl = UIUtils.Label(hdr.transform, "Sub",
            "ALL SECTORS OPERATIONAL", 9f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Center);
        var sRT = subLbl.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);   sRT.anchorMax = new Vector2(1f, 0.48f);
        sRT.offsetMin = new Vector2(12f, 5f);  sRT.offsetMax = new Vector2(-12f, 0f);

        // Divider under header
        var div    = new GameObject("Div"); div.transform.SetParent(card.transform, false);
        var divImg = div.AddComponent<Image>();
        divImg.color = UIUtils.Border;
        UIUtils.PinTop(divImg.rectTransform, 74f, 1f);

        // ── Stat cards ────────────────────────────────────────────────────
        float elapsed2 = Time.realtimeSinceStartup - _startTime;
        int   minutes  = Mathf.FloorToInt(elapsed2 / 60f);
        int   seconds  = Mathf.FloorToInt(elapsed2 % 60f);
        float pop      = ResourceManager.Instance.Get(ResourceType.Population);
        int   bldgs    = Building.Count;

        (string lbl, string val, Color col)[] stats =
        {
            ("SESSION TIME", $"{minutes:00}:{seconds:00}", UIUtils.Cyan),
            ("POPULATION",   $"{Mathf.FloorToInt(pop)}",  UIUtils.Green),
            ("BUILDINGS",    $"{bldgs}",                   UIUtils.Amber),
        };

        const float statW  = 138f;
        const float statH  = 68f;
        const float totalW = 3 * statW + 2 * 10f;
        float       startX = -totalW * 0.5f + statW * 0.5f;

        for (int i = 0; i < stats.Length; i++)
        {
            var (slbl, sval, scol) = stats[i];

            var sc   = new GameObject("Stat" + i); sc.transform.SetParent(card.transform, false);
            var scRT = sc.AddComponent<RectTransform>();
            scRT.anchorMin        = new Vector2(0.5f, 0.5f);
            scRT.anchorMax        = new Vector2(0.5f, 0.5f);
            scRT.pivot            = new Vector2(0.5f, 0.5f);
            scRT.anchoredPosition = new Vector2(startX + i * (statW + 10f), 58f);
            scRT.sizeDelta        = new Vector2(statW, statH);
            UIUtils.Rounded(sc, UIUtils.CardBg, 8);

            // Color stripe at top
            var cs    = new GameObject("Stripe"); cs.transform.SetParent(sc.transform, false);
            var csImg = cs.AddComponent<Image>();
            csImg.color = new Color(scol.r, scol.g, scol.b, 0.8f);
            csImg.rectTransform.anchorMin        = new Vector2(0.08f, 1f);
            csImg.rectTransform.anchorMax        = new Vector2(0.92f, 1f);
            csImg.rectTransform.pivot            = new Vector2(0.5f, 1f);
            csImg.rectTransform.anchoredPosition = Vector2.zero;
            csImg.rectTransform.sizeDelta        = new Vector2(0f, 2f);

            // Value
            var valLbl = UIUtils.Label(sc.transform, "Val", sval, 22f,
                scol, FontStyles.Bold, TextAlignmentOptions.Center);
            var vRT = valLbl.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(0f, 0.42f); vRT.anchorMax = Vector2.one;
            vRT.offsetMin = new Vector2(6f, 0f);    vRT.offsetMax = new Vector2(-6f, -6f);

            // Label
            var lblLbl = UIUtils.Label(sc.transform, "Lbl", slbl, 7f,
                UIUtils.TextSub, FontStyles.Bold, TextAlignmentOptions.Center);
            var lRT = lblLbl.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 0f);   lRT.anchorMax = new Vector2(1f, 0.45f);
            lRT.offsetMin = new Vector2(6f, 4f);   lRT.offsetMax = new Vector2(-6f, 0f);
        }

        // ── Flavour text ──────────────────────────────────────────────────
        var flavour = UIUtils.Label(card.transform, "Flavour",
            "The city pulses with life.\nA new dawn breaks over the neon horizon.",
            11f, UIUtils.TextSub, FontStyles.Italic, TextAlignmentOptions.Center);
        flavour.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var fRT = flavour.GetComponent<RectTransform>();
        fRT.anchorMin        = new Vector2(0.5f, 0.5f);
        fRT.anchorMax        = new Vector2(0.5f, 0.5f);
        fRT.pivot            = new Vector2(0.5f, 0.5f);
        fRT.anchoredPosition = new Vector2(0f, -60f);
        fRT.sizeDelta        = new Vector2(420f, 44f);

        // ── New Game button ───────────────────────────────────────────────
        var btnGO = new GameObject("NewGameBtn");
        btnGO.transform.SetParent(card.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0f);
        btnRT.anchorMax        = new Vector2(0.5f, 0f);
        btnRT.pivot            = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 24f);
        btnRT.sizeDelta        = new Vector2(200f, 44f);
        UIUtils.Rounded(btnGO, new Color(0.04f, 0.30f, 0.58f), 10);

        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => SaveManager.Instance.NewGame());

        var bG  = btnGO.GetComponent<Graphic>();
        var bET = btnGO.AddComponent<EventTrigger>();
        var be  = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        be.callback.AddListener(_ => bG.color = new Color(0.06f, 0.44f, 0.82f));
        bET.triggers.Add(be);
        var bx  = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        bx.callback.AddListener(_ => bG.color = new Color(0.04f, 0.30f, 0.58f));
        bET.triggers.Add(bx);

        var btnLbl = UIUtils.Label(btnGO.transform, "Lbl", "PLAY AGAIN", 13f,
            Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        var blRT = btnLbl.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one; blRT.sizeDelta = Vector2.zero;
    }
}
