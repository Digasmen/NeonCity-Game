using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permanent global buffs purchased with Nano. One-time purchase per decree.
/// Effects are applied two ways:
///   • Static multipliers polled by hot code (DroneSpeedMultiplier).
///   • Rate/consumption modifiers read by Building.Initialize / RecomputeRate
///     via GetRateBonusFor / GetConsumptionMultFor.
/// Persisted via SaveData.ownedDecrees; re-applied silently on load.
/// </summary>
public class DecreeManager : MonoBehaviour
{
    public static DecreeManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<DecreeManager>() == null)
            new GameObject("_DecreeManager").AddComponent<DecreeManager>();
    }

    [Serializable]
    public struct Decree
    {
        public string id;
        public string name;
        public string description;
        public int    nanoCost;
    }

    public static readonly Decree[] All =
    {
        new Decree { id = "quantum_route", name = "Quantum Routing",
            description = "All drones move 25% faster.",                  nanoCost = 300  },
        new Decree { id = "stim_distrib",  name = "Stim Distribution",
            description = "Shelter produces 30% more Population.",        nanoCost = 250  },
        new Decree { id = "cold_sync",     name = "Cold Sync",
            description = "Charging Stations drain 50% less Energy.",     nanoCost = 400  },
        new Decree { id = "bio_recycle",   name = "Bio-Recycling",
            description = "Polymer Extractor +50% production rate.",      nanoCost = 500  },
        new Decree { id = "neural_net",    name = "Neural Net",
            description = "Data Tower +50% production rate.",             nanoCost = 700  },
        new Decree { id = "sector_beacon", name = "Sector Beacon",
            description = "Unlocks the Server Farm building.",            nanoCost = 1000 },
    };

    readonly HashSet<string> _owned = new();
    public IEnumerable<string> OwnedIds => _owned;
    public bool IsOwned(string id) => _owned.Contains(id);

    /// <summary>Static so hot Drone code (and others) can read without null-check.</summary>
    public static float DroneSpeedMultiplier { get; private set; } = 1f;

    public event Action<string> OnDecreePurchased;

    void Awake() => Instance = this;

    public bool CanAfford(Decree d) =>
        ResourceManager.Instance != null &&
        ResourceManager.Instance.CanAfford(ResourceType.Nano, d.nanoCost);

    public bool TryPurchase(string id)
    {
        if (_owned.Contains(id)) return false;
        if (!TryFind(id, out Decree d)) return false;
        if (!CanAfford(d)) return false;

        ResourceManager.Instance.Spend(ResourceType.Nano, d.nanoCost);
        _owned.Add(id);
        ApplyEffect(id, silent: false);
        OnDecreePurchased?.Invoke(id);
        return true;
    }

    /// <summary>Re-applies all owned decrees silently (no spend, no toast). Called on load.</summary>
    public void LoadFromSave(List<string> ids)
    {
        // Reset transient state
        DroneSpeedMultiplier = 1f;
        _owned.Clear();
        if (ids == null) return;
        foreach (var id in ids)
        {
            if (!TryFind(id, out _)) continue;
            _owned.Add(id);
            ApplyEffect(id, silent: true);
        }
    }

    static bool TryFind(string id, out Decree d)
    {
        foreach (var x in All)
            if (x.id == id) { d = x; return true; }
        d = default;
        return false;
    }

    // ── Effect application ───────────────────────────────────────────────

    void ApplyEffect(string id, bool silent)
    {
        switch (id)
        {
            case "quantum_route":
                DroneSpeedMultiplier = 1.25f;
                break;

            case "sector_beacon":
                if (SaveManager.Instance != null && BuildMenuUI.Instance != null)
                {
                    var farm = SaveManager.Instance.allBuildings
                        .Find(b => b != null && b.buildingName == "Server Farm");
                    if (farm != null) BuildMenuUI.Instance.UnlockBuilding(farm);
                }
                break;

            // The rest are passive multipliers — Building reads them on RecomputeRate().
            case "stim_distrib":
            case "cold_sync":
            case "bio_recycle":
            case "neural_net":
                break;
        }

        // Always refresh all building rates so passive multipliers take effect.
        Building.RecomputeAllRates();
    }

    // ── Building-side accessors ──────────────────────────────────────────

    /// <summary>Returns the cumulative production multiplier from all owned decrees
    /// that target this building name. Always >= 1.</summary>
    public static float GetRateBonusFor(string buildingName)
    {
        if (Instance == null) return 1f;
        float mult = 1f;
        var owned = Instance._owned;
        if (owned.Contains("stim_distrib") && buildingName == "Shelter")           mult *= 1.30f;
        if (owned.Contains("bio_recycle")  && buildingName == "Polymer Extractor") mult *= 1.50f;
        if (owned.Contains("neural_net")   && buildingName == "Data Tower")        mult *= 1.50f;
        return mult;
    }

    /// <summary>Returns the consumption multiplier (typically &lt; 1 for reductions).</summary>
    public static float GetConsumptionMultFor(string buildingName)
    {
        if (Instance == null) return 1f;
        if (Instance._owned.Contains("cold_sync") && buildingName == "Charging Station") return 0.5f;
        return 1f;
    }
}
