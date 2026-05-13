using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns procedural ParticleSystem bursts at world positions.
///   • PlayBuildPlace(pos)  — small cyan spark burst when a building is placed.
///   • PlayMilestone()      — large multi-colour celebration burst at centre of view.
/// All particle systems are self-destroying after playback.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<ParticleManager>() == null)
            new GameObject("_ParticleManager").AddComponent<ParticleManager>();
    }

    void Awake()
    {
        Instance = this;
        MilestoneManager.Instance.OnMilestoneCompleted += _ => PlayMilestone();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Small spark burst at the building's world position.</summary>
    public void PlayBuildPlace(Vector3 worldPos)
        => StartCoroutine(SpawnBurst(worldPos,
            count: 16, speed: 2.5f, lifetime: 0.55f,
            size: 0.08f, color: new Color(0.15f, 0.85f, 1f)));

    /// <summary>Large celebration burst above the city centre.</summary>
    public void PlayMilestone()
    {
        // Find centroid of all buildings, fall back to grid centre
        var buildings = Building.All;
        Vector3 centre = Vector3.zero;
        if (buildings.Count > 0)
        {
            foreach (var b in buildings) centre += b.transform.position;
            centre /= buildings.Count;
        }
        else
        {
            GridManager gm = GridManager.Instance;
            if (gm != null)
                centre = gm.GetWorldPosition(gm.width / 2, gm.height / 2);
        }
        centre += Vector3.up * 2f;

        // Gold burst
        StartCoroutine(SpawnBurst(centre,
            count: 60, speed: 5f, lifetime: 1.4f,
            size: 0.14f, color: new Color(1f, 0.85f, 0.1f)));

        // Cyan burst (slight delay)
        StartCoroutine(DelayedBurst(0.12f, centre,
            count: 40, speed: 4f, lifetime: 1.2f,
            size: 0.10f, color: new Color(0.1f, 0.85f, 1f)));

        // Green burst (second delay)
        StartCoroutine(DelayedBurst(0.24f, centre,
            count: 30, speed: 3.5f, lifetime: 1.0f,
            size: 0.08f, color: new Color(0.2f, 1f, 0.5f)));
    }

    // ── Coroutines ─────────────────────────────────────────────────────────

    IEnumerator DelayedBurst(float delay, Vector3 pos,
        int count, float speed, float lifetime, float size, Color color)
    {
        yield return new WaitForSeconds(delay);
        yield return SpawnBurst(pos, count, speed, lifetime, size, color);
    }

    IEnumerator SpawnBurst(Vector3 worldPos,
        int count, float speed, float lifetime, float size, Color color)
    {
        var go  = new GameObject("Burst");
        go.transform.position = worldPos;
        var ps  = go.AddComponent<ParticleSystem>();
        var psR = go.GetComponent<ParticleSystemRenderer>();

        // Main module
        var main = ps.main;
        main.startLifetime      = lifetime;
        main.startSpeed         = speed;
        main.startSize          = size;
        main.startColor         = color;
        main.maxParticles       = count * 2;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.gravityModifier    = 0.15f;

        // Emission — one burst
        var emission = ps.emission;
        emission.enabled        = false;

        // Shape — sphere
        var shape = ps.shape;
        shape.enabled           = true;
        shape.shapeType         = ParticleSystemShapeType.Sphere;
        shape.radius            = 0.1f;

        // Colour over lifetime: fade out at end
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new(color, 0f), new(color, 0.7f), new(Color.white, 1f) },
            new GradientAlphaKey[] { new(1f, 0f), new(1f, 0.6f), new(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Renderer: use URP particles shader, fall back to built-in if not found
        psR.renderMode = ParticleSystemRenderMode.Billboard;
        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default");
        if (pShader != null) psR.material = new Material(pShader);

        // Fire burst
        ps.Emit(count);

        // Wait for particles to die, then clean up
        yield return new WaitForSeconds(lifetime + 0.2f);
        Destroy(go);
    }
}
