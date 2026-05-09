using UnityEngine;

public class DroneVisuals : MonoBehaviour
{
    public Color glowColor = new Color(0.3f, 0.8f, 1f);

    [Header("Pulse")]
    public float minIntensity = 0.8f;
    public float maxIntensity = 2.2f;
    public float pulseSpeed = 3f;

    private Material droneMat;

    void Start()
    {
        SetupGlow();
        SetupTrail();
    }

    void SetupGlow()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null) return;
        droneMat = r.material;
        droneMat.EnableKeyword("_EMISSION");
        droneMat.SetColor("_EmissionColor", glowColor * maxIntensity);
    }

    void SetupTrail()
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Material trailMat = new Material(Shader.Find("Sprites/Default"));
        trailMat.color = glowColor;
        trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(glowColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    void Update()
    {
        if (droneMat == null) return;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        droneMat.SetColor("_EmissionColor", glowColor * intensity);
    }

    void OnDestroy()
    {
        if (droneMat != null) Destroy(droneMat);
    }
}
