using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingPopup : MonoBehaviour
{
    public static BuildingPopup Instance { get; private set; }

    const float CanvasScale  = 0.007f;
    const float CamRightDist = 2.2f;   // world-units to the camera-right
    const float CamUpDist    = 0.6f;   // world-units upward

    private GameObject     _canvasGO;
    private Image          _accentBar;
    private TextMeshProUGUI nameText, rateText, upgradeBtnText;
    private Graphic        upgradeBtnImage;
    private Building       currentBuilding;
    private Camera         _cam;

    private GameObject     _normalActionRow;
    private GameObject     _removeConfirmRow;

    void Awake() { Instance = this; }

    void Start()
    {
        _cam = Camera.main;
        CreatePopup();
    }

    void CreatePopup()
    {
        // ── World-space canvas ────────────────────────────────────────────
        _canvasGO = new GameObject("BuildingPopupCanvas");
        Canvas c = _canvasGO.AddComponent<Canvas>();
        c.renderMode  = RenderMode.WorldSpace;
        c.worldCamera = _cam;
        c.sortingOrder = 10;
        _canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform cr = _canvasGO.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(225, 185);
        _canvasGO.transform.localScale = Vector3.one * CanvasScale;

        // Background — rounded, matches panel palette
        UIUtils.Rounded(_canvasGO, UIUtils.PanelBg, 14);

        // Top glow line
        AddLine("TopLine", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 1),
            new Color(0f, 0.8f, 1f, 0.6f));

        // Left accent bar (color set per-building in Show)
        var accentGO = new GameObject("AccentBar");
        accentGO.transform.SetParent(_canvasGO.transform, false);
        var ar = accentGO.AddComponent<RectTransform>();
        ar.anchorMin = Vector2.zero; ar.anchorMax = new Vector2(0, 1);
        ar.pivot = new Vector2(0, 0.5f); ar.sizeDelta = new Vector2(3, 0);
        _accentBar = accentGO.AddComponent<Image>();
        _accentBar.color = new Color(0f, 0.8f, 1f, 0.9f);

        // Name + level
        nameText = MakeLabel("", 13, FontStyles.Bold,
            new Vector2(0, 0.82f), new Vector2(1, 1f), Color.white);

        // Rate
        rateText = MakeLabel("", 11, FontStyles.Normal,
            new Vector2(0, 0.64f), new Vector2(1, 0.82f), new Color(0.4f, 1f, 0.6f));

        // Upgrade button
        var upgBtn = MakeButton(new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.62f),
            new Color(0.04f, 0.30f, 0.58f));
        upgradeBtnImage = upgBtn.GetComponent<RoundedRect>();
        upgradeBtnText  = MakeLabel(upgBtn.transform, "", 10, FontStyles.Bold,
            Vector2.zero, Vector2.one, Color.white);
        upgBtn.GetComponent<Button>().onClick.AddListener(OnUpgradeClicked);

        // ── Normal action row: MOVE + REMOVE ────────────────────────────
        _normalActionRow = new GameObject("NormalActionRow");
        _normalActionRow.transform.SetParent(_canvasGO.transform, false);
        {
            var r = _normalActionRow.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.04f, 0.20f); r.anchorMax = new Vector2(0.96f, 0.38f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var hlg = _normalActionRow.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            var moveBtn = MakeButtonInRow(_normalActionRow.transform, new Color(0.06f, 0.36f, 0.10f));
            MakeLabel(moveBtn.transform, "MOVE", 10, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Color(0.5f, 1f, 0.55f));
            moveBtn.GetComponent<Button>().onClick.AddListener(OnMoveClicked);

            var removeBtn = MakeButtonInRow(_normalActionRow.transform, new Color(0.48f, 0.07f, 0.07f));
            MakeLabel(removeBtn.transform, "REMOVE", 10, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Color(1f, 0.5f, 0.5f));
            removeBtn.GetComponent<Button>().onClick.AddListener(ShowRemoveConfirm);
        }

        // ── Remove confirm row: REMOVE? [✓ YES] [✕ CANCEL] ──────────────
        _removeConfirmRow = new GameObject("RemoveConfirmRow");
        _removeConfirmRow.transform.SetParent(_canvasGO.transform, false);
        {
            var r = _removeConfirmRow.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.04f, 0.20f); r.anchorMax = new Vector2(0.96f, 0.38f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var hlg = _removeConfirmRow.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 5; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // "REMOVE?" label
            var lbl = new GameObject("ConfirmLabel");
            lbl.transform.SetParent(_removeConfirmRow.transform, false);
            lbl.AddComponent<RectTransform>();
            var le = lbl.AddComponent<UnityEngine.UI.LayoutElement>();
            le.flexibleWidth = 0.6f;
            var tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text = "REMOVE?"; tmp.fontSize = 9; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.35f, 0.35f);

            var yesBtn = MakeButtonInRow(_removeConfirmRow.transform, new Color(0.55f, 0.10f, 0.10f));
            MakeLabel(yesBtn.transform, "✓ YES", 9, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Color(1f, 0.6f, 0.6f));
            yesBtn.GetComponent<Button>().onClick.AddListener(OnConfirmRemove);

            var cancelBtn = MakeButtonInRow(_removeConfirmRow.transform, new Color(0.10f, 0.22f, 0.35f));
            MakeLabel(cancelBtn.transform, "✕ CANCEL", 9, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Color(0.55f, 0.75f, 1f));
            cancelBtn.GetComponent<Button>().onClick.AddListener(HideRemoveConfirm);
        }
        _removeConfirmRow.SetActive(false);

        // Hint
        MakeLabel("Right-click to cancel move", 7.5f, FontStyles.Italic,
            new Vector2(0, 0f), new Vector2(1, 0.18f), new Color(0.4f, 0.4f, 0.55f));

        // ── Critical: only the 3 buttons should catch clicks ──────────────
        // Everything else on this world-space canvas would eat raycasts and
        // block the screen-space UI behind it (sortingOrder = 10 makes this worse).
        foreach (var g in _canvasGO.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
        // Re-enable raycast only on the actual Button graphics
        foreach (var btn in _canvasGO.GetComponentsInChildren<Button>(true))
        {
            var btnGfx = btn.GetComponent<Graphic>();
            if (btnGfx != null) btnGfx.raycastTarget = true;
        }

        _canvasGO.SetActive(false);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Update()
    {
        if (currentBuilding == null || !_canvasGO.activeSelf) return;
        RefreshUpgradeButton();
    }

    void LateUpdate()
    {
        // Billboard — face the camera exactly like BuildingLabel
        if (_cam == null) _cam = Camera.main;
        if (_canvasGO != null && _canvasGO.activeSelf && _cam != null)
        {
            _canvasGO.transform.rotation = _cam.transform.rotation;

            // Keep world position locked to building each frame (handles moving buildings)
            if (currentBuilding != null)
                _canvasGO.transform.position = PopupWorldPos(currentBuilding);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Show(Building building)
    {
        currentBuilding = building;
        RefreshAll();

        var pb = building.GetComponent<ProceduralBuilding>();
        _accentBar.color = pb != null ? pb.GlowColor : building.data.glowColor;

        _canvasGO.transform.position = PopupWorldPos(building);
        _canvasGO.SetActive(true);
    }

    public void Hide()
    {
        HideRemoveConfirm();
        _canvasGO.SetActive(false);
        currentBuilding = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    Vector3 PopupWorldPos(Building b)
    {
        if (_cam == null) _cam = Camera.main;
        return b.transform.position
             + _cam.transform.right * CamRightDist
             + Vector3.up * CamUpDist;
    }

    void RefreshAll()
    {
        if (currentBuilding == null) return;
        BuildingData d = currentBuilding.data;
        nameText.text = $"{d.buildingName}  <color=#00C8FF>Lv.{currentBuilding.level}</color>";

        if (d.passiveRatePerMinute > 0f)
        {
            float rate = d.passiveRatePerMinute *
                (currentBuilding.level <= d.rateMultipliers.Length
                    ? d.rateMultipliers[currentBuilding.level - 1] : 1f);
            rateText.text = $"+{rate:0}/m  {d.passiveResourceType}";
        }
        else
        {
            rateText.text = d.droneData != null ? "Drone active" : "";
        }

        RefreshUpgradeButton();
    }

    void RefreshUpgradeButton()
    {
        if (currentBuilding == null) return;
        BuildingData d = currentBuilding.data;

        if (currentBuilding.level >= d.maxLevel)
        {
            upgradeBtnText.text   = "MAX LEVEL";
            upgradeBtnImage.color = new Color(0.12f, 0.12f, 0.14f);
            return;
        }

        int scrap = currentBuilding.UpgradeScrapCost();
        int nano  = currentBuilding.UpgradeNanoCost();
        string cost = nano > 0 ? $"{scrap} Scrap + {nano} Nano" : $"{scrap} Scrap";
        upgradeBtnText.text   = $"↑ Lv.{currentBuilding.level + 1}  |  {cost}";
        upgradeBtnImage.color = currentBuilding.CanUpgrade()
            ? new Color(0.04f, 0.30f, 0.58f)
            : new Color(0.30f, 0.08f, 0.08f);
    }

    void OnUpgradeClicked() { if (currentBuilding) { currentBuilding.Upgrade(); RefreshAll(); } }

    void OnMoveClicked()
    {
        if (currentBuilding == null) return;
        Building b = currentBuilding;
        Hide();
        BuildingPlacer.Instance.SelectForMove(b);
    }

    void ShowRemoveConfirm()
    {
        if (_normalActionRow  != null) _normalActionRow.SetActive(false);
        if (_removeConfirmRow != null) _removeConfirmRow.SetActive(true);
    }

    void HideRemoveConfirm()
    {
        if (_removeConfirmRow != null) _removeConfirmRow.SetActive(false);
        if (_normalActionRow  != null) _normalActionRow.SetActive(true);
    }

    void OnConfirmRemove()
    {
        if (currentBuilding == null) return;
        ResourceManager.Instance.Add(ResourceType.Scrap, currentBuilding.data.scrapCost / 2);
        GridManager.Instance.SetOccupied(currentBuilding.gridCell.x, currentBuilding.gridCell.y, false);
        Destroy(currentBuilding.gameObject);
        Hide();
    }

    // ── UI factory helpers ────────────────────────────────────────────────

    void AddLine(string name, Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(_canvasGO.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax; r.pivot = pivot; r.sizeDelta = sizeDelta;
        go.AddComponent<Image>().color = color;
    }

    // Label parented to _canvasGO
    TextMeshProUGUI MakeLabel(string text, float size, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
        => MakeLabel(_canvasGO.transform, text, size, style, anchorMin, anchorMax, color);

    // Label parented to arbitrary transform
    TextMeshProUGUI MakeLabel(Transform parent, string text, float size, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(8, 2); r.offsetMax = new Vector2(-6, -2);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color; tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return tmp;
    }

    GameObject MakeButton(Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject("Btn"); go.transform.SetParent(_canvasGO.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(2f, 2f); r.offsetMax = new Vector2(-2f, -2f);
        UIUtils.Rounded(go, color, 8);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;
        return go;
    }

    /// <summary>Button sized by HorizontalLayoutGroup — no anchor positioning needed.</summary>
    GameObject MakeButtonInRow(Transform parent, Color color)
    {
        var go = new GameObject("Btn"); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        UIUtils.Rounded(go, color, 6);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;
        return go;
    }
}
