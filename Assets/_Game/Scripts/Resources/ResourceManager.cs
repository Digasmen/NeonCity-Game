using UnityEngine;
using System.Collections.Generic;

public enum ResourceType { Scrap, Energy, Polymer, Data, Population }

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

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
        new Resource { type = ResourceType.Population, amount = 0,   maxAmount = 300  }
    };

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (var r in resources)
        {
            if (r.ratePerMinute == 0) continue;
            r.amount += r.ratePerMinute / 60f * Time.deltaTime;
            r.amount = Mathf.Clamp(r.amount, 0, r.maxAmount);
        }
    }

    public float Get(ResourceType type)
    {
        return GetResource(type)?.amount ?? 0;
    }

    public float GetRate(ResourceType type)
    {
        return GetResource(type)?.ratePerMinute ?? 0;
    }

    public bool CanAfford(ResourceType type, float amount)
    {
        return Get(type) >= amount;
    }

    public bool Spend(ResourceType type, float amount)
    {
        var r = GetResource(type);
        if (r == null || r.amount < amount) return false;
        r.amount -= amount;
        return true;
    }

    public void Add(ResourceType type, float amount)
    {
        var r = GetResource(type);
        if (r == null) return;
        r.amount = Mathf.Clamp(r.amount + amount, 0, r.maxAmount);
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

    Resource GetResource(ResourceType type)
    {
        return resources.Find(r => r.type == type);
    }
}
