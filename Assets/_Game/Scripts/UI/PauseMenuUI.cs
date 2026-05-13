using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Pause / settings overlay.  Press Escape to toggle.
/// Shows volume sliders, game speed reminder, and a "Return to Game" button.
/// Uses unscaled time so it works while paused (timeScale = 0).
/// Auto-instantiates — no scene wiring required.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<PauseMenuUI>() == null)
            new GameObject("_PauseMenuUI").AddComponent<PauseMenuUI>();
    }

    Canvas     _canvas;
    GameObject _overlay;
    bool       _open;
    float      _prevTimeScale = 1f;

    void Start()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvas = c; break; }
        if (_canvas == null) return;

        BuildOverlay();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();

        // ── Panic key (F12) — nukes any UI overlay that's leaked + clears state ──
        if (Input.GetKeyDown(KeyCode.F12))
            PanicCleanup();
    }

    /// <summary>Emergency: destroys any leaked TutorialOverlay / VictoryOverlay /
    /// PauseOverlay / BuildingPopup canvas, and forces timeScale back to 1.
    /// Bind to F12 so the player always has an escape hatch.</summary>
    public static void PanicCleanup()
    {
        string[] leakable =
        {
            "TutorialOverlay", "PauseOverlay", "VictoryOverlay",
            "BuildingPopupCanvas",
        };
        foreach (var name in leakable)
        {
            var go = GameObject.Find(name);
            if (go != null) Destroy(go);
        }
        Time.timeScale = 1f;
        Debug.Log("[PauseMenuUI] PanicCleanup ran — destroyed leaked overlays.");
    }

    // ── Build ──────────────────────────────────────────────────────────────

    void BuildOverlay()
    {
        _overlay = new GameObject("PauseOverlay");
        _overlay.transform.SetParent(_canvas.transform, false);

        // Full-screen dark backdrop — Image catches all raycasts, blocking world interaction
        var overlayRT = _overlay.AddComponent<RectTransform>();
        UIUtils.Fill(overlayRT);
        _overlay.AddComponent<Image>().color = new Color(0f, 0f, 0.04f, 0.82f);

        // ── Card ──────────────────────────────────────────────────────────
        var card   = new GameObject("PauseCard");
        card.transform.SetParent(_overlay.transform, false);
        UIUtils.Rounded(card, UIUtils.PanelBg, 14);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(340f, 300f);

        // Cyan glow border — decorative, must not block clicks
        var bdr = new GameObject("Border"); bdr.transform.SetParent(_overlay.transform, false);
        UIUtils.Rounded(bdr, new Color(0f, 0.8f, 1f, 0.25f), 14, raycastable: false);
        var bdrRT = bdr.GetComponent<RectTransform>();
        bdrRT.anchorMin = new Vector2(0.5f, 0.5f); bdrRT.anchorMax = new Vector2(0.5f, 0.5f);
        bdrRT.pivot     = new Vector2(0.5f, 0.5f);
        bdrRT.anchoredPosition = Vector2.zero;
        bdrRT.sizeDelta = new Vector2(346f, 306f);
        bdr.transform.SetSiblingIndex(0);   // render behind the card

        // ── Header ────────────────────────────────────────────────────────
        var hdr = new GameObject("Header");
        hdr.transform.SetParent(card.transform, false);
        UIUtils.Rounded(hdr, UIUtils.PanelHdrBg, 14);
        UIUtils.PinTop(hdr.GetComponent<RectTransform>(), 0f, 58f);

        var hGlow = new GameObject("HGlow"); hGlow.transform.SetParent(hdr.transform, false);
        var hImg  = hGlow.AddComponent<Image>();
        hImg.color = new Color(0f, 0.85f, 1f, 0.50f);
        hImg.rectTransform.anchorMin = new Vector2(0f, 1f); hImg.rectTransform.anchorMax = new Vector2(1f, 1f);
        hImg.rectTransform.pivot     = new Vector2(0.5f, 1f);
        hImg.rectTransform.anchoredPosition = Vector2.zero;
        hImg.rectTransform.sizeDelta = new Vector2(0f, 1f);

        var titleLbl = UIUtils.Label(hdr.transform, "Title", "PAUSED", 22f,
            new Color(0f, 1f, 0.88f), FontStyles.Bold, TextAlignmentOptions.Center);
        UIUtils.Fill(titleLbl.GetComponent<RectTransform>(), 12f, 0f, 12f, 8f);

        // ── Volume sliders ────────────────────────────────────────────────
        float sliderY = 72f;
        MakeSlider(card.transform, "SFX VOLUME", sliderY, SoundManager.Instance?.sfxVolume ?? 0.6f,
            v => { if (SoundManager.Instance) SoundManager.Instance.sfxVolume = v; });

        MakeSlider(card.transform, "AMBIENT VOLUME", sliderY + 54f, SoundManager.Instance?.ambientVolume ?? 0.15f,
            v => { if (SoundManager.Instance) SoundManager.Instance.ambientVolume = v; });

        MakeSlider(card.transform, "UI SOUNDS", sliderY + 108f, SoundManager.Instance?.uiVolume ?? 0.35f,
            v => { if (SoundManager.Instance) SoundManager.Instance.uiVolume = v; });

        // ── Buttons ───────────────────────────────────────────────────────
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(card.transform, false);
        var brRT = btnRow.AddComponent<RectTransform>();
        UIUtils.PinBottom(brRT, 14f, 40f, 16f, 16f);
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = 12f;
        hlg.childControlWidth  = true;  hlg.childForceExpandWidth  = true;
        hlg.childControlHeight = true;  hlg.childForceExpandHeight = true;

        MakeButton(btnRow.transform, "RESUME", UIUtils.Cyan, Close);
        MakeButton(btnRow.transform, "NEW GAME",
            new Color(0.9f, 0.25f, 0.25f), () => SaveManager.Instance?.NewGame());

        _overlay.SetActive(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void MakeSlider(Transform parent, string label, float yOffset, float initial,
                    System.Action<float> onChange)
    {
        var row = new GameObject("Row_" + label);
        row.transform.SetParent(parent, false);
        var rt = row.AddComponent<RectTransform>();
        UIUtils.PinTop(rt, yOffset, 46f, 16f, 16f);

        // Label
        var lbl = UIUtils.Label(row.transform, "Lbl", label, 8.5f,
            UIUtils.TextSub, FontStyles.Bold, TextAlignmentOptions.Left);
        UIUtils.PinTop(lbl.GetComponent<RectTransform>(), 0f, 16f);

        // Value label (right-aligned)
        var valLbl = UIUtils.Label(row.transform, "Val", $"{initial * 100f:0}%", 8.5f,
            UIUtils.TextMain, FontStyles.Normal, TextAlignmentOptions.Right);
        UIUtils.PinTop(valLbl.GetComponent<RectTransform>(), 0f, 16f);

        // Slider track
        var track = new GameObject("Track");
        track.transform.SetParent(row.transform, false);
        UIUtils.Rounded(track, new Color(0.04f, 0.06f, 0.15f), 4);
        UIUtils.PinBottom(track.GetComponent<RectTransform>(), 0f, 18f);

        var slider = track.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = initial;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.targetGraphic = track.GetComponent<Graphic>();

        // Fill area
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(track.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        UIUtils.Fill(faRT, 0f, 0f, 0f, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        UIUtils.Rounded(fill, UIUtils.Cyan, 4);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(initial, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect   = fillRT;
        slider.handleRect = fillRT;   // simple — no separate handle knob

        slider.onValueChanged.AddListener(v =>
        {
            valLbl.text = $"{v * 100f:0}%";
            onChange?.Invoke(v);
        });
    }

    void MakeButton(Transform parent, string label, Color col, System.Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        UIUtils.Rounded(go, new Color(col.r * 0.15f, col.g * 0.15f, col.b * 0.15f), 8);

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => onClick?.Invoke());

        Graphic bg = go.GetComponent<Graphic>();
        var et = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => bg.color = new Color(col.r*0.28f, col.g*0.28f, col.b*0.28f));
        et.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => bg.color = new Color(col.r*0.15f, col.g*0.15f, col.b*0.15f));
        et.triggers.Add(exit);

        var lbl = UIUtils.Label(go.transform, "Lbl", label, 10.5f,
            col, FontStyles.Bold, TextAlignmentOptions.Center);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one; lRT.sizeDelta = Vector2.zero;
    }

    // ── Toggle ─────────────────────────────────────────────────────────────

    void Toggle()
    {
        if (_open) Close(); else Open();
    }

    void Open()
    {
        if (_overlay == null) return;
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _overlay.SetActive(true);
        _open = true;
    }

    public void Close()
    {
        if (_overlay == null) return;
        Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
        _overlay.SetActive(false);
        _open = false;
    }
}
