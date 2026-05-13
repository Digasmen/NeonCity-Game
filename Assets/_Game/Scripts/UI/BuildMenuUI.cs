using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Persistent bottom build panel — always visible, collapsible via chevron.
/// Cards scroll horizontally; category tabs filter the visible set.
/// </summary>
public class BuildMenuUI : MonoBehaviour
{
    public static BuildMenuUI Instance { get; private set; }

    [Header("Buildings to show in menu")]
    public List<BuildingData> availableBuildings;

    /// <summary>Fired whenever a previously-locked building becomes available.</summary>
    public static event System.Action<BuildingData> OnBuildingUnlocked;

    // ── Categories ────────────────────────────────────────────────────────

    enum Category { Resource = 0, Housing, Support, Recon }

    static Category GetCategory(string name) => name switch
    {
        "Shelter"          => Category.Housing,
        "Clinic"           => Category.Support,
        "Charging Station" => Category.Support,
        "Scout Hub"        => Category.Recon,
        _                  => Category.Resource,
    };

    static readonly (string label, Color color)[] _tabs =
    {
        ("ALL",      new Color(0.25f, 0.55f, 1.00f)),
        ("RESOURCE", new Color(0.20f, 0.82f, 1.00f)),
        ("HOUSING",  new Color(0.30f, 1.00f, 0.50f)),
        ("SUPPORT",  new Color(0.70f, 0.40f, 1.00f)),
        ("RECON",    new Color(1.00f, 0.55f, 0.10f)),
    };

    static string BuildingIcon(string name) => name switch
    {
        "Scrap Collector"   => "SC",
        "Energy Generator"  => "EG",
        "Polymer Extractor" => "PX",
        "Data Tower"        => "DT",
        "Shelter"           => "SH",
        "Clinic"            => "CL",
        "Charging Station"  => "CS",
        "Scout Hub"         => "RN",
        _                   => "??",
    };

    // ── Layout constants ──────────────────────────────────────────────────

    const float PanelHFull      = 172f;
    const float PanelHCollapsed = 32f;
    const float HeaderH         = 32f;
    const float TabH            = 26f;
    const float CardW           = 98f;
    const float CardH           = 108f;
    const float IconH           = 56f;
    const float CardGap         = 6f;

    // ── Per-card data ─────────────────────────────────────────────────────

    class CardEntry
    {
        public BuildingData    data;
        public Category        category;
        public GameObject      root;
        public Graphic         bg;
        public TextMeshProUGUI costLabel;
        public TextMeshProUGUI popLabel;   // null when no pop requirement
    }

    // ── Internal refs ─────────────────────────────────────────────────────

    RectTransform           _panelRT;
    GameObject              _body;
    Transform               _contentRoot;
    Image[]                 _tabBgs;
    TextMeshProUGUI[]       _tabLabels;
    Image[]                 _tabUnderlines;
    TextMeshProUGUI         _chevron;
    int                     _activeTab = 0;
    bool                    _collapsed = false;

    // ── Tooltip ───────────────────────────────────────────────────────────
    GameObject              _tooltip;
    TextMeshProUGUI         _ttName;
    TextMeshProUGUI         _ttDesc;
    TextMeshProUGUI         _ttProd;
    TextMeshProUGUI         _ttCons;

    readonly HashSet<BuildingData> _cardSet  = new();
    readonly List<CardEntry>       _cardList = new();

    // ─────────────────────────────────────────────────────────────────────

    void Awake() { Instance = this; }
    void Start()  { CreateUI(); }

    // ── Update: affordability tinting ─────────────────────────────────────

    void Update()
    {
        if (_collapsed || ResourceManager.Instance == null) return;
        foreach (var e in _cardList)
        {
            if (!e.root.activeSelf) continue;
            bool canScrap = ResourceManager.Instance.CanAfford(ResourceType.Scrap, e.data.scrapCost);
            bool hasPop   = e.data.populationRequired <= 0 ||
                            ResourceManager.Instance.CanAfford(ResourceType.Population, e.data.populationRequired);
            e.costLabel.color = canScrap
                ? new Color(0.30f, 1.00f, 0.50f)
                : new Color(1.00f, 0.30f, 0.30f);
            if (e.popLabel != null)
                e.popLabel.color = hasPop
                    ? new Color(0.75f, 0.50f, 1.00f)
                    : new Color(1.00f, 0.30f, 0.30f);
        }
    }

