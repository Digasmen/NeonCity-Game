using UnityEngine;

public class BuildingGlow : MonoBehaviour
{
    [Header("Pulse")]
    public float minIntensity = 0.4f;
    public float maxIntensity = 2.5f;
    public float beatInterval = 1.7f;

    private Material mat;
    private Color baseColor;

    void Start()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null) return;

        mat = r.material;
        Color emission = mat.GetColor("_EmissionColor");

        // Strip intensity to get pure hue
        float peak = Mathf.Max(emission.r, emission.g, emission.b);
        baseColor = peak > 0 ? emission / peak : Color.white;
    }

    void Update()
    {
        if (mat == null) return;

        float beat = (Mathf.Sin(Time.time * (2f * Mathf.PI / beatInterval)) + 1f) / 2f;

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, beat);
        mat.SetColor("_EmissionColor", baseColor * intensity);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
