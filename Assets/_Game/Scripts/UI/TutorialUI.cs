using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// First-launch tutorial overlay.
/// Shows 3 sequential hint cards with animated arrows pointing to key UI zones.
/// Stores completion in PlayerPrefs — never shown twice.
/// Auto-instantiates.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<TutorialUI>() == null)
            new GameObject("_TutorialUI").AddComponent<TutorialUI>();
    }

    const string PrefKey = "NeonCity_TutorialDone";

    struct Hint
    {
        public string title;
        public string body;
        // Anchor in normalised screen-space [0..1]
        public Vector2 cardAnchor;
        // Direction the arrow points from the card
        public Vector2 arrowDir;
    }

    readonly Hint[] _hints =
    {
        new Hint { title = "BUILD YOUR CITY",
                   body  = "Open the CONSTRUCT panel at the bottom to place buildings. " +
                            "Click a card, then click on the grid to place.",
                   cardAnchor = new Vector2(0.5f, 0.35f),
                   arrowDir   = new Vector2(0f, -1f) },

        new Hint { title = "WATCH YOUR RESOURCES",
                   body  = "The bar at the top shows Scrap, Energy, Population and more. " +
                            "Keep expanding to unlock new buildings.",
                   cardAnchor = new Vector2(0.5f, 0.6f),
                   arrowDir   = new Vector2(0f, 1f) },

        new Hint { title = "NAVIGATE THE MAP",
                   body  = "Right-click and drag to pan the camera. " +
                            "Scroll to zoom.  Press ESC for settings.",
                   cardAnchor = new Vector2(0.5f, 0.5f),
                   arrowDir   = Vector2.zero },
    };

    Canvas     _canvas;
    GameObject _overlay;
    bool       _completed;

    void Start()
    {
        if (PlayerPrefs.GetInt(PrefKey, 0) == 1) { _completed = true; return; }   // already seen

        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvas = c; break; }
        if (_canvas == null) return;

        StartCoroutine(ShowHints());
    }

    /// <summary>Safety net: if the component is disabled/destroyed mid-tutorial
    /// (scene reload, recompile, etc.), make sure the dimming overlay doesn't leak.</summary>
    void OnDisable()
    {
        if (_overlay != null) Destroy(_overlay);
    }

    void Update()
    {
        // Press Escape to skip the tutorial entirely
        if (!_completed && _overlay != null && Input.GetKeyDown(KeyCode.Escape))
            CompleteAndCleanup();
    }

    void CompleteAndCleanup()
    {
        _completed = true;
        StopAllCoroutines();
        if (_overlay != null) Destroy(_overlay);
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
    }

    IEnumerator ShowHints()
    {
        // Build a semi-transparent dimming overlay (its Image blocks gameplay clicks during the tutorial)
        _overlay = new GameObject("TutorialOverlay");
        _overlay.transform.SetParent(_canvas.transform, false);
        var ort = _overlay.AddComponent<RectTransform>();
        UIUtils.Fill(ort);
        _overlay.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.55f);

        for (int i = 0; i < _hints.Length; i++)
        {
            bool dismissed = false;
            yield return ShowHint(_hints[i], i + 1, _hints.Length, () => dismissed = true);
            // Wait until user dismisses or 8 s elapse
            float t = 0f;
            while (!dismissed && t < 8f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        CompleteAndCleanup();
    }

    IEnumerator ShowHint(Hint hint, int index, int total, System.Action onDismiss)
    {
        var card   = new GameObject("HintCard");
        card.transform.SetParent(_overlay.transform, false);
        UIUtils.Rounded(card, new Color(0.04f, 0.06f, 0.16f, 0.97f), 12);

        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin        = hint.cardAnchor;
        cardRT.anchorMax        = hint.cardAnchor;
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(320f, 140f);

        // Border — purely decorative, must not block button clicks
        var bdr = new GameObject("Bdr"); bdr.transform.SetParent(_overlay.transform, false);
        var bdrGfx = UIUtils.Rounded(bdr, new Color(0f, 0.8f, 1f, 0.30f), 12);
        bdrGfx.raycastTarget = false;
        var bdrRT = bdr.GetComponent<RectTransform>();
        bdrRT.anchorMin = hint.cardAnchor; bdrRT.anchorMax = hint.cardAnchor;
        bdrRT.pivot     = new Vector2(0.5f, 0.5f);
        bdrRT.anchoredPosition = Vector2.zero;
        bdrRT.sizeDelta = new Vector2(326f, 146f);
        bdr.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // put border behind card

        // Index label
        var numLbl = UIUtils.Label(card.transform, "Num", $"{index} / {total}", 8f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Right);
        UIUtils.PinTop(numLbl.GetComponent<RectTransform>(), 6f, 14f, 0f, 12f);

        // Title
        var titleLbl = UIUtils.Label(card.transform, "Title", hint.title, 14f,
            UIUtils.Cyan, FontStyles.Bold, TextAlignmentOptions.Center);
        UIUtils.PinTop(titleLbl.GetComponent<RectTransform>(), 8f, 22f, 12f, 12f);

        // Body
        var bodyLbl = UIUtils.Label(card.transform, "Body", hint.body, 9.5f,
            UIUtils.TextMain, FontStyles.Normal, TextAlignmentOptions.Center);
        bodyLbl.textWrappingMode = TMPro.TextWrappingModes.Normal;
        UIUtils.PinTop(bodyLbl.GetComponent<RectTransform>(), 34f, 62f, 14f, 14f);

        // Dismiss button
        var btnGO = new GameObject("DismissBtn");
        btnGO.transform.SetParent(card.transform, false);
        UIUtils.Rounded(btnGO, new Color(0.05f, 0.20f, 0.45f), 8);
        UIUtils.PinBottom(btnGO.GetComponent<RectTransform>(), 10f, 28f, 80f, 80f);
        var btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        bool tapped = false;
        btn.onClick.AddListener(() => { tapped = true; onDismiss?.Invoke(); });
        var btnLbl = UIUtils.Label(btnGO.transform, "Lbl", index < total ? "NEXT  ›" : "GOT IT!", 9.5f,
            Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        var blRT = btnLbl.GetComponent<RectTransform>();
        blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one; blRT.sizeDelta = Vector2.zero;

        // Arrow (if direction is non-zero)
        if (hint.arrowDir.sqrMagnitude > 0.01f)
        {
            var arrow   = new GameObject("Arrow");
            arrow.transform.SetParent(_overlay.transform, false);
            var arrowLbl = UIUtils.Label(arrow.transform, "A",
                hint.arrowDir.y < 0 ? "↓" : "↑",
                28f, UIUtils.Cyan, FontStyles.Bold, TextAlignmentOptions.Center);
            var arRT = arrowLbl.GetComponent<RectTransform>();
            arRT.anchorMin = arRT.anchorMax = hint.cardAnchor;
            arRT.pivot     = new Vector2(0.5f, 0.5f);
            float ofs      = 90f;
            arRT.anchoredPosition = new Vector2(0f, hint.arrowDir.y * ofs);
            arRT.sizeDelta = new Vector2(40f, 40f);

            // Bounce animation
            StartCoroutine(BounceArrow(arRT, hint.arrowDir));

            yield return new WaitUntil(() => tapped);
            Destroy(arrow);
        }
        else
        {
            yield return new WaitUntil(() => tapped);
        }

        // Fade out card
        var cg = card.AddComponent<CanvasGroup>();
        float e = 0f;
        while (e < 0.2f)
        {
            e += Time.unscaledDeltaTime;
            cg.alpha = 1f - e / 0.2f;
            yield return null;
        }
        Destroy(bdr);
        Destroy(card);
    }

    IEnumerator BounceArrow(RectTransform rt, Vector2 dir)
    {
        Vector2 basePos = rt.anchoredPosition;
        while (rt != null)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 3.5f) * 8f;
            rt.anchoredPosition = basePos + dir * bounce;
            yield return null;
        }
    }
}
