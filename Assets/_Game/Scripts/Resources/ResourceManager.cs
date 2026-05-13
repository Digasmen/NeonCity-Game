using UnityEngine;
using System.Collections.Generic;

public enum ResourceType { Scrap, Energy, Polymer, Data, Population, Nano }

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    /// <summary>Fired when a drone delivers resources. Supplies type, amount, world-space position
    /// so FloatingText can spawn at the right place.</summary>
    public static event System.Action<ResourceType, float, Vector3> OnResourceCollected;

    [System.Serializable]
    public class Resource
    {
        public ResourceType type;
        public float amount;
        public float maxAmount = 1000f;
        public float ratePerMinute;
    }

    public List<Resource> resources = new List<Resource>
    {
        new Resource { type = ResourceType.Scrap,      amount = 200, maxAmount = 2000 },
        new Resource { type = ResourceType.Energy,     amount = 100, maxAmount = 1000 },
        new Resource { type = ResourceType.Polymer,    amount = 0,   maxAmount = 500  },
        new Resource { type = ResourceType.Data,       amount = 0,   maxAmount = 500  },
        new Resource { type = ResourceType.Population, amount = 0,   maxAmount = 300  },
        new Resource { type = ResourceType.Nano,       amount = 0,   maxAmount = 1000 }
    };

    // ── History ring buffer (1 sample/s, 60 s window) ─────────────────────
    public const int HistoryLen = 60;
    readonly Dictionary<ResourceType, float[]> _history  = new();
    readonly Dictionary<ResourceType, int>     _histHead = new();
    float _histTimer;

    void Awake()
    {
        Instance = this;

        // ── Self-heal: ensure every ResourceType enum value has an entry. ──────
        // The scene file is serialized with whatever resources existed when it
        // was saved.  If a new ResourceType is added to the enum later, scene
        // copies will be missing it and Collect() will silently no-op.
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (resources.Find(r => r.type == type) == null)
            {
                float defaultMax = DefaultMaxFor(type);
                resources.Add(new Resource { type = type, amount = 0f, maxAmount = defaultMax });
                Debug.Log($"[ResourceManager] Added missing resource entry for {type} (max {defaultMax}).");
            }
        }

        foreach (var r in resources)
        {
            _history[r.type]  = new float[HistoryLen];
            _histHead[r.type] = 0;
        }
    }

    static float DefaultMaxFor(ResourceType type) => type switch
    {
        ResourceType.Scrap      => 2000f,
        ResourceType.Energy     => 1000f,
        ResourceType.Polymer    => 500f,
        ResourceType.Data       => 500f,
        ResourceType.Population => 300f,
        ResourceType.Nano       => 1000f,
        _                       => 500f,
    };

    void Update()
    {
        foreach (var r in resources)
        {
            if (r.ratePerMinute == 0) continue;
            r.amount += r.ratePerMinute / 60f * Time.deltaTime;
            r.amount  = Mathf.Clamp(r.amount, 0f, r.maxAmount);
        }

        _histTimer += Time.deltaTime;
        if (_histTimer >= 1f)
        {
            _histTimer = 0f;
            foreach (var r in resources)
            {
                int h = _histHead[r.type];
                _history[r.type][h] = r.amount;
                _histHead[r.type]   = (h + 1) % HistoryLen;
            }
        }
    }

    // ── Queries ────────────────────────────────────────────────────────────

    public float Get(ResourceType type)     => GetResource(type)?.amount      ?? 0f;
    public float GetRate(ResourceType type)  => GetResource(type)?.ratePerMinute ?? 0f;
    public float GetMax(ResourceType type)   => GetResource(type)?.maxAmount   ?? 0f;

    public bool CanAfford(ResourceType type, float amount) => Get(type) >= amount;

    /// <summary>Returns 60 history samples in chronological order (oldest → newest).</summary>
    public float[] GetHistory(ResourceType type)
    {
        if (!_history.TryGetValue(type, out var buf)) return new float[HistoryLen];
        int head   = _histHead[type];
        var result = new float[HistoryLen];
        for (int i = 0; i < HistoryLen; i++)
            result[i] = buf[(head + i) % HistoryLen];
        return result;
    }

    // ── Mutators ───────────────────────────────────────────────────────────

    public bool Spend(ResourceType type, float amount)
    {
        var r = GetResource(type);
        if (r == null || r.amount < amount) return false;
        r.amount -= amount;
        return true;
    }

    public void SetAmount(ResourceType type, float amount)
    {
        var r = GetResource(type);
        if (r != null) r.amount = Mathf.Clamp(amount, 0f, r.maxAmount);
    }

    /// <summary>Silent add — passive rate ticks, save/load. Does NOT fire floating text.</summary>
    public void Add(ResourceType type, float amount)
    {
        var r = GetResource(type);
        if (r == null) return;
        r.amount = Mathf.Clamp(r.amount + amount, 0f, r.maxAmount);
    }

    /// <summary>Drone deposit — adds amount and fires OnResourceCollected for floating text.</summary>
    public void Collect(ResourceType type, float amount, Vector3 worldPos)
    {
        var r = GetResource(type);
        if (r == null) return;
        float prev = r.amount;
        r.amount = Mathf.Clamp(r.amount + amount, 0f, r.maxAmount);
        float actual = r.amount - prev;
        if (actual > 0.01f)
            OnResourceCollected?.Invoke(type, actual, worldPos);
    }

    public void AddRate(ResourceType type, float ratePerMinute)
    {
        var r = GetResource(type);
        if (r != null) r.ratePerMinute += ratePerMinute;
    }

    public void RemoveRate(ResourceType type, float ratePerMinute)
    {
        var r = GetResource(type);
        if (r != null) r.ratePerMinute -= ratePerMinute;
    }

    Resource GetResource(ResourceType type) => resources.Find(r => r.type == type);
}
