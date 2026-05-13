using UnityEngine;
using System.Collections.Generic;

public class ProceduralBuilding : MonoBehaviour
{
    /// <summary>If true (default), each building gets its own real-time point light.
    /// At 30+ buildings this is a huge perf cost — set false for the city-wide ProceduralBuilding
    /// component (we still have emission-based glow from materials).</summary>
    [Tooltip("Per-building real-time point lights kill framerate above ~15 buildings.")]
    public static bool EnablePerBuildingLights = false;

    /// <summary>How many existing buildings get to keep their light. Cheap "hero lighting" cap.</summary>
    public const int MaxLitBuildings = 8;

    // Cache the URP/Lit shader once — Shader.Find is slow and was called per-part-per-building
    static Shader _litShader;
    static Shader LitShader
    {
        get
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _litShader;
        }
    }

    readonly List<Material> _ownedMats = new();
    public Color GlowColor { get; private set; } = Color.white;

    public void Build(string buildingName)
    {
        // Remove root mesh left by placeholder
        var mr = GetComponent<MeshRenderer>();
        var mf = GetComponent<MeshFilter>();
        var col = GetComponent<Collider>();
        if (mr) Destroy(mr);
        if (mf) Destroy(mf);
        if (col) Destroy(col);

        // Remove children from original prefab (drone points added later, after this call)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        switch (buildingName)
        {
            case "Scrap Collector":   BuildScrapCollector();   break;
            case "Energy Generator":  BuildEnergyGenerator();  break;
            case "Polymer Extractor": BuildPolymerExtractor(); break;
            case "Data Tower":        BuildDataTower();        break;
            case "Shelter":           BuildShelter();          break;
            case "Clinic":            BuildClinic();           break;
            case "Scout Hub":         BuildScoutHub();         break;
            case "Charging Station":  BuildChargingStation();  break;
            default:                  BuildGeneric();          break;
        }

        // Root collider for click detection
        var box = gameObject.AddComponent<BoxCollider>();
        box.center  = new Vector3(0, 0.6f, 0);
        box.size    = new Vector3(0.9f, 1.2f, 0.9f);

        // Only add a real-time point light if we're under the cap — emissive materials carry the glow otherwise
        if (EnablePerBuildingLights && Building.Count <= MaxLitBuildings)
            AddPointLight(GlowColor);
    }

    // ── Building recipes ──────────────────────────────────────────────────

    void BuildScrapCollector()
    {
        Color dark = Hex("1E5A8A"); Color glow = Hex("00C8FF");
        Color emit = glow * 2.5f;
        GlowColor = glow;

        Part(Cube, new Vector3(0,    0.08f,  0),    new Vector3(1f,    0.15f, 1f),    dark, Color.black); // platform
        Part(Cube, new Vector3(0,    0.47f,  0),    new Vector3(0.72f, 0.55f, 0.72f), dark, Color.black); // body
        Part(Cube, new Vector3(0.3f, 1.05f,  0),    new Vector3(0.1f,  0.85f, 0.1f),  dark, Color.black); // crane tower
        Part(Cube, new Vector3(0.08f,1.5f,   0),    new Vector3(0.55f, 0.08f, 0.08f), dark, Color.black); // crane arm
        Part(Cube, new Vector3(0,    0.16f,  0.52f),new Vector3(0.85f, 0.05f, 0.04f), glow, emit);        // base strip
        Part(Cube, new Vector3(0,    0.72f,  0.37f),new Vector3(0.65f, 0.04f, 0.04f), glow, emit);        // body strip
    }

    void BuildEnergyGenerator()
    {
        Color dark = Hex("4A3000"); Color glow = Hex("FF8000"); Color dome = Hex("F0C040");
        Color emit = glow * 2.2f;
        GlowColor = glow;

        Part(Cube,     new Vector3(0,    0.4f,  0),    new Vector3(0.88f, 0.8f,  0.88f), dark, Color.black);
        Part(Sphere,   new Vector3(0,    0.98f, 0),    new Vector3(0.52f, 0.42f, 0.52f), dome, emit * 0.4f);
        Part(Cylinder, new Vector3(-0.3f,1.25f, 0),    new Vector3(0.1f,  0.35f, 0.1f),  dark, Color.black);
        Part(Cylinder, new Vector3( 0.3f,1.25f, 0),    new Vector3(0.1f,  0.35f, 0.1f),  dark, Color.black);
        Part(Cube,     new Vector3(0,    0.5f,  0.45f),new Vector3(0.7f,  0.6f,  0.04f), glow, emit);
        Part(Cube,     new Vector3(0,    0.5f, -0.45f),new Vector3(0.7f,  0.6f,  0.04f), glow, emit);
    }

    void BuildPolymerExtractor()
    {
        Color dark = Hex("4A0F99"); Color glow = Hex("B000FF");
        Color emit = glow * 2.8f;
        GlowColor = glow;

        Part(Cube,     new Vector3(0,     0.55f, 0),    new Vector3(0.7f,  1.1f,  0.7f),  dark, Color.black);
        Part(Cylinder, new Vector3(-0.5f, 0.38f, 0),    new Vector3(0.22f, 0.45f, 0.22f), dark, Color.black);
        Part(Cylinder, new Vector3( 0.5f, 0.38f, 0),    new Vector3(0.22f, 0.45f, 0.22f), dark, Color.black);
        Part(Cube,     new Vector3(0,     1.12f, 0.36f),new Vector3(0.6f,  0.05f, 0.04f), glow, emit);
        Part(Cube,     new Vector3(0,     1.12f,-0.36f),new Vector3(0.6f,  0.05f, 0.04f), glow, emit);
        Part(Cube,     new Vector3(0.36f, 1.12f, 0),    new Vector3(0.04f, 0.05f, 0.6f),  glow, emit);
        Part(Cube,     new Vector3(-0.36f,1.12f, 0),    new Vector3(0.04f, 0.05f, 0.6f),  glow, emit);
        Part(Cube,     new Vector3(0,     0.5f,  0.36f),new Vector3(0.6f,  0.05f, 0.04f), glow, emit * 0.5f);
    }

    void BuildDataTower()
    {
        Color dark = Hex("0F3352"); Color glow = Hex("00D4FF");
        Color emit = glow * 2.2f;
        GlowColor = glow;

        Part(Cube,     new Vector3(0,    0.18f, 0),    new Vector3(0.85f, 0.35f, 0.85f), dark, Color.black);
        Part(Cube,     new Vector3(0,    1.15f, 0),    new Vector3(0.28f, 1.8f,  0.28f), dark, Color.black);
        Part(Cube,     new Vector3(0,    2.15f, 0),    new Vector3(0.65f, 0.06f, 0.65f), dark, Color.black);
        Part(Cylinder, new Vector3(0,    2.65f, 0),    new Vector3(0.05f, 0.55f, 0.05f), glow, emit * 3f);
        Part(Cube,     new Vector3(0,    0.9f,  0.15f),new Vector3(0.22f, 0.04f, 0.04f), glow, emit);
        Part(Cube,     new Vector3(0,    1.4f,  0.15f),new Vector3(0.22f, 0.04f, 0.04f), glow, emit);
        Part(Cube,     new Vector3(0,    1.9f,  0.15f),new Vector3(0.22f, 0.04f, 0.04f), glow, emit);
    }

    void BuildShelter()
    {
        Color dark = Hex("1A4A2E"); Color glow = Hex("33FF66"); Color win = Hex("88FFAA");
        Color neonA = Hex("FF2D6B"); Color neonB = Hex("00EEFF");
        Color emit = glow * 1.8f; Color winEmit = win * 0.8f;
        Color emitA = neonA * 3f;  Color emitB = neonB * 3f;
        GlowColor = glow;

        Part(Cube, new Vector3(0,    0.85f, 0),    new Vector3(0.78f, 1.7f,  0.78f), dark, Color.black);
        Part(Cube, new Vector3(0,    1.76f, 0),    new Vector3(0.88f, 0.08f, 0.88f), dark, Color.black);
        // windows front
        Part(Cube, new Vector3(-0.2f,0.6f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        Part(Cube, new Vector3( 0.2f,0.6f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        Part(Cube, new Vector3(-0.2f,1.0f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        Part(Cube, new Vector3( 0.2f,1.0f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        Part(Cube, new Vector3(-0.2f,1.4f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        Part(Cube, new Vector3( 0.2f,1.4f,  0.4f), new Vector3(0.18f, 0.14f, 0.03f), win, winEmit);
        // entry + roof strip
        Part(Cube, new Vector3(0,    0.04f, 0.4f), new Vector3(0.28f, 0.04f, 0.03f), glow, emit);
        Part(Cube, new Vector3(0,    1.78f, 0.4f), new Vector3(0.7f,  0.04f, 0.03f), glow, emit);
        // neon sign frame above door — hot pink
        Part(Cube, new Vector3(0,    0.32f, 0.41f), new Vector3(0.3f,  0.03f, 0.03f), neonA, emitA); // top bar
        Part(Cube, new Vector3(-0.15f,0.18f,0.41f), new Vector3(0.03f, 0.27f, 0.03f), neonA, emitA); // left leg
        Part(Cube, new Vector3( 0.15f,0.18f,0.41f), new Vector3(0.03f, 0.27f, 0.03f), neonA, emitA); // right leg
        // vertical neon tubes on front corners — cyan
        Part(Cube, new Vector3(-0.4f, 0.85f, 0.38f), new Vector3(0.03f, 1.4f, 0.03f), neonB, emitB);
        Part(Cube, new Vector3( 0.4f, 0.85f, 0.38f), new Vector3(0.03f, 1.4f, 0.03f), neonB, emitB);
        // horizontal accent band mid-building — pink
        Part(Cube, new Vector3(0,    1.2f,  0.41f), new Vector3(0.72f, 0.03f, 0.03f), neonA, emitA);
    }

    void BuildClinic()
    {
        Color dark = Hex("1E1E4A"); Color glow = Hex("66CCFF"); Color cross = Hex("FFFFFF");
        Color emit = glow * 2.0f; Color crossEmit = new Color(0.4f, 1.4f, 2.2f);
        GlowColor = glow;

        Part(Cube, new Vector3(0,    0.38f, 0),    new Vector3(0.88f, 0.75f, 0.88f), dark, Color.black);
        Part(Cube, new Vector3(0,    0.88f, 0),    new Vector3(0.68f, 0.22f, 0.68f), dark, Color.black);
        // medical cross
        Part(Cube, new Vector3(0,    1.12f, 0),    new Vector3(0.09f, 0.38f, 0.09f), cross, crossEmit);
        Part(Cube, new Vector3(0,    1.2f,  0),    new Vector3(0.32f, 0.09f, 0.09f), cross, crossEmit);
        // glow strips
        Part(Cube, new Vector3(0,    0.78f, 0.45f),new Vector3(0.75f, 0.04f, 0.04f), glow, emit);
        Part(Cube, new Vector3(0,    0.78f,-0.45f),new Vector3(0.75f, 0.04f, 0.04f), glow, emit);
        Part(Cube, new Vector3(0.45f,0.78f, 0),    new Vector3(0.04f, 0.04f, 0.75f), glow, emit);
        Part(Cube, new Vector3(-0.45f,0.78f,0),    new Vector3(0.04f, 0.04f, 0.75f), glow, emit);
    }

    void BuildScoutHub()
    {
        Color dark = Hex("1A1400"); Color glow = Hex("FFB800"); Color pad = Hex("FF6600");
        Color emit = glow * 2.8f;  Color padEmit = pad * 2.5f;
        GlowColor = glow;

        Part(Cube,     new Vector3(0,     0.08f,  0),     new Vector3(1f,    0.15f, 1f),    dark, Color.black); // base
        Part(Cube,     new Vector3(0,     0.16f,  0),     new Vector3(0.78f, 0.02f, 0.12f), pad,  padEmit);     // pad X
        Part(Cube,     new Vector3(0,     0.16f,  0),     new Vector3(0.12f, 0.02f, 0.78f), pad,  padEmit);     // pad X
        Part(Cube,     new Vector3(-0.35f,0.55f, -0.35f), new Vector3(0.22f, 0.75f, 0.22f), dark, Color.black); // tower
        Part(Cylinder, new Vector3(-0.35f,1.08f, -0.35f), new Vector3(0.04f, 0.38f, 0.04f), glow, emit);        // antenna
        Part(Cube,     new Vector3(-0.35f,0.72f, -0.24f), new Vector3(0.18f, 0.04f, 0.04f), glow, emit);        // tower strip
        Part(Cylinder, new Vector3( 0.42f,0.22f,  0.42f), new Vector3(0.06f, 0.15f, 0.06f), pad,  padEmit);     // corner markers
        Part(Cylinder, new Vector3(-0.42f,0.22f,  0.42f), new Vector3(0.06f, 0.15f, 0.06f), pad,  padEmit);
        Part(Cylinder, new Vector3( 0.42f,0.22f, -0.42f), new Vector3(0.06f, 0.15f, 0.06f), pad,  padEmit);
        Part(Cylinder, new Vector3(-0.42f,0.22f, -0.42f), new Vector3(0.06f, 0.15f, 0.06f), pad,  padEmit);
    }

    void BuildChargingStation()
    {
        Color dark     = Hex("0A2E1C"); Color glow = Hex("00FF88"); Color coil = Hex("44FFBB");
        Color emit     = glow  * 3.0f;  Color coilEmit = coil * 2.5f;
        GlowColor = glow;

        // Landing pad
        Part(Cube, new Vector3(0,     0.05f,  0),      new Vector3(1f,    0.10f, 1f),    dark, Color.black);
        // Pad guide rings (cross pattern)
        Part(Cube, new Vector3(0,     0.11f,  0.30f),  new Vector3(0.80f, 0.02f, 0.04f), glow, emit);
        Part(Cube, new Vector3(0,     0.11f, -0.30f),  new Vector3(0.80f, 0.02f, 0.04f), glow, emit);
        Part(Cube, new Vector3( 0.30f,0.11f,  0),      new Vector3(0.04f, 0.02f, 0.80f), glow, emit);
        Part(Cube, new Vector3(-0.30f,0.11f,  0),      new Vector3(0.04f, 0.02f, 0.80f), glow, emit);
        // Central charging tower body
        Part(Cube, new Vector3(0,     0.46f,  0),      new Vector3(0.26f, 0.72f, 0.26f), dark, Color.black);
        // Coil rings — lower band
        Part(Cube, new Vector3( 0.14f,0.52f,  0),      new Vector3(0.04f, 0.04f, 0.24f), coil, coilEmit);
        Part(Cube, new Vector3(-0.14f,0.52f,  0),      new Vector3(0.04f, 0.04f, 0.24f), coil, coilEmit);
        Part(Cube, new Vector3(0,     0.52f,  0.14f),  new Vector3(0.24f, 0.04f, 0.04f), coil, coilEmit);
        Part(Cube, new Vector3(0,     0.52f, -0.14f),  new Vector3(0.24f, 0.04f, 0.04f), coil, coilEmit);
        // Coil rings — upper band
        Part(Cube, new Vector3( 0.14f,0.76f,  0),      new Vector3(0.04f, 0.04f, 0.24f), coil, coilEmit);
        Part(Cube, new Vector3(-0.14f,0.76f,  0),      new Vector3(0.04f, 0.04f, 0.24f), coil, coilEmit);
        Part(Cube, new Vector3(0,     0.76f,  0.14f),  new Vector3(0.24f, 0.04f, 0.04f), coil, coilEmit);
        Part(Cube, new Vector3(0,     0.76f, -0.14f),  new Vector3(0.24f, 0.04f, 0.04f), coil, coilEmit);
        // Top beacon orb
        Part(Sphere, new Vector3(0,   1.02f,  0),      new Vector3(0.20f, 0.20f, 0.20f), glow, emit * 2f);
    }

    void BuildGeneric()
    {
        Part(Cube, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 1f, 0.8f), Hex("2A2A5E"), Color.black);
    }

    // ── Upgrade visuals ───────────────────────────────────────────────────

    public void UpgradeVisual(int level)
    {
        Transform existing = transform.Find("UpgradeVisuals");
        if (existing != null) Destroy(existing.gameObject);
        if (level <= 1) return;

        GameObject container = new GameObject("UpgradeVisuals");
        container.transform.SetParent(transform, false);

        bool gold = level >= 4;
        Color color = gold ? Hex("FFB800") : Hex("00C8FF");
        Color emit  = color * 3.5f;

        float pillarH = 0.1f + (level - 1) * 0.12f;
        float pillarY = pillarH * 0.5f;
        float offset  = 0.48f;

        // Corner pillars — grow taller each level
        PartIn(container.transform, Cube, new Vector3(-offset, pillarY, -offset), new Vector3(0.06f, pillarH, 0.06f), color, emit);
        PartIn(container.transform, Cube, new Vector3( offset, pillarY, -offset), new Vector3(0.06f, pillarH, 0.06f), color, emit);
        PartIn(container.transform, Cube, new Vector3(-offset, pillarY,  offset), new Vector3(0.06f, pillarH, 0.06f), color, emit);
        PartIn(container.transform, Cube, new Vector3( offset, pillarY,  offset), new Vector3(0.06f, pillarH, 0.06f), color, emit);

        if (level >= 3)
        {
            // Connecting bands at top of pillars
            float bandY = pillarH;
            float span  = offset * 2f + 0.06f;
            PartIn(container.transform, Cube, new Vector3(0,  bandY, -offset), new Vector3(span, 0.04f, 0.04f), color, emit);
            PartIn(container.transform, Cube, new Vector3(0,  bandY,  offset), new Vector3(span, 0.04f, 0.04f), color, emit);
            PartIn(container.transform, Cube, new Vector3(-offset, bandY, 0),  new Vector3(0.04f, 0.04f, span), color, emit);
            PartIn(container.transform, Cube, new Vector3( offset, bandY, 0),  new Vector3(0.04f, 0.04f, span), color, emit);
        }

        if (level >= 5)
        {
            // Apex orb
            PartIn(container.transform, Sphere, new Vector3(0, 2.3f, 0), new Vector3(0.22f, 0.22f, 0.22f), color, emit * 1.5f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static PrimitiveType Cube     => PrimitiveType.Cube;
    static PrimitiveType Sphere   => PrimitiveType.Sphere;
    static PrimitiveType Cylinder => PrimitiveType.Cylinder;

    void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Color baseCol, Color emission)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Destroy(go.GetComponent<Collider>());

        Material mat = new Material(LitShader);
        mat.SetColor("_BaseColor", baseCol);
        mat.SetColor("_Color", baseCol);
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        go.GetComponent<Renderer>().material = mat;
        _ownedMats.Add(mat);
    }

    void PartIn(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color baseCol, Color emission)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Destroy(go.GetComponent<Collider>());

        Material mat = new Material(LitShader);
        mat.SetColor("_BaseColor", baseCol);
        mat.SetColor("_Color", baseCol);
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        go.GetComponent<Renderer>().material = mat;
        _ownedMats.Add(mat);
    }

    void AddPointLight(Color color)
    {
        GameObject lightGO = new GameObject("BuildingLight");
        lightGO.transform.SetParent(transform, false);
        lightGO.transform.localPosition = new Vector3(0, 1.2f, 0);
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 1.8f;
        light.range = 4.5f;
        light.shadows = LightShadows.None;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    void OnDestroy()
    {
        foreach (var m in _ownedMats)
            if (m != null) Destroy(m);
    }
}
