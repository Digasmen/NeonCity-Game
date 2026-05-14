using UnityEngine;

/// <summary>
/// 240-second day/night loop driven by unscaled real time (pausing freezes the cycle).
/// Drives the scene directional light, ambient light, and fog.
/// Other systems (BuildingGlow, BuildingLabel, SoundManager) subscribe via the
/// public <see cref="NightAmount"/> property.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<DayNightCycle>() == null)
            new GameObject("_DayNightCycle").AddComponent<DayNightCycle>();
    }

    // ── Public state ──────────────────────────────────────────────────────
    /// <summary>0 = full day, 1 = full night. Smooth sine curve, 240 s period.</summary>
    public float NightAmount { get; private set; } = 0f;

    // ── Config ────────────────────────────────────────────────────────────
    [Header("Cycle")]
    [Tooltip("Real-time seconds for one full day+night cycle.")]
    public float cycleDuration = 240f;

    [Header("Directional Light")]
    public Color  dayLightColor   = new Color(0.90f, 0.93f, 1.00f);     // cool white
    public Color  nightLightColor = new Color(0.18f, 0.08f, 0.28f);     // deep violet
    public float  dayIntensity    = 1.20f;
    public float  nightIntensity  = 0.35f;

    [Header("Ambient")]
    public Color  dayAmbient   = new Color(0.12f, 0.14f, 0.22f);
    public Color  nightAmbient = new Color(0.04f, 0.02f, 0.08f);

    [Header("Fog")]
    public Color  dayFog   = new Color(0.09f, 0.11f, 0.18f);
    public Color  nightFog = new Color(0.02f, 0.01f, 0.05f);

    // ── Private ───────────────────────────────────────────────────────────
    Light _dirLight;

    void Awake()
    {
        Instance  = this;
        _dirLight = FindFirstObjectByType<Light>();
    }

    void Update()
    {
        // Smooth sine wave: 0 at t=0 (dawn), 1 at t=0.5 (midnight), 0 at t=1 (next dawn)
        float t = (Time.unscaledTime % cycleDuration) / cycleDuration;
        NightAmount = (1f - Mathf.Cos(t * 2f * Mathf.PI)) * 0.5f;

        // ── Directional light ──────────────────────────────────────────────
        if (_dirLight != null)
        {
            _dirLight.color     = Color.Lerp(dayLightColor,  nightLightColor, NightAmount);
            _dirLight.intensity = Mathf.Lerp(dayIntensity,   nightIntensity,  NightAmount);
        }

        // ── Scene ambient ──────────────────────────────────────────────────
        RenderSettings.ambientLight = Color.Lerp(dayAmbient, nightAmbient, NightAmount);

        // ── Fog ────────────────────────────────────────────────────────────
        if (RenderSettings.fog)
            RenderSettings.fogColor = Color.Lerp(dayFog, nightFog, NightAmount);
    }
}
