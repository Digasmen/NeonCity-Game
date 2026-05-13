using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top-right procedural mini-map.  Renders the grid as a texture:
///   • Fog = very dark navy
///   • Revealed empty = dark blue-grey
///   • Buildings = coloured dot per category
/// Updates every 0.4 s to keep GC low.
/// Auto-instantiates — no scene wiring required.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<MinimapUI>() == null)
            new GameObject("_MinimapUI").AddComponent<MinimapUI>();
    }

    const float PanelSize = 130f;   // square panel, px
    const float UpdateInterval = 0.4f;

    RawImage  _mapImage;
    Texture2D _tex;
    float     _timer;

    // Pre-allocated pixel array
    Color32[] _pixels;
    int       _gridW, _gridH;

    // Dot colours per building name (must match ProceduralBuilding palette)
    static Color32 Col(float r, float g, float b) => new Color32((byte)(r*255), (byte)(g*255), (byte)(b*255), 255);
    static readonly Dictionary<string, Color32> _buildingColors = new()
    {
        ["Scrap Collector"]   = Col(0.10f, 0.78f, 1.00f),
        ["Energy Generator"]  = Col(1.00f, 0.50f, 0.00f),
        ["Polymer Extractor"] = Col(0.69f, 0.00f, 1.00f),
        ["Data Tower"]        = Col(0.00f, 0.83f, 1.00f),
        ["Shelter"]           = Col(0.20f, 1.00f, 0.40f),
        ["Clinic"]            = Col(0.40f, 0.80f, 1.00f),
        ["Scout Hub"]         = Col(1.00f, 0.72f, 0.00f),
        ["Charging Station"]  = Col(0.00f, 1.00f, 0.53f),
    };

    static readonly Color32 ColorFog      = new Color32(8,  12,  25,  255);
    static readonly Color32 ColorRevealed = new Color32(20, 32,  58,  255);

    void Start()
    {
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) return;

        BuildPanel(canvas.transform);
    }

    void BuildPanel(Transform root)
    {
        // ── Outer panel ──────────────────────────────────────────────────
        var panel = new GameObject("MinimapPanel");
        panel.transform.SetParent(root, false);
        UIUtils.Rounded(panel, UIUtils.PanelBg, 10);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta        = new Vector2(PanelSize + 14f, PanelSize + 30f);

        // Header
        var hdrLbl = UIUtils.Label(panel.transform, "Hdr", "MINIMAP", 8f,
            UIUtils.Cyan, FontStyles.Bold, TextAlignmentOptions.Center);
        var hRT = hdrLbl.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot     = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -4f);
        hRT.sizeDelta = new Vector2(0f, 14f);

        // Map image
        var mapGO = new GameObject("Map");
        mapGO.transform.SetParent(panel.transform, false);
        var mapRT = mapGO.AddComponent<RectTransform>();
        mapRT.anchorMin        = new Vector2(0.5f, 0f);
        mapRT.anchorMax        = new Vector2(0.5f, 0f);
        mapRT.pivot            = new Vector2(0.5f, 0f);
        mapRT.anchoredPosition = new Vector2(0f, 6f);
        mapRT.sizeDelta        = new Vector2(PanelSize, PanelSize);

        _mapImage = mapGO.AddComponent<RawImage>();

        // One pixel per grid cell
        _gridW = GridManager.Instance != null ? GridManager.Instance.width  : 20;
        _gridH = GridManager.Instance != null ? GridManager.Instance.height : 20;
        _tex    = new Texture2D(_gridW, _gridH, TextureFormat.RGBA32, false);
        _tex.filterMode = FilterMode.Point;
        _pixels = new Color32[_gridW * _gridH];
        _mapImage.texture = _tex;

        // Initial render
        RenderMap();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= UpdateInterval)
        {
            _timer = 0f;
            RenderMap();
        }
    }

    void RenderMap()
    {
        if (_tex == null || FogManager.Instance == null || GridManager.Instance == null) return;

        // Paint fog/revealed base
        for (int y = 0; y < _gridH; y++)
        for (int x = 0; x < _gridW; x++)
        {
            bool revealed = FogManager.Instance.IsCellRevealed(x, y);
            _pixels[y * _gridW + x] = revealed ? ColorRevealed : ColorFog;
        }

        // Paint buildings (uses static registry — no per-frame FindObjectsByType)
        foreach (var b in Building.All)
        {
            int gx = b.gridCell.x;
            int gy = b.gridCell.y;
            if (gx < 0 || gx >= _gridW || gy < 0 || gy >= _gridH) continue;

            Color32 col = _buildingColors.TryGetValue(b.data?.buildingName ?? "", out var c)
                ? c
                : new Color32(180, 180, 200, 255);
            _pixels[gy * _gridW + gx] = col;
        }

        _tex.SetPixels32(_pixels);
        _tex.Apply(false);
    }

    void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }
}
