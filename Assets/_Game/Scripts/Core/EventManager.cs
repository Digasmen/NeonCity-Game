using System.Collections;
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

    [System.Serializable]
    struct GameEvent
    {
        public string       title;
        public string       description;
        public bool         isPositive;
        public ResourceType resourceType;
        public float        amount;          // immediate bonus / penalty
        public float        rateDelta;       // temporary rate change per minute
        public float        duration;        // seconds (0 = instant only)
    }

    readonly GameEvent[] _events =
    {
        new GameEvent { title = "SALVAGE CACHE",      description = "Scouts found a cache of materials!",
            isPositive = true,  resourceType = ResourceType.Scrap,   amount = 150f, rateDelta = 0f,   duration = 0f },

        new GameEvent { title = "ENERGY SURGE",       description = "Grid output spiked — capacitors charged.",
            isPositive = true,  resourceType = ResourceType.Energy,  amount = 80f,  rateDelta = 0f,   duration = 0f },

        new GameEvent { title = "POLYMER DEPOSIT",    description = "Survey drones located a polymer seam.",
            isPositive = true,  resourceType = ResourceType.Polymer, amount = 60f,  rateDelta = 5f,   duration = 90f },

        new GameEvent { title = "SYSTEM GLITCH",      description = "Electrical interference disrupts production.",
            isPositive = false, resourceType = ResourceType.Energy,  amount = 0f,   rateDelta = -15f, duration = 45f },

        new GameEvent { title = "DATA BREACH",        description = "Intrusion detected — data rerouted.",
            isPositive = false, resourceType = ResourceType.Data,    amount = -40f, rateDelta = -8f,  duration = 30f },

        new GameEvent { title = "POPULATION INFLUX",  description = "Refugees arrive seeking shelter.",
            isPositive = true,  resourceType = ResourceType.Population, amount = 20f, rateDelta = 3f, duration = 120f },

        new GameEvent { title = "SCRAP STORM",        description = "Corrosive weather damages collection gear.",
            isPositive = false, resourceType = ResourceType.Scrap,   amount = 0f,   rateDelta = -20f, duration = 40f },

        new GameEvent { title = "NANO RECOVERY",      description = "Nano-assembler cache discovered in ruins.",
            isPositive = true,  resourceType = ResourceType.Nano,    amount = 35f,  rateDelta = 0f,   duration = 0f },
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
            var ev = _events[Random.Range(0, _events.Length)];
            TriggerEvent(ev);
            yield return new WaitForSeconds(Random.Range(MinInterval, MaxInterval));
        }
    }

    void TriggerEvent(GameEvent ev)
    {
        // Immediate resource change
        if (ev.amount > 0f)
            ResourceManager.Instance.Add(ev.resourceType, ev.amount);
        else if (ev.amount < 0f)
            ResourceManager.Instance.Spend(ev.resourceType, -ev.amount);

        // Temporary rate change
        if (!Mathf.Approximately(ev.rateDelta, 0f) && ev.duration > 0f)
            StartCoroutine(TemporaryRateChange(ev.resourceType, ev.rateDelta, ev.duration));

        // Toast notification
        Color accent = ev.isPositive ? UIUtils.Green : UIUtils.Red;
        ToastUI toast = FindFirstObjectByType<ToastUI>();
        toast?.Enqueue(ev.title, ev.description, accent);

        // Sound
        SoundManager.Instance?.PlayEventAlert();

        Debug.Log($"[EventManager] {ev.title}: {ev.description}");
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
