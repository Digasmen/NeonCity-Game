using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-right HUD panel showing all active drones with battery bars and state.
/// Matches the reference UI style: dark navy, rounded cards, neon accents.
/// Auto-instantiates itself at scene load — no scene wiring required.
/// </summary>
public class DroneHUD : MonoBehaviour
{
    // ── Layout constants ──────────────────────────────────────────────────

    const float PanelW  = 330f;
    const int   Cols    = 3;
    const float CardW   = 95f;
    const float CardH   = 100f;   // portrait(44) + battery(53) + stripe(3)
    const float CardGap = 8f;
    const float PadH    = 12f;
    const float PadV    = 10f;
    const float HdrH    = 40f;

    // ── Per-card live references ───────────────────────────────────────────

    class CardData
    {
        public Drone           drone;
        public Graphic         fill;
        public TextMeshProUGUI pct;
        public TextMeshProUGUI stateLbl;
    }

    // ── Internal state ────────────────────────────────────────────────────

    RectTransform        _panelRT;
    GameObject           _cardsRoot;
    TextMeshProUGUI      _countLbl;
    readonly List<CardData> _cards = new();
    int _lastCount = -1;

    // ── Auto-instantiate ──────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<DroneHUD>() == null)
            new GameObject("_DroneHUD").AddComponent<DroneHUD>();
    }

    // ── Bootstrap ─────────────────────────────────────────────────────────

    void Start()
    {
        // Target the screen-space overlay canvas
        Canvas target = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { target = c; break; }
        if (target == null) return;

        BuildPanel(target.transform);
    }

    void BuildPanel(Transform root)
    {
        // ── Outer panel ───────────────────────────────────────────────────

        var panel = new GameObject("DroneHUD_Panel");
        panel.transform.SetParent(root, false);
        UIUtils.Rounded(panel, UIUtils.PanelBg, 12);

        _panelRT              = panel.GetComponent<RectTransform>();
        _panelRT.anchorMin    = new Vector2(1f, 0f);
        _panelRT.anchorMax    = new Vector2(1f, 0f);
        _panelRT.pivot        = new Vector2(1f, 0f);
        _panelRT.anchoredPosition = new Vector2(-10f, 175f);
        _panelRT.sizeDelta    = new Vector2(PanelW, HdrH + PadV * 2f);   // grows per row

        // ── Header ────────────────────────────────────────────────────────

        var hdr = new GameObject("Header");
        hdr.transform.SetParent(panel.transform, false);
        UIUtils.Rounded(hdr, UIUtils.PanelHdrBg, 12);
        UIUtils.PinTop(hdr.GetComponent<RectTransform>(), 0f, HdrH);

        // "◈  DRONES" — left
        var titleLbl = UIUtils.Label(hdr.transform, "Title", "◈  DRONES", 12f,
            UIUtils.Cyan, FontStyles.Bold, TextAlignmentOptions.Left);
        var tRT = titleLbl.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);  tRT.anchorMax = new Vector2(0.6f, 1f);
        tRT.offsetMin = new Vector2(14f, 0f); tRT.offsetMax = Vector2.zero;

        // "0 / 6" — right
        _countLbl = UIUtils.Label(hdr.transform, "Count", "0 / 0", 11f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Right);
        var cRT = _countLbl.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.6f, 0f); cRT.anchorMax = Vector2.one;
        cRT.offsetMin = Vector2.zero;          cRT.offsetMax = new Vector2(-14f, 0f);

        // ── Divider ───────────────────────────────────────────────────────

        var div = new GameObject("Divider");
        div.transform.SetParent(panel.transform, false);
        div.AddComponent<Image>().color = UIUtils.Border;
        UIUtils.PinTop(div.GetComponent<RectTransform>(), HdrH, 1f);

        // ── Cards root ────────────────────────────────────────────────────

        _cardsRoot = new GameObject("Cards");
        _cardsRoot.transform.SetParent(panel.transform, false);
        var crRT = _cardsRoot.AddComponent<RectTransform>();
        crRT.anchorMin        = new Vector2(0f, 1f);
        crRT.anchorMax        = new Vector2(1f, 1f);
        crRT.pivot            = new Vector2(0.5f, 1f);
        crRT.anchoredPosition = new Vector2(0f, -(HdrH + 1f));
        crRT.sizeDelta        = new Vector2(0f, 0f);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (_panelRT == null) return;

        var drones = Drone.All;

        if (_countLbl != null)
            _countLbl.text = $"{drones.Count} / {Mathf.Max(drones.Count, 6)}";

        if (drones.Count != _lastCount)
        {
            Rebuild(drones);
            _lastCount = drones.Count;
        }

        // Live updates
        for (int i = 0; i < _cards.Count; i++)
        {
            var cd = _cards[i];
            if (cd.drone == null) continue;

            float t = cd.drone.battery / 100f;

            cd.fill.rectTransform.anchorMax = new Vector2(t, 1f);
            cd.fill.color   = UIUtils.BatteryColor(t);
            cd.pct.text     = $"{cd.drone.battery:0}%";
            cd.stateLbl.text  = StateShort(cd.drone.currentState);
            cd.stateLbl.color = StateColor(cd.drone.currentState);
        }
    }

    // ── Card grid ─────────────────────────────────────────────────────────

    void Rebuild(IReadOnlyList<Drone> drones)
    {
        foreach (Transform child in _cardsRoot.transform)
            Destroy(child.gameObject);
        _cards.Clear();

        int rows = drones.Count == 0 ? 0 : (drones.Count + Cols - 1) / Cols;

        // Resize panel height to fit cards
        float gridH = rows > 0
            ? PadV + rows * CardH + (rows - 1) * CardGap + PadV
            : PadV;
        _panelRT.sizeDelta = new Vector2(PanelW, HdrH + 1f + gridH);

        for (int i = 0; i < drones.Count; i++)
        {
            int col = i % Cols;
            int row = i / Cols;
            float x = PadH + col * (CardW + CardGap);
            float y = PadV + row * (CardH + CardGap);
            _cards.Add(MakeCard(drones[i], x, y));
        }
    }

    // ── Card builder ───────────────────────────────────────────────────────

    CardData MakeCard(Drone drone, float x, float y)
    {
        Color droneCol = drone.data != null ? drone.data.droneColor : UIUtils.Cyan;
        Color bandBg   = new Color(droneCol.r * 0.17f, droneCol.g * 0.17f, droneCol.b * 0.17f, 1f);
        Drone capturedDrone = drone;

        // ── Card container ────────────────────────────────────────────────

        var card = new GameObject("Card");
        card.transform.SetParent(_cardsRoot.transform, false);
        var cardBg = UIUtils.Rounded(card, UIUtils.CardBg, 10);
        UIUtils.PlaceAt(card.GetComponent<RectTransform>(), x, y, CardW, CardH);

        // Click: snap camera to this drone
        var btn = card.AddComponent<UnityEngine.UI.Button>();
        btn.transition = UnityEngine.UI.Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            if (capturedDrone != null)
                CameraController.Instance?.FocusOn(capturedDrone.transform.position);
        });
        var et = card.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => cardBg.color = new Color(0.10f, 0.16f, 0.30f, 1f));
        et.triggers.Add(enter);
        var exit = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => cardBg.color = UIUtils.CardBg);
        et.triggers.Add(exit);

        // ── Top accent stripe (3 px, drone colour) ────────────────────────

        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(card.transform, false);
        stripe.AddComponent<Image>().color = droneCol;
        UIUtils.PinTop(stripe.GetComponent<RectTransform>(), 0f, 3f);

        // ── Portrait zone (44 px, coloured background) ────────────────────

        var portrait = new GameObject("Portrait");
        portrait.transform.SetParent(card.transform, false);
        UIUtils.Rounded(portrait, bandBg, 10);
        UIUtils.PinTop(portrait.GetComponent<RectTransform>(), 3f, 44f);

        // Resource type label  e.g. "NANO"
        string resStr = drone.data != null ? drone.data.resourceType.ToString().ToUpper() : "—";
        var resLbl = UIUtils.Label(portrait.transform, "Res", resStr, 15f,
            droneCol, FontStyles.Bold, TextAlignmentOptions.Center);
        UIUtils.PinTop(resLbl.GetComponent<RectTransform>(), 5f, 20f);

        // Drone name  e.g. "SCOUT"
        string nameStr = drone.data != null
            ? drone.data.droneName.ToUpper().Replace(" DRONE", "").Replace("DRONE", "")
            : "DRONE";
        var nameLbl = UIUtils.Label(portrait.transform, "Name", nameStr.Trim(), 8.5f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Center);
        nameLbl.overflowMode = TextOverflowModes.Truncate;
        UIUtils.PinTop(nameLbl.GetComponent<RectTransform>(), 27f, 12f, 4f, 4f);

        // ── Battery zone (53 px, bottom of card) ──────────────────────────

        var batZone = new GameObject("BatZone");
        batZone.transform.SetParent(card.transform, false);
        var batRT = batZone.AddComponent<RectTransform>();   // must be explicit — no Image triggers auto-add here
        UIUtils.PinBottom(batRT, 0f, 53f);

        // Large percentage
        float t0 = drone.battery / 100f;
        var pctLbl = UIUtils.Label(batZone.transform, "Pct", $"{drone.battery:0}%", 20f,
            UIUtils.TextMain, FontStyles.Bold, TextAlignmentOptions.Center);
        UIUtils.PinTop(pctLbl.GetComponent<RectTransform>(), 4f, 22f);

        // Bar track
        var barTrack = new GameObject("Track");
        barTrack.transform.SetParent(batZone.transform, false);
        UIUtils.Rounded(barTrack, new Color(0.03f, 0.05f, 0.12f, 1f), 4);
        UIUtils.PinTop(barTrack.GetComponent<RectTransform>(), 28f, 7f, 8f, 8f);

        // Bar fill  (anchors driven each frame)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barTrack.transform, false);
        UIUtils.Rounded(fillGO, UIUtils.BatteryColor(t0), 4);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(t0, 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // State label
        var stateLbl = UIUtils.Label(batZone.transform, "State", StateShort(drone.currentState), 8f,
            StateColor(drone.currentState), FontStyles.Normal, TextAlignmentOptions.Center);
        UIUtils.PinTop(stateLbl.GetComponent<RectTransform>(), 37f, 11f);

        return new CardData
        {
            drone    = drone,
            fill     = fillGO.GetComponent<RoundedRect>(),
            pct      = pctLbl,
            stateLbl = stateLbl
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static string StateShort(Drone.State s) => s switch
    {
        Drone.State.MovingToTarget  => "COLLECT",
        Drone.State.Collecting      => "COLLECT",
        Drone.State.ReturningHome   => "RETURN",
        Drone.State.Depositing      => "DEPOSIT",
        Drone.State.MovingToCharger => "→ CHARGE",
        Drone.State.Charging        => "CHARGING",
        _                           => ""
    };

    static Color StateColor(Drone.State s) => s switch
    {
        Drone.State.Charging        => UIUtils.Green,
        Drone.State.MovingToCharger => UIUtils.Amber,
        Drone.State.Depositing      => UIUtils.Cyan,
        _                           => UIUtils.TextSub
    };
}
