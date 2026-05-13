using UnityEngine;
using System.Collections.Generic;

public class BuildingGlow : MonoBehaviour
{
    public float minIntensity = 0.4f;
    public float maxIntensity = 2.5f;
    public float beatInterval = 1.7f;

    float _phaseOffset;

    struct GlowTarget { public Material mat; public Color baseColor; }
    readonly List<GlowTarget> _targets = new();

    void Start()
    {
        _phaseOffset = (GetInstanceID() % 1000) / 1000f * Mathf.PI * 2f;
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
        if (_targets.Count == 0) return;
        float beat = (Mathf.Sin(Time.time * (2f * Mathf.PI / beatInterval) + _phaseOffset) + 1f) / 2f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, beat);
        foreach (var t in _targets)
            t.mat.SetColor("_EmissionColor", t.baseColor * intensity);
    }
}
