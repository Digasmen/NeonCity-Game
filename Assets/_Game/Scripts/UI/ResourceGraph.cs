using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small collapsible panel showing 60-second sparkline graphs for resources
/// that have a non-zero rate.  Sits below the resource bar on the left side.
/// Auto-instantiates — no scene wiring required.
/// </summary>
public class ResourceGraph : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<ResourceGraph>() == null)
            new GameObject("_ResourceGraph").AddComponent<ResourceGraph>();
    }

    const float PanelW    = 190f;
    const float LineH     = 36f;
    const float LineGap   = 4f;
    const float HeaderH   = 22f;
    const float PadV      = 6f;
    const float UpdateInt = 1.2f;

    Canvas    _canvas;
    GameObject _panel;
    float     _timer;

    // One entry per resource type we're currently showing
    struct LineEntry
    {
        public ResourceType     type;
        public SparklineGraphic spark;
        public TextMeshProUGUI  valLbl;
        public TextMeshProUGUI  rateLbl;
    }

    readonly System.Collections.Generic.List<LineEntry> _lines = new();

    // Map resource types to display colours
    static Color SparkColor(ResourceType t) => t switch
    {
        ResourceType.Scrap      => new Color(0.95f, 0.75f, 0.30f),
        ResourceType.Energy     => new Color(1.00f, 0.65f, 0.10f),
        ResourceType.Polymer    => new Color(0.75f, 0.35f, 1.00f),
        ResourceType.Data       => new Color(0.10f, 0.85f, 1.00f),
        ResourceType.Population => new Color(0.20f, 1.00f, 0.50f),
        ResourceType.Nano       => new Color(1.00f, 1.00f, 0.40f),
        _                       => Color.white,
    };

    void Start()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvas = c; break; }
        if (_canvas == null) return;

        BuildPanel();
    }

    void BuildPanel()
    {
        _panel = new GameObject("ResourceGraphPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        UIUtils.Rounded(_panel, UIUtils.PanelBg, 10);

        var rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -50f);   // just below resource bar
        rt.sizeDelta        = new Vector2(PanelW, HeaderH + PadV);

        // Header
        var hdr = UIUtils.Label(_panel.transform, "Hdr", "◈  TRENDS", 8f,
            UIUtils.TextSub, FontStyles.Bold, TextAlignmentOptions.Left);
        var hRT = hdr.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot     = new Vector2(0f, 1f);
        hRT.anchoredPosition = new Vector2(10f, -4f);
        hRT.sizeDelta = new Vector2(-20f, HeaderH - 4f);

        RefreshLines();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= UpdateInt)
        {
            _timer = 0f;
            UpdateGraphs();
        }
    }

    // Rebuild which resource rows are shown (only those with rate != 0)
    void RefreshLines()
    {
        // Destroy existing line rows (skip the header label at child index 0)
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in _panel.transform)
            if (child.name != "Hdr") toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) Destroy(go);
        _lines.Clear();

        if (ResourceManager.Instance == null) return;

        int row = 0;
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (Mathf.Abs(ResourceManager.Instance.GetRate(type)) < 0.01f) continue;
            AddLine(type, row++);
        }

        // Resize panel height
        float h = HeaderH + PadV + row * (LineH + LineGap) + PadV;
        _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelW, Mathf.Max(h, HeaderH + PadV * 2f));
    }

    void AddLine(ResourceType type, int row)
    {
        Color col   = SparkColor(type);
        float yOffset = HeaderH + PadV + row * (LineH + LineGap);

        var lineGO = new GameObject(type.ToString());
        lineGO.transform.SetParent(_panel.transform, false);
        var lineRT = lineGO.AddComponent<RectTransform>();
        UIUtils.PinTop(lineRT, yOffset, LineH, 8f, 8f);

        // Type label
        var typeLbl = UIUtils.Label(lineGO.transform, "Type", type.ToString().ToUpper()[..3], 7.5f,
            col, FontStyles.Bold, TextAlignmentOptions.Left);
        var tlRT = typeLbl.GetComponent<RectTransform>();
        tlRT.anchorMin = new Vector2(0f, 0.5f); tlRT.anchorMax = new Vector2(0f, 0.5f);
        tlRT.pivot     = new Vector2(0f, 0.5f);
        tlRT.anchoredPosition = Vector2.zero;
        tlRT.sizeDelta = new Vector2(24f, LineH);

        // Sparkline
        var sparkGO = new GameObject("Spark");
        sparkGO.transform.SetParent(lineGO.transform, false);
        var sparkRT = sparkGO.AddComponent<RectTransform>();
        sparkRT.anchorMin = new Vector2(0f, 0f); sparkRT.anchorMax = new Vector2(1f, 1f);
        sparkRT.offsetMin = new Vector2(28f, 2f); sparkRT.offsetMax = new Vector2(-56f, -2f);
        var spark = sparkGO.AddComponent<SparklineGraphic>();
        spark.color     = col;
        spark.lineWidth = 1.2f;

        // Value label (right)
        var valLbl = UIUtils.Label(lineGO.transform, "Val", "", 8f,
            UIUtils.TextMain, FontStyles.Bold, TextAlignmentOptions.Right);
        var vlRT = valLbl.GetComponent<RectTransform>();
        vlRT.anchorMin = new Vector2(1f, 0f); vlRT.anchorMax = new Vector2(1f, 1f);
        vlRT.pivot     = new Vector2(1f, 0.5f);
        vlRT.anchoredPosition = Vector2.zero;
        vlRT.sizeDelta = new Vector2(50f, LineH);

        // Rate label (below value)
        var rateLbl = UIUtils.Label(lineGO.transform, "Rate", "", 7f,
            UIUtils.TextSub, FontStyles.Normal, TextAlignmentOptions.Right);
        var rlRT = rateLbl.GetComponent<RectTransform>();
        rlRT.anchorMin = new Vector2(1f, 0f); rlRT.anchorMax = new Vector2(1f, 0.45f);
        rlRT.pivot     = new Vector2(1f, 0f);
        rlRT.anchoredPosition = Vector2.zero;
        rlRT.sizeDelta = new Vector2(50f, 0f);

        _lines.Add(new LineEntry
        {
            type    = type,
            spark   = spark,
            valLbl  = valLbl,
            rateLbl = rateLbl,
        });
    }

    void UpdateGraphs()
    {
        if (ResourceManager.Instance == null) return;

        // Check if the set of active resources has changed
        bool needRebuild = false;
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            bool hasRate = Mathf.Abs(ResourceManager.Instance.GetRate(type)) >= 0.01f;
            bool inList  = _lines.Exists(l => l.type == type);
            if (hasRate != inList) { needRebuild = true; break; }
        }
        if (needRebuild) { RefreshLines(); return; }

        // Update existing lines
        foreach (var line in _lines)
        {
            float[] hist = ResourceManager.Instance.GetHistory(line.type);
            line.spark.data = hist;
            line.spark.SetVerticesDirty();

            float cur  = ResourceManager.Instance.Get(line.type);
            float rate = ResourceManager.Instance.GetRate(line.type);
            line.valLbl.text  = cur  >= 1000f ? $"{cur/1000f:0.#}k" : $"{cur:0}";
            line.rateLbl.text = rate >= 0f ? $"+{rate:0.#}/m" : $"{rate:0.#}/m";
            line.rateLbl.color = rate >= 0f ? UIUtils.Green : UIUtils.Red;
        }
    }
}
