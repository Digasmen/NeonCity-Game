using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    // ── Global registry (replaces FindObjectsByType<Building> calls) ──────
    static readonly List<Building> _all = new();
    public static IReadOnlyList<Building> All => _all;
    public static int Count => _all.Count;
    void OnEnable()  => _all.Add(this);
    void OnDisable() => _all.Remove(this);

    public BuildingData data;
    public Vector2Int   gridCell;
    public int          level = 1;

    private GameObject spawnedDrone;
    private GameObject _meshChild;           // currently active FBX mesh child (null when procedural)
    private int        _activeMeshVariant = -1; // index into data.meshVariants; -1 = procedural
    private float      _activeRate;          // current production rate (includes adjacency bonus)
    private float      _consumptionRate;     // current consumption debit (always positive)
    private float      _secondaryRate;       // secondary passive production (Bioreactor etc.; no adjacency)

    /// <summary>The point light that illuminates this building's surroundings.
    /// Spawned once in Initialize(); BuildingGlow pulses its intensity.</summary>
    public Light BuildingLight { get; private set; }

    /// <summary>Live production multiplier vs the level-only base rate.
    /// Returns 1.0 when there's no passive production, no adjacency target, or no matching neighbors.</summary>
    public float CurrentMultiplier
    {
        get
        {
            if (data == null || _activeRate <= 0f || data.passiveRatePerMinute <= 0f) return 1f;
            float baseRate = data.passiveRatePerMinute * GetMultiplier(level);
            return baseRate > 0f ? _activeRate / baseRate : 1f;
        }
    }

    public void Initialize(BuildingData buildingData)
    {
        data = buildingData;

        // Spawn the appropriate mesh for level 1. If no FBX variants are configured,
        // fall back to procedural geometry. The FBX is always a child so it can be
        // swapped later when the building crosses a level threshold.
        int varIdx = data.GetVariantIndexForLevel(1);
        if (varIdx < 0)
        {
            GetComponent<ProceduralBuilding>()?.Build(data.buildingName);
        }
        else
        {
            var pb = GetComponent<ProceduralBuilding>();
            if (pb != null) Destroy(pb);
            _activeMeshVariant = varIdx;
            _meshChild = SpawnMeshChild(data.meshVariants[varIdx], TextureForVariant(varIdx), ScaleMultiplierForVariant(varIdx));
            EnsureRootCollider();
        }

        if (data.buildingName == "Charging Station")
            gameObject.AddComponent<ChargingStation>();
        if (data.droneData != null) SpawnDrone();

        // Production rate (decree bonus baked in before adjacency on first RefreshAdjacency)
        if (data.passiveRatePerMinute > 0f)
        {
            _activeRate = data.passiveRatePerMinute * GetMultiplier(1)
                        * DecreeManager.GetRateBonusFor(data.buildingName);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate, this);
        }

        // Consumption rate (decree may reduce e.g. Charging Station cold_sync)
        if (data.consumptionRatePerMinute > 0f)
        {
            _consumptionRate = data.consumptionRatePerMinute
                             * DecreeManager.GetConsumptionMultFor(data.buildingName);
            ResourceManager.Instance.AddRate(data.consumptionType, -_consumptionRate, this);
        }

        // Secondary passive (Bioreactor etc.) — flat, no adjacency, no decree mod
        if (data.secondaryPassivePerMinute > 0f)
        {
            _secondaryRate = data.secondaryPassivePerMinute;
            ResourceManager.Instance.AddRate(data.secondaryPassiveType, _secondaryRate, this);
        }

        SpawnBuildingLight();
        gameObject.AddComponent<BuildingGlow>();
        var label = gameObject.AddComponent<BuildingLabel>();
        label.Setup(this);
    }

    // ── Upgrade ───────────────────────────────────────────────────────────

    public bool CanUpgrade()
    {
        if (level >= data.maxLevel) return false;
        int scrap = UpgradeScrapCost();
        int nano  = UpgradeNanoCost();
        return ResourceManager.Instance.CanAfford(ResourceType.Scrap, scrap) &&
               (nano == 0 || ResourceManager.Instance.CanAfford(ResourceType.Nano, nano));
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;

        ResourceManager.Instance.Spend(ResourceType.Scrap, UpgradeScrapCost());
        int nano = UpgradeNanoCost();
        if (nano > 0) ResourceManager.Instance.Spend(ResourceType.Nano, nano);

        if (_activeRate > 0f)
        {
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate, this);
            _activeRate = data.passiveRatePerMinute * GetMultiplier(level + 1)
                        * DecreeManager.GetRateBonusFor(data.buildingName);
            // Preserve adjacency multiplier
            _activeRate = ApplyAdjacencyMultiplier(_activeRate);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate, this);
        }

        level++;
        RefreshVisuals();
    }

    /// <summary>Used on save/load — bypasses resource cost.</summary>
    public void SetLevel(int target)
    {
        target = Mathf.Clamp(target, 1, data.maxLevel);
        if (target == level) return;

        if (_activeRate > 0f)
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate, this);

        level = target;

        if (data.passiveRatePerMinute > 0f)
        {
            _activeRate = data.passiveRatePerMinute * GetMultiplier(level)
                        * DecreeManager.GetRateBonusFor(data.buildingName);
            _activeRate = ApplyAdjacencyMultiplier(_activeRate);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate, this);
        }

        RefreshVisuals();
    }

    // ── Adjacency ─────────────────────────────────────────────────────────

    /// <summary>Called by BuildingPlacer after gridCell is set and building is registered.
    /// Also called on neighbors when a new building lands nearby.</summary>
    public void RefreshAdjacency()
    {
        if (data == null || _activeRate <= 0f ||
            string.IsNullOrEmpty(data.adjacencyBuildingType)) return;

        float baseRate = data.passiveRatePerMinute * GetMultiplier(level)
                       * DecreeManager.GetRateBonusFor(data.buildingName);
        float newRate  = ApplyAdjacencyMultiplier(baseRate);

        if (!Mathf.Approximately(newRate, _activeRate))
        {
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
            _activeRate = newRate;
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
        }
    }

    float ApplyAdjacencyMultiplier(float rate)
    {
        if (string.IsNullOrEmpty(data.adjacencyBuildingType)) return rate;
        int matches = 0;
        foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var nb = GridManager.Instance.GetBuildingAt(gridCell + d);
            if (nb != null && nb.data != null && nb.data.buildingName == data.adjacencyBuildingType)
                matches++;
        }
        if (matches == 0) return rate;
        // Each matching neighbor adds half the bonus, capped at 2 neighbors (full bonus)
        float mult = Mathf.Lerp(1f, data.adjacencyBonus, Mathf.Min(matches, 2) * 0.5f);
        return rate * mult;
    }

    // ── Decree integration ────────────────────────────────────────────────

    /// <summary>Re-derive _activeRate and _consumptionRate from current decree state.
    /// Called after a decree is purchased so existing buildings update without re-instantiation.</summary>
    public void RecomputeRate()
    {
        if (data == null) return;

        // Passive production
        if (data.passiveRatePerMinute > 0f)
        {
            float baseRate = data.passiveRatePerMinute * GetMultiplier(level)
                           * DecreeManager.GetRateBonusFor(data.buildingName);
            float newRate  = ApplyAdjacencyMultiplier(baseRate);
            if (!Mathf.Approximately(newRate, _activeRate))
            {
                ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
                _activeRate = newRate;
                ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
            }
        }

        // Consumption
        if (data.consumptionRatePerMinute > 0f)
        {
            float newCons = data.consumptionRatePerMinute
                          * DecreeManager.GetConsumptionMultFor(data.buildingName);
            if (!Mathf.Approximately(newCons, _consumptionRate))
            {
                ResourceManager.Instance.RemoveRate(data.consumptionType, -_consumptionRate);
                _consumptionRate = newCons;
                ResourceManager.Instance.AddRate(data.consumptionType, -_consumptionRate);
            }
        }
    }

    /// <summary>Walks every active Building and re-derives its rates. Cheap; called by DecreeManager on purchase/load.</summary>
    public static void RecomputeAllRates()
    {
        foreach (var b in _all)
            b?.RecomputeRate();
    }

    // ── Event-driven temporary modifiers ──────────────────────────────────

    /// <summary>Brownout-style: zero out production for `duration`, then restore. Idempotent — safe to fire
    /// while already stopped (just extends duration).</summary>
    public IEnumerator ApplyEventStop(float duration)
    {
        if (_activeRate <= 0f) yield break;
        float saved = _activeRate;
        ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
        _activeRate = 0f;
        yield return new WaitForSeconds(duration);
        if (this == null || data == null) yield break;
        _activeRate = saved;
        ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
    }

    /// <summary>Multiplies current rate by `mult` for `duration`, then restores.</summary>
    public IEnumerator ApplyEventMultiplier(float mult, float duration)
    {
        if (_activeRate <= 0f) yield break;
        float saved = _activeRate;
        ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
        _activeRate = saved * mult;
        ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
        yield return new WaitForSeconds(duration);
        if (this == null || data == null) yield break;
        ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
        _activeRate = saved;
        ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
    }

    // ── Upgrade helpers ───────────────────────────────────────────────────

    public int UpgradeScrapCost()
    {
        int idx = level - 1;
        if (data.upgradeCostScrap != null && idx < data.upgradeCostScrap.Length)
            return data.upgradeCostScrap[idx];
        return idx switch { 0 => 150, 1 => 300, 2 => 200, 3 => 400, _ => 500 };
    }

    public int UpgradeNanoCost()
    {
        int idx = level - 1;
        if (data.upgradeCostNano != null && idx < data.upgradeCostNano.Length)
            return data.upgradeCostNano[idx];
        return idx switch { 2 => 50, 3 => 120, _ => 0 };
    }

    float GetMultiplier(int lvl)
    {
        int idx = lvl - 1;
        if (data.rateMultipliers != null && idx < data.rateMultipliers.Length)
            return data.rateMultipliers[idx];
        return lvl switch { 1 => 1f, 2 => 1.6f, 3 => 2.5f, 4 => 3.5f, 5 => 5f, _ => 5f };
    }

    void RefreshVisuals()
    {
        if (data.meshVariants != null && data.meshVariants.Length > 0)
        {
            int targetIdx = data.GetVariantIndexForLevel(level);
            if (targetIdx != _activeMeshVariant)
            {
                if (_meshChild != null) Destroy(_meshChild);
                _meshChild = targetIdx >= 0
                    ? SpawnMeshChild(data.meshVariants[targetIdx], TextureForVariant(targetIdx), ScaleMultiplierForVariant(targetIdx))
                    : null;
                _activeMeshVariant = targetIdx;
            }
        }
        else
        {
            GetComponent<ProceduralBuilding>()?.UpgradeVisual(level);
        }
        var glow = GetComponent<BuildingGlow>();
        if (glow != null) { Destroy(glow); gameObject.AddComponent<BuildingGlow>(); }
        GetComponent<BuildingLabel>()?.Refresh();
    }

    Texture2D TextureForVariant(int idx) =>
        data.textureVariants != null && idx < data.textureVariants.Length
            ? data.textureVariants[idx] : null;

    // ── Helper: resolve per-variant scale multiplier from BuildingData ────────
    float ScaleMultiplierForVariant(int varIdx)
    {
        if (data.meshScaleMultipliers != null && varIdx < data.meshScaleMultipliers.Length)
            return Mathf.Max(0.05f, data.meshScaleMultipliers[varIdx]);
        return 1f;
    }

    GameObject SpawnMeshChild(GameObject prefab, Texture2D texture = null, float scaleMultiplier = 1f)
    {
        var child = Instantiate(prefab, transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        foreach (var col in child.GetComponentsInChildren<Collider>())
            Destroy(col);

        // Auto-scale to fit the grid cell footprint regardless of the FBX's export scale.
        // Set localScale=1 first so bounds are measured at a consistent reference scale,
        // then apply the cell-fit factor and any per-variant override multiplier.
        var renderers = child.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            child.transform.localScale = Vector3.one;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint > 0.001f)
                child.transform.localScale = Vector3.one *
                    (GridManager.Instance.cellSize * 0.9f / footprint) * scaleMultiplier;
        }

        if (texture != null)
        {
            foreach (var r in child.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)  // .materials creates per-instance clones
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", texture);
        }
        return child;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void OnDestroy()
    {
        if (data != null)
        {
            if (_activeRate > 0f)
                ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate, this);
            if (_consumptionRate > 0f)
                ResourceManager.Instance.RemoveRate(data.consumptionType, -_consumptionRate, this);
            if (_secondaryRate > 0f)
                ResourceManager.Instance.RemoveRate(data.secondaryPassiveType, _secondaryRate, this);
        }
        if (spawnedDrone != null) Destroy(spawnedDrone);
        GridManager.Instance?.UnregisterBuilding(gridCell);
    }

    // ── Drone spawning ────────────────────────────────────────────────────

    void SpawnDrone()
    {
        ResourceNode target = FindNearestNode(data.droneData.resourceType);
        if (target == null)
        {
            Debug.LogWarning($"[{data.buildingName}] No ResourceNode of type " +
                $"{data.droneData.resourceType} in scene — will retry every 3 s.");
            StartCoroutine(RetrySpawnDrone());
            return;
        }
        DeployDroneTo(target);
    }

    IEnumerator RetrySpawnDrone()
    {
        while (spawnedDrone == null)
        {
            yield return new WaitForSeconds(3f);
            if (data == null) yield break;
            ResourceNode target = FindNearestNode(data.droneData.resourceType);
            if (target != null)
            {
                Debug.Log($"[{data.buildingName}] ResourceNode found — deploying drone.");
                DeployDroneTo(target);
            }
        }
    }

    void DeployDroneTo(ResourceNode target)
    {
        GameObject droneObj = data.droneData.prefab != null
            ? Instantiate(data.droneData.prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // ── Sanitize the prefab — third-party FBX models often ship with components
        // that fight our manual transform.position writes. Strip them defensively. ──
        SanitizeDronePrefab(droneObj);

        droneObj.transform.position   = transform.position + Vector3.up * 0.5f;
        droneObj.transform.localScale = Vector3.one * 0.3f;
        droneObj.name = "Drone_" + data.buildingName;
        spawnedDrone  = droneObj;

        Debug.Log($"[{data.buildingName}] Spawned drone — target: {target.name} " +
                  $"({target.resourceType}) at {target.transform.position}, " +
                  $"home at {droneObj.transform.position}");

        Drone drone = droneObj.AddComponent<Drone>();
        drone.data  = data.droneData;

        DroneVisuals visuals = droneObj.AddComponent<DroneVisuals>();
        visuals.droneColor   = data.droneData.droneColor;

        GameObject homeGO   = new GameObject("HomePoint");
        homeGO.transform.position = transform.position + Vector3.up * 0.5f;
        homeGO.transform.SetParent(transform);

        GameObject targetGO = new GameObject("TargetPoint");
        targetGO.transform.position = target.transform.position;
        targetGO.transform.SetParent(target.transform);

        drone.homePoint   = homeGO.transform;
        drone.targetPoint = targetGO.transform;
    }

    /// <summary>Ensures the building's root has a single tap-friendly BoxCollider
    /// (so <see cref="BuildingPlacer"/>'s raycast can hit it).  Used for real-prefab
    /// buildings — ProceduralBuilding adds its own collider in the procedural path.</summary>
    void EnsureRootCollider()
    {
        if (GetComponent<Collider>() != null) return;
        var box = gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.6f, 0f);
        box.size   = new Vector3(0.9f, 1.2f, 0.9f);
    }

    /// <summary>Removes / disables components that would fight against our manual
    /// transform.position writes in <see cref="Drone.MoveTowards"/>:
    ///   • Animator (root-motion or idle anims override position)
    ///   • Rigidbody (physics integrates velocity, conflicts with kinematic moves)
    ///   • Colliders on children (drone has no collision interactions)
    /// Called on every spawned drone, regardless of source prefab.</summary>
    static void SanitizeDronePrefab(GameObject droneObj)
    {
        // Animator — most common offender on third-party FBX rigs
        foreach (var a in droneObj.GetComponentsInChildren<Animator>(true))
            a.enabled = false;

        // Rigidbody — physics would override transform writes
        foreach (var rb in droneObj.GetComponentsInChildren<Rigidbody>(true))
            Destroy(rb);

        // Colliders — drones shouldn't trigger physics or block placement raycasts
        foreach (var col in droneObj.GetComponentsInChildren<Collider>(true))
            Destroy(col);
    }

    // ── Building light ─────────────────────────────────────────────────────────

    void SpawnBuildingLight()
    {
        var lightGO = new GameObject("GlowLight");
        lightGO.transform.SetParent(transform);
        lightGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);

        var l = lightGO.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = data != null ? data.glowColor : Color.cyan;
        l.intensity = 0f;           // BuildingGlow drives this; starts at zero
        l.range     = 5f;
        l.shadows   = LightShadows.None;
        l.renderMode = LightRenderMode.Auto;

        BuildingLight = l;
    }

    ResourceNode FindNearestNode(ResourceType type)
    {
        ResourceNode[] nodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        ResourceNode nearest = null;
        float minDist = float.MaxValue;
        foreach (var node in nodes)
        {
            if (node.resourceType != type) continue;
            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist < minDist) { minDist = dist; nearest = node; }
        }
        return nearest;
    }
}
