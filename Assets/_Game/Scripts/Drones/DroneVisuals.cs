using UnityEngine;

public class DroneVisuals : MonoBehaviour
{
    public Color droneColor = new Color(0.05f, 0.35f, 1f);

    [Header("Pulse")]
    public float minIntensity = 0.8f;
    public float maxIntensity = 2.2f;
    public float pulseSpeed = 3f;

    private Material droneMat;
    private Material trimMat;
    private Drone    _drone;

    void Start()
    {
        _drone = GetComponent<Drone>() ?? GetComponentInParent<Drone>();
        if (_drone != null && _drone.data != null)
            droneColor = _drone.data.droneColor;

        SetupGlow();
        SetupTrail();
        SetupHum();
    }

    void SetupGlow()
    {
        Renderer r = (Renderer)GetComponentInChildren<SkinnedMeshRenderer>()
                  ?? GetComponentInChildren<MeshRenderer>();
        if (r == null) return;

        Material[] mats = r.materials;
        foreach (Material m in mats)
        {
            if (m.HasProperty("_Ramp"))
            {
                droneMat = m;
                Color body = droneColor;
                body.a = 1f;
                m.SetColor("_Color", body);
                m.SetColor("_BaseColor", body);
                m.SetColor("_HColor", Color.Lerp(droneColor, Color.white, 0.5f));
                m.SetColor("_SColor", droneColor * 0.3f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", droneColor * maxIntensity);
            }
            else if (m.HasProperty("_BaseColor"))
            {
                trimMat = m;
                Color trim = droneColor * 0.25f;
                trim.a = 1f;
                m.SetColor("_BaseColor", trim);
                if (m.HasProperty("_Color")) m.SetColor("_Color", trim);
            }
        }

        // Fallback: no Toon shader found (sphere primitive or URP-only model)
        if (droneMat == null && trimMat != null)
        {
            droneMat = trimMat;
            trimMat = null;
            droneMat.EnableKeyword("_EMISSION");
            droneMat.SetColor("_EmissionColor", droneColor * maxIntensity);
            Color body = droneColor * 0.5f;
            body.a = 1f;
            droneMat.SetColor("_BaseColor", body);
            if (droneMat.HasProperty("_Color")) droneMat.SetColor("_Color", body);
        }
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
        trailMat.color = droneColor;
        trail.material = trailMat;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(droneColor, 0f),
                new GradientColorKey(droneColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    void SetupHum()
    {
        AudioClip clip = SoundManager.Instance.droneHumClip;
        if (clip == null) return;

        AudioSource hum = gameObject.AddComponent<AudioSource>();
        hum.clip = clip;
        hum.loop = true;
        hum.volume = 0.3f;
        hum.spatialBlend = 1f;
        hum.minDistance = 2f;
        hum.maxDistance = 20f;
        hum.rolloffMode = AudioRolloffMode.Linear;
        hum.Play();
    }

    void Update()
    {
        if (droneMat == null) return;

        // Battery visual: blend toward red and pulse faster when low
        float batteryFactor = _drone != null ? _drone.battery / 100f : 1f;

        Color emitColor;
        float speed;

        if (batteryFactor < 0.25f)
        {
            // 0 = dead (full red), 0.25 = threshold (drone color)
            float t     = batteryFactor / 0.25f;
            emitColor   = Color.Lerp(Color.red, droneColor, t);
            speed       = Mathf.Lerp(pulseSpeed * 4f, pulseSpeed, t);  // faster flash when low
        }
        else
        {
            emitColor = droneColor;
            speed     = pulseSpeed;
        }

        float intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        droneMat.SetColor("_EmissionColor", emitColor * intensity);
    }

    void OnDestroy()
    {
        if (droneMat != null) Destroy(droneMat);
        if (trimMat != null) Destroy(trimMat);
    }
}
