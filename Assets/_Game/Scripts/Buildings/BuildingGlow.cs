using UnityEngine;
using System.Collections.Generic;

public class BuildingGlow : MonoBehaviour
{
    public float minIntensity = 0.4f;
    public float maxIntensity = 2.5f;
    public float beatInterval = 1.7f;

    // Point-light pulse range (world units of intensity, not lumen)
    const float LightMin = 0.0f;
    const float LightMax = 2.2f;

    float _phaseOffset;
    Light _light;

    struct GlowTarget { public Material mat; public Color baseColor; }
    readonly List<GlowTarget> _targets = new();

    void Start()
    {
        _phaseOffset = (GetInstanceID() % 1000) / 1000f * Mathf.PI * 2f;

        // Grab the GlowLight spawned by Building.Initialize()
        _light = GetComponent<Building>()?.BuildingLight;

        Rebuild();
    }

    public void Rebuild()
    {
        _targets.Clear();
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in r.materials)
            {
                if (!mat.IsKeywordEnabled("_EMISSION")) continue;
                Color emission = mat.GetColor("_EmissionColor");
                float peak = Mathf.Max(emission.r, emission.g, emission.b);
                if (peak <= 0) continue;
                _targets.Add(new GlowTarget { mat = mat, baseColor = emission / peak });
            }
        }
    }

    void Update()
    {
        float beat = (Mathf.Sin(Time.time * (2f * Mathf.PI / beatInterval) + _phaseOffset) + 1f) / 2f;

        // Night boost: emissions and lights get brighter as the world darkens
        float night      = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightAmount : 0f;
        float nightBoost = Mathf.Lerp(1f, 2.0f, night);

        // ── Emission pulse ────────────────────────────────────────────────
        if (_targets.Count > 0)
        {
            float emitIntensity = Mathf.Lerp(minIntensity, maxIntensity, beat) * nightBoost;
            foreach (var t in _targets)
                t.mat.SetColor("_EmissionColor", t.baseColor * emitIntensity);
        }

        // ── Point-light pulse (in sync with emission) ─────────────────────
        if (_light != null)
        {
            // At night the light range also expands slightly for a more dramatic look
            _light.intensity = Mathf.Lerp(LightMin, LightMax, beat) * nightBoost;
            _light.range     = Mathf.Lerp(4f, 6f, night);
        }
    }
}
