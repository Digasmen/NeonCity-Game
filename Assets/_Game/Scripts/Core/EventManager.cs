using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fires random gameplay events every 60–180 seconds.
/// Events can be positive (bonus resources) or negative (temporary rate debuffs).
/// Shows a toast and plays an alert sound; effects expire automatically.
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<EventManager>() == null)
            new GameObject("_EventManager").AddComponent<EventManager>();
    }

    public enum EffectKind
    {
        ResourceInstant,     // amount: immediate add/spend
        RateDelta,           // rateDelta + duration: temporary rate change
        BuildingStop,        // targetBuildingName + duration: random building of name halts
        BuildingMultiplier,  // targetBuildingName + multiplier + duration: random building output ×mult
        DroneSpeed,          // multiplier + duration: scales drone speed globally
        CapReduction,        // resourceType + multiplier + duration: scales maxAmount
    }

    [System.Serializable]
    struct GameEvent
    {
        public string       title;
        public string       description;
        public bool         isPositive;
        public EffectKind   kind;
        public ResourceType resourceType;
        public float        amount;          // immediate bonus / penalty
        public float        rateDelta;       // temporary rate change per minute
        public float        duration;        // seconds (0 = instant only)
        public string       targetBuildingName;  // for BuildingStop / BuildingMultiplier
        public float        multiplier;          // for BuildingMultiplier / DroneSpeed / CapReduction
    }

    /// <summary>Event-driven drone speed scalar — multiplies with DecreeManager.DroneSpeedMultiplier in Drone.MoveTowards.</summary>
    public static float DroneSpeedEventMult { get; private set; } = 1f;

    readonly GameEvent[] _events =
    {
        new GameEvent { title = "SALVAGE CACHE",      description = "Scouts found a cache of materials!",
            isPositive = true,  kind = EffectKind.ResourceInstant,
            resourceType = ResourceType.Scrap,   amount = 150f },

        new GameEvent { title = "ENERGY SURGE",       description = "Grid output spiked — capacitors charged.",
            isPositive = true,  kind = EffectKind.ResourceInstant,
            resourceType = ResourceType.Energy,  amount = 80f },

        new GameEvent { title = "POLYMER DEPOSIT",    description = "Survey drones located a polymer seam.",
            isPositive = true,  kind = EffectKind.RateDelta,
            resourceType = ResourceType.Polymer, amount = 60f,  rateDelta = 5f,   duration = 90f },

        new GameEvent { title = "SYSTEM GLITCH",      description = "Electrical interference disrupts production.",
            isPositive = false, kind = EffectKind.RateDelta,
            resourceType = ResourceType.Energy,  rateDelta = -15f, duration = 45f },

        new GameEvent { title = "DATA BREACH",        description = "Intrusion detected — data rerouted.",
            isPositive = false, kind = EffectKind.RateDelta,
            resourceType = ResourceType.Data,    amount = -40f, rateDelta = -8f,  duration = 30f },

        new GameEvent { title = "POPULATION INFLUX",  description = "Refugees arrive seeking shelter.",
            isPositive = true,  kind = EffectKind.RateDelta,
            resourceType = ResourceType.Population, amount = 20f, rateDelta = 3f, duration = 120f },

        new GameEvent { title = "SCRAP STORM",        description = "Corrosive weather damages collection gear.",
            isPositive = false, kind = EffectKind.RateDelta,
            resourceType = ResourceType.Scrap,   rateDelta = -20f, duration = 40f },

        new GameEvent { title = "NANO RECOVERY",      description = "Nano-assembler cache discovered in ruins.",
            isPositive = true,  kind = EffectKind.ResourceInstant,
            resourceType = ResourceType.Nano,    amount = 35f },

        // ── Threat events ──────────────────────────────────────────────────
        new GameEvent { title = "BROWNOUT",           description = "Generator sector failed — output offline.",
            isPositive = false, kind = EffectKind.BuildingStop,
            targetBuildingName = "Energy Generator", duration = 60f },

        new GameEvent { title = "DATA CORRUPTION",    description = "Tower compromised — throughput halved.",
            isPositive = false, kind = EffectKind.BuildingMultiplier,
            targetBuildingName = "Data Tower", multiplier = 0.5f, duration = 90f },

        new GameEvent { title = "DRONE STORM",        description = "Ion storm — drones slow to a crawl.",
            isPositive = false, kind = EffectKind.DroneSpeed,
            multiplier = 0.5f, duration = 45f },

        new GameEvent { title = "POLYMER LEAK",       description = "Containment breach — storage cap reduced.",
            isPositive = false, kind = EffectKind.CapReduction,
            resourceType = ResourceType.Polymer, multiplier = 0.7f, duration = 60f },
    };

    const float MinInterval = 60f;
    const float MaxInterval = 180f;

    void Awake() => Instance = this;

    void Start() => StartCoroutine(EventLoop());

    IEnumerator EventLoop()
    {
        // Initial delay before first event
        yield return new WaitForSeconds(Random.Range(45f, 90f));

        while (true)
        {
            var ev = PickWeighted();
            TriggerEvent(ev);
            yield return new WaitForSeconds(Random.Range(MinInterval, MaxInterval));
        }
    }

    /// <summary>Weighted pick — negative events become more likely as milestone progress advances.
    /// Sector-1 early game: ~30% threats. Post-M5: ~70% threats.</summary>
    GameEvent PickWeighted()
    {
        float progress = 0.3f;
        if (MilestoneManager.Instance != null && MilestoneManager.Instance.milestones.Count > 0)
            progress = Mathf.Clamp01(
                MilestoneManager.Instance.CurrentIndex /
                (float)MilestoneManager.Instance.milestones.Count);
        float threatWeight = Mathf.Lerp(0.3f, 0.7f, progress);

        // Compute weights and total
        float total = 0f;
        var weights = new float[_events.Length];
        for (int i = 0; i < _events.Length; i++)
        {
            float w = _events[i].isPositive ? (1f - threatWeight) : threatWeight;
            weights[i] = w;
            total += w;
        }

        float roll = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < _events.Length; i++)
        {
            acc += weights[i];
            if (roll <= acc) return _events[i];
        }
        return _events[_events.Length - 1];
    }

    void TriggerEvent(GameEvent ev)
    {
        switch (ev.kind)
        {
            case EffectKind.ResourceInstant:
                if (ev.amount > 0f)      ResourceManager.Instance.Add(ev.resourceType, ev.amount);
                else if (ev.amount < 0f) ResourceManager.Instance.Spend(ev.resourceType, -ev.amount);
                break;

            case EffectKind.RateDelta:
                if (ev.amount != 0f)
                {
                    if (ev.amount > 0f) ResourceManager.Instance.Add(ev.resourceType, ev.amount);
                    else                ResourceManager.Instance.Spend(ev.resourceType, -ev.amount);
                }
                if (!Mathf.Approximately(ev.rateDelta, 0f) && ev.duration > 0f)
                    StartCoroutine(TemporaryRateChange(ev.resourceType, ev.rateDelta, ev.duration));
                break;

            case EffectKind.BuildingStop:
                ApplyToRandomBuilding(ev.targetBuildingName,
                    b => StartCoroutine(b.ApplyEventStop(ev.duration)));
                break;

            case EffectKind.BuildingMultiplier:
                ApplyToRandomBuilding(ev.targetBuildingName,
                    b => StartCoroutine(b.ApplyEventMultiplier(ev.multiplier, ev.duration)));
                break;

            case EffectKind.DroneSpeed:
                StartCoroutine(DroneSpeedFor(ev.multiplier, ev.duration));
                break;

            case EffectKind.CapReduction:
                StartCoroutine(ResourceManager.Instance.ScaleMaxFor(
                    ev.resourceType, ev.multiplier, ev.duration));
                break;
        }

        // Toast + sound
        Color accent = ev.isPositive ? UIUtils.Green : UIUtils.Red;
        ToastUI toast = FindFirstObjectByType<ToastUI>();
        toast?.Enqueue(ev.title, ev.description, accent);
        SoundManager.Instance?.PlayEventAlert();

        Debug.Log($"[EventManager] {ev.title}: {ev.description}");
    }

    static void ApplyToRandomBuilding(string name, System.Action<Building> apply)
    {
        var matches = new List<Building>();
        foreach (var b in Building.All)
            if (b != null && b.data != null && b.data.buildingName == name) matches.Add(b);
        if (matches.Count == 0) return;
        apply(matches[Random.Range(0, matches.Count)]);
    }

    IEnumerator DroneSpeedFor(float mult, float duration)
    {
        DroneSpeedEventMult *= mult;
        yield return new WaitForSeconds(duration);
        DroneSpeedEventMult /= mult;
    }

    IEnumerator TemporaryRateChange(ResourceType type, float delta, float duration)
    {
        ResourceManager.Instance.AddRate(type, delta);
        yield return new WaitForSeconds(duration);
        ResourceManager.Instance.RemoveRate(type, delta);
    }

    /// <summary>Force-trigger a named event (for testing).</summary>
    public void TriggerByTitle(string title)
    {
        foreach (var ev in _events)
            if (ev.title == title) { TriggerEvent(ev); return; }
    }
}