    // ── Build ─────────────────────────────────────────────────────────────

    void CreateUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        Transform root = canvas.transform;

        // ── Outer panel ───────────────────────────────────────────────────
        var panel = new GameObject("BuildMenuPanel");
        panel.transform.SetParent(root, false);
        _panelRT = panel.AddComponent<RectTransform>();
        _panelRT.anchorMin        = new Vector2(0f, 0f);
        _panelRT.anchorMax        = new Vector2(1f, 0f);
        _panelRT.pivot            = new Vector2(0.5f, 0f);
        _panelRT.anchoredPosition = Vector2.zero;
        _panelRT.sizeDelta        = new Vector2(0f, PanelHFull);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.09f, 0.97f);

        // Top cyan glow border
        HLine(panel.transform, "TopGlow", new Color(0f, 0.8f, 1f, 0.55f), true, 0f);

        // ── Header ────────────────────────────────────────────────────────
        var hdr = new GameObject("Header");
        hdr.transform.SetParent(panel.transform, false);
        var hdrRT = hdr.AddComponent<RectTransform>();
        UIUtils.PinTop(hdrRT, 0f, HeaderH);
        hdr.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.15f, 1f);

        // "◈  CONSTRUCT" title
        var title = UIUtils.Label(hdr.transform, "Title", "◈  CONSTRUCT", 11f,
            new Color(0.20f, 0.82f, 1.00f), FontStyles.Bold, TextAlignmentOptions.Left);
        var tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f); tRT.anchorMax = new Vector2(0.7f, 1f);
        tRT.offsetMin = new Vector2(16f, 0f); tRT.offsetMax = Vector2.zero;

        // Collapse chevron button
        var chevGO = new GameObject("Chevron");
        chevGO.transform.SetParent(hdr.transform, false);
        var chevRT = chevGO.AddComponent<RectTransform>();
        chevRT.anchorMin        = new Vector2(1f, 0.5f);
        chevRT.anchorMax        = new Vector2(1f, 0.5f);
        chevRT.pivot            = new Vector2(1f, 0.5f);
        chevRT.anchoredPosition = new Vector2(-14f, 0f);
        chevRT.sizeDelta        = new Vector2(32f, 20f);
        UIUtils.Rounded(chevGO, new Color(0.05f, 0.15f, 0.30f, 1f), 6);
        _chevron = UIUtils.Label(chevGO.transform, "Arrow", "∨", 12f,
            new Color(0.45f, 0.70f, 0.90f), FontStyles.Bold, TextAlignmentOptions.Center);
        var chevLblRT = _chevron.GetComponent<RectTransform>();
        chevLblRT.anchorMin = Vector2.zero; chevLblRT.anchorMax = Vector2.one;
        chevLblRT.offsetMin = Vector2.zero; chevLblRT.offsetMax = Vector2.zero;
        var chevBtn = chevGO.AddComponent<Button>();
        chevBtn.transition = Selectable.Transition.None;
        chevBtn.onClick.AddListener(ToggleCollapse);
        var chevGraphic = chevGO.GetComponent<Graphic>();
        var chevET = chevGO.AddComponent<EventTrigger>();
        AddHover(chevET,
            () => chevGraphic.color = new Color(0.08f, 0.25f, 0.48f, 1f),
            () => chevGraphic.color = new Color(0.05f, 0.15f, 0.30f, 1f));

        // ── Body (hidden when collapsed) ──────────────────────────────────
        _body = new GameObject("Body");
        _body.transform.SetParent(panel.transform, false);
        var bodyRT = _body.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f); bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(0f, 0f); bodyRT.offsetMax = new Vector2(0f, -HeaderH);

        // ── Tab strip ─────────────────────────────────────────────────────
        var tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(_body.transform, false);
        var tabBarRT = tabBar.AddComponent<RectTransform>();
        UIUtils.PinTop(tabBarRT, 0f, TabH);
        tabBar.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.12f, 1f);

        var tabHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHLG.childAlignment      = TextAnchor.MiddleLeft;
        tabHLG.spacing             = 0f;
        tabHLG.padding             = new RectOffset(12, 12, 0, 0);
        tabHLG.childControlWidth   = false; tabHLG.childForceExpandWidth  = false;
        tabHLG.childControlHeight  = true;  tabHLG.childForceExpandHeight = true;

        _tabBgs        = new Image[_tabs.Length];
        _tabLabels     = new TextMeshProUGUI[_tabs.Length];
        _tabUnderlines = new Image[_tabs.Length];

        for (int i = 0; i < _tabs.Length; i++)
        {
            int  idx          = i;
            var (lbl, col)    = _tabs[i];
            float tabW        = lbl.Length * 7.8f + 22f;

            var tabGO = new GameObject("Tab_" + lbl);
            tabGO.transform.SetParent(tabBar.transform, false);
            tabGO.AddComponent<RectTransform>().sizeDelta = new Vector2(tabW, 0f);

            _tabBgs[i] = tabGO.AddComponent<Image>();
            _tabBgs[i].color = i == 0
                ? new Color(col.r * 0.20f, col.g * 0.20f, col.b * 0.20f, 1f)
                : Color.clear;

            var tabBtn = tabGO.AddComponent<Button>();
            tabBtn.transition = Selectable.Transition.None;
            tabBtn.onClick.AddListener(() => SetActiveTab(idx));

            // Hover tint
            Image  tbi = _tabBgs[i];
            Color  tc  = col;
            var tabET = tabGO.AddComponent<EventTrigger>();
            AddHover(tabET,
                () => { if (_activeTab != idx) tbi.color = new Color(tc.r*0.10f, tc.g*0.10f, tc.b*0.10f, 1f); },
                () => { if (_activeTab != idx) tbi.color = Color.clear; });

            // Label
            _tabLabels[i] = UIUtils.Label(tabGO.transform, "Lbl", lbl, 8.5f,
                i == 0 ? col : new Color(0.45f, 0.55f, 0.70f),
                FontStyles.Bold, TextAlignmentOptions.Center);
            var lblRT = _tabLabels[i].GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f); lblRT.anchorMax = new Vector2(1f, 1f);
            lblRT.offsetMin = new Vector2(2f, 4f);  lblRT.offsetMax = new Vector2(-2f, -2f);

            // Active underline
            var ul = new GameObject("Underline");
            ul.transform.SetParent(tabGO.transform, false);
            var ulRT = ul.AddComponent<RectTransform>();
            ulRT.anchorMin        = new Vector2(0.08f, 0f);
            ulRT.anchorMax        = new Vector2(0.92f, 0f);
            ulRT.pivot            = new Vector2(0.5f, 0f);
            ulRT.anchoredPosition = Vector2.zero;
            ulRT.sizeDelta        = new Vector2(0f, 2f);
            _tabUnderlines[i] = ul.AddComponent<Image>();
            _tabUnderlines[i].color = i == 0 ? col : Color.clear;
        }

        // Divider under tabs
        HLine(_body.transform, "Div", new Color(0.08f, 0.22f, 0.48f, 0.45f), true, TabH);

        // ── Horizontal scroll area ─────────────────────────────────────────
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_body.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = new Vector2(0f, -(TabH + 1f));
        scrollGO.AddComponent<Image>().color = Color.clear;   // raycasting target

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal        = true;
        sr.vertical          = false;
        sr.scrollSensitivity = 35f;
        sr.movementType      = ScrollRect.MovementType.Elastic;
        sr.elasticity        = 0.05f;
        sr.inertia           = true;
        sr.decelerationRate  = 0.135f;

        // Viewport
        var vp = new GameObject("Viewport");
        vp.transform.SetParent(scrollGO.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<RectMask2D>();   // rect-based clip — no stencil, no TMP conflicts
        sr.viewport = vpRT;

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin        = new Vector2(0f, 0f);
        contentRT.anchorMax        = new Vector2(0f, 1f);
        contentRT.pivot            = new Vector2(0f, 0.5f);
        contentRT.anchoredPosition = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment     = TextAnchor.MiddleLeft;
        hlg.spacing            = CardGap;
        hlg.padding            = new RectOffset(12, 12, 0, 0);
        hlg.childControlWidth  = false; hlg.childForceExpandWidth  = false;
        hlg.childControlHeight = false; hlg.childForceExpandHeight = false;
        sr.content = contentRT;
        _contentRoot = content.transform;

        // ── Tooltip (hidden by default) ───────────────────────────────────
        CreateTooltip(root);

        // ── Seed starter cards ────────────────────────────────────────────
        foreach (var data in availableBuildings)
            if (!data.lockedAtStart)
                CreateBuildingCard(data);

        // ── New Game button (sits above panel, bottom-right) ──────────────
        var ngGO = new GameObject("NewGameBtn");
        ngGO.transform.SetParent(root, false);
        var ngRT = ngGO.AddComponent<RectTransform>();
        ngRT.anchorMin        = new Vector2(1f, 0f);
        ngRT.anchorMax        = new Vector2(1f, 0f);
        ngRT.pivot            = new Vector2(1f, 0f);
        ngRT.anchoredPosition = new Vector2(-10f, PanelHFull + 8f);
        ngRT.sizeDelta        = new Vector2(108f, 28f);
        UIUtils.Rounded(ngGO, new Color(0.20f, 0.04f, 0.04f, 0.92f), 6);
        var ngBtn = ngGO.AddComponent<Button>();
        ngBtn.transition = Selectable.Transition.None;
        var ngG  = ngGO.GetComponent<Graphic>();
        var ngET = ngGO.AddComponent<EventTrigger>();
        AddHover(ngET,
            () => ngG.color = new Color(0.40f, 0.07f, 0.07f, 1f),
            () => ngG.color = new Color(0.20f, 0.04f, 0.04f, 0.92f));
        ngBtn.onClick.AddListener(() => SaveManager.Instance.NewGame());
        var ngLbl = UIUtils.Label(ngGO.transform, "Lbl", "NEW GAME", 9.5f,
            new Color(1f, 0.40f, 0.40f), FontStyles.Bold, TextAlignmentOptions.Center);
        var ngLR = ngLbl.GetComponent<RectTransform>();
        ngLR.anchorMin = Vector2.zero; ngLR.anchorMax = Vector2.one; ngLR.sizeDelta = Vector2.zero;
    }

    // ── Tooltip builder ───────────────────────────────────────────────────

    void CreateTooltip(Transform root)
    {
        _tooltip = new GameObject("BuildTooltip");
        _tooltip.transform.SetParent(root, false);
        var rt = _tooltip.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(190f, 112f);
        UIUtils.Rounded(_tooltip, new Color(0.03f, 0.05f, 0.14f, 0.97f), 10);

        // Top accent line
        var acTop = new GameObject("Accent"); acTop.transform.SetParent(_tooltip.transform, false);
        var acImg = acTop.AddComponent<Image>();
        acImg.color = UIUtils.Cyan;
        acImg.rectTransform.anchorMin        = new Vector2(0f, 1f);
        acImg.rectTransform.anchorMax        = new Vector2(1f, 1f);
        acImg.rectTransform.pivot            = new Vector2(0.5f, 1f);
        acImg.rectTransform.anchoredPosition = Vector2.zero;
        acImg.rectTransform.sizeDelta        = new Vector2(0f, 1.5f);

        _ttName = UIUtils.Label(_tooltip.transform, "Name", "", 11f,
            UIUtils.TextMain, FontStyles.Bold, TextAlignmentOptions.Left);
        UIUtils.PinTop(_ttName.GetComponent<RectTransform>(), 5f, 17f, 10f, 6f);

        _ttDesc = UIUtils.Label(_tooltip.transform, "Desc", "", 8.5f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Left);
        _ttDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        UIUtils.PinTop(_ttDesc.GetComponent<RectTransform>(), 22f, 34f, 10f, 6f);

        _ttProd = UIUtils.Label(_tooltip.transform, "Prod", "", 8.5f,
            UIUtils.Green, FontStyles.Normal, TextAlignmentOptions.Left);
        UIUtils.PinTop(_ttProd.GetComponent<RectTransform>(), 57f, 13f, 10f, 6f);

        _ttCons = UIUtils.Label(_tooltip.transform, "Cons", "", 8.5f,
            UIUtils.Red, FontStyles.Normal, TextAlignmentOptions.Left);
        UIUtils.PinTop(_ttCons.GetComponent<RectTransform>(), 71f, 13f, 10f, 6f);

        var ttAdj = UIUtils.Label(_tooltip.transform, "Adj", "", 8f,
            UIUtils.Amber, FontStyles.Normal, TextAlignmentOptions.Left);
        UIUtils.PinTop(ttAdj.GetComponent<RectTransform>(), 85f, 13f, 10f, 6f);

        // Tooltip must never intercept clicks — it's purely decorative
        foreach (var g in _tooltip.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        _tooltip.SetActive(false);
    }

    void ShowTooltip(BuildingData data, RectTransform cardRT)
    {
        if (_tooltip == null) return;
        _ttName.text = data.buildingName.ToUpper();
        _ttDesc.text = string.IsNullOrEmpty(data.description)
            ? "Contributes to city growth."
            : data.description;

        _ttProd.text = data.passiveRatePerMinute > 0f
            ? $"+{data.passiveRatePerMinute:0.#}/min {data.passiveResourceType}"
            : "";
        _ttCons.text = data.consumptionRatePerMinute > 0f
            ? $"-{data.consumptionRatePerMinute:0.#}/min {data.consumptionType}"
            : "";

        // Find adj label (sibling index 5 after accent, name, desc, prod, cons)
        var adjLbl = _tooltip.transform.GetChild(5).GetComponent<TextMeshProUGUI>();
        if (adjLbl != null)
            adjLbl.text = !string.IsNullOrEmpty(data.adjacencyBuildingType)
                ? $"+{(data.adjacencyBonus - 1f) * 100f:0}% near {data.adjacencyBuildingType}"
                : "";

        // Convert card centre to canvas local space and position tooltip above it
        var tooltipRT = _tooltip.GetComponent<RectTransform>();
        Canvas canvas = _tooltip.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                cardRT.position,       // cardRT.position is already screen-space
                null,
                out Vector2 localPt);
            float clampedX = Mathf.Clamp(localPt.x - tooltipRT.sizeDelta.x * 0.5f,
                                          4f,
                                          canvas.GetComponent<RectTransform>().rect.width - tooltipRT.sizeDelta.x - 4f);
            tooltipRT.anchoredPosition = new Vector2(clampedX, PanelHFull + 8f);
        }

        _tooltip.SetActive(true);
    }

    void HideTooltip() => _tooltip?.SetActive(false);

    // ── Tab switching ──────────────────────────────────────────────────────

    void SetActiveTab(int index)
    {
        _activeTab = index;
        for (int i = 0; i < _tabs.Length; i++)
        {
            var (_, col) = _tabs[i];
            bool on = (i == index);
            _tabBgs[i].color        = on ? new Color(col.r*0.20f, col.g*0.20f, col.b*0.20f, 1f) : Color.clear;
            _tabLabels[i].color     = on ? col : new Color(0.45f, 0.55f, 0.70f);
            _tabUnderlines[i].color = on ? col : Color.clear;
        }
        foreach (var e in _cardList)
            e.root.SetActive(_activeTab == 0 || (int)e.category == _activeTab - 1);
    }

    // ── Collapse ──────────────────────────────────────────────────────────

    void ToggleCollapse()
    {
        _collapsed            = !_collapsed;
        _body.SetActive(!_collapsed);
        _panelRT.sizeDelta    = new Vector2(0f, _collapsed ? PanelHCollapsed : PanelHFull);
        _chevron.text         = _collapsed ? "∧" : "∨";
    }

    // ── Card builder ──────────────────────────────────────────────────────

    void CreateBuildingCard(BuildingData data)
    {
        _cardSet.Add(data);
        Category cat    = GetCategory(data.buildingName);
        Color    col    = _tabs[(int)cat + 1].color;
        Color    iconBg = new Color(col.r * 0.14f, col.g * 0.14f, col.b * 0.14f, 1f);
        Color    iconFg = new Color(col.r * 0.55f + 0.45f, col.g * 0.55f + 0.45f, col.b * 0.55f + 0.45f);

        // Card root
        var card = new GameObject(data.buildingName);
        card.transform.SetParent(_contentRoot, false);
        card.AddComponent<RectTransform>().sizeDelta = new Vector2(CardW, CardH);
        var cardBg = UIUtils.Rounded(card, new Color(0.05f, 0.08f, 0.18f, 1f), 10);

        // Hover highlight + tooltip
        var cardRT2 = card.GetComponent<RectTransform>();
        BuildingData tipData = data;
        var et = card.AddComponent<EventTrigger>();
        AddHover(et,
            () => { cardBg.color = new Color(0.09f, 0.14f, 0.28f, 1f); ShowTooltip(tipData, cardRT2); },
            () => { cardBg.color = new Color(0.05f, 0.08f, 0.18f, 1f); HideTooltip(); });

        // Click → start placement + collapse panel
        var btn = card.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        BuildingData cap = data;
        btn.onClick.AddListener(() =>
        {
            BuildingPlacer.Instance.StartPlacement(cap);
            if (!_collapsed) ToggleCollapse();
        });

        // ── Icon zone ─────────────────────────────────────────────────────
        var iconZone = new GameObject("IconZone");
        iconZone.transform.SetParent(card.transform, false);
        UIUtils.Rounded(iconZone, iconBg, 10);
        UIUtils.PinTop(iconZone.GetComponent<RectTransform>(), 0f, IconH);

        var iconLbl = UIUtils.Label(iconZone.transform, "Icon",
            BuildingIcon(data.buildingName), 26f, iconFg,
            FontStyles.Normal, TextAlignmentOptions.Center);
        var ilRT = iconLbl.GetComponent<RectTransform>();
        ilRT.anchorMin = Vector2.zero; ilRT.anchorMax = Vector2.one;
        ilRT.offsetMin = Vector2.zero; ilRT.offsetMax = Vector2.zero;

        // Category dot — top-right
        var dot = new GameObject("Dot");
        dot.transform.SetParent(iconZone.transform, false);
        UIUtils.Rounded(dot, col, 3);
        var dotRT = dot.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(1f, 1f); dotRT.anchorMax = new Vector2(1f, 1f);
        dotRT.pivot     = new Vector2(1f, 1f);
        dotRT.anchoredPosition = new Vector2(-5f, -5f);
        dotRT.sizeDelta = new Vector2(7f, 7f);

        // Bottom accent line on icon zone
        var accentLine = new GameObject("Accent");
        accentLine.transform.SetParent(iconZone.transform, false);
        var alImg = accentLine.AddComponent<Image>();
        alImg.color = new Color(col.r, col.g, col.b, 0.55f);
        var alRT = alImg.rectTransform;
        alRT.anchorMin = new Vector2(0.08f, 0f); alRT.anchorMax = new Vector2(0.92f, 0f);
        alRT.pivot     = new Vector2(0.5f, 0f);
        alRT.anchoredPosition = Vector2.zero;
        alRT.sizeDelta = new Vector2(0f, 1f);

        // ── Info zone ─────────────────────────────────────────────────────
        float infoTop = IconH + 4f;

        var nameLbl = UIUtils.Label(card.transform, "Name", data.buildingName, 9f,
            UIUtils.TextMain, FontStyles.Bold, TextAlignmentOptions.Center);
        nameLbl.overflowMode = TextOverflowModes.Truncate;
        UIUtils.PinTop(nameLbl.GetComponent<RectTransform>(), infoTop, 15f, 4f, 4f);

        bool hasPop = data.populationRequired > 0;

        var costLbl = UIUtils.Label(card.transform, "Cost",
            $"{data.scrapCost} Scrap", 8.5f,
            new Color(0.30f, 1.00f, 0.50f), FontStyles.Normal, TextAlignmentOptions.Center);
        UIUtils.PinTop(costLbl.GetComponent<RectTransform>(), infoTop + 17f, 13f, 4f, 4f);

        TextMeshProUGUI popLbl = null;
        if (hasPop)
        {
            popLbl = UIUtils.Label(card.transform, "Pop",
                $"{data.populationRequired} Pop", 7.5f,
                new Color(0.75f, 0.50f, 1.00f), FontStyles.Normal, TextAlignmentOptions.Center);
            UIUtils.PinTop(popLbl.GetComponent<RectTransform>(), infoTop + 31f, 12f, 4f, 4f);
        }

        // Show/hide based on active tab
        card.SetActive(_activeTab == 0 || (int)cat == _activeTab - 1);

        _cardList.Add(new CardEntry
        {
            data      = data,
            category  = cat,
            root      = card,
            bg        = cardBg,
            costLabel = costLbl,
            popLabel  = popLbl,
        });
    }

    // ── Public unlock ──────────────────────────────────────────────────────

    public void UnlockBuilding(BuildingData data)
    {
        if (_cardSet.Contains(data)) return;
        CreateBuildingCard(data);
        OnBuildingUnlocked?.Invoke(data);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void AddHover(EventTrigger et, System.Action onEnter, System.Action onExit)
    {
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => onEnter());
        et.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => onExit());
        et.triggers.Add(exit);
    }

    // 1px horizontal rule, pinned to top or bottom of parent with optional offset.
    static void HLine(Transform parent, string name, Color color, bool atTop, float offset = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (atTop)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -offset);
        }
        else
        {
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, offset);
        }
        rt.sizeDelta = new Vector2(0f, 1f);
        go.AddComponent<Image>().color = color;
    }
}
