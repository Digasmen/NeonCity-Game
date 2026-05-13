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
            _meshChild = SpawnMeshChild(data.meshVariants[varIdx], TextureForVariant(varIdx));
            EnsureRootCollider();
        }

        if (data.buildingName == "Charging Station")
            gameObject.AddComponent<ChargingStation>();
        if (data.droneData != null) SpawnDrone();

        // Production rate
        if (data.passiveRatePerMinute > 0f)
        {
            _activeRate = data.passiveRatePerMinute * GetMultiplier(1);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
        }

        // Consumption rate
        if (data.consumptionRatePerMinute > 0f)
        {
            _consumptionRate = data.consumptionRatePerMinute;
            ResourceManager.Instance.AddRate(data.consumptionType, -_consumptionRate);
        }

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
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
            _activeRate = data.passiveRatePerMinute * GetMultiplier(level + 1);
            // Preserve adjacency multiplier
            _activeRate = ApplyAdjacencyMultiplier(_activeRate);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
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
            ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);

        level = target;

        if (data.passiveRatePerMinute > 0f)
        {
            _activeRate = data.passiveRatePerMinute * GetMultiplier(level);
            _activeRate = ApplyAdjacencyMultiplier(_activeRate);
            ResourceManager.Instance.AddRate(data.passiveResourceType, _activeRate);
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

        float baseRate = data.passiveRatePerMinute * GetMultiplier(level);
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
                    ? SpawnMeshChild(data.meshVariants[targetIdx], TextureForVariant(targetIdx))
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

    GameObject SpawnMeshChild(GameObject prefab, Texture2D texture = null)
    {
        var child = Instantiate(prefab, transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        foreach (var col in child.GetComponentsInChildren<Collider>())
            Destroy(col);
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
                ResourceManager.Instance.RemoveRate(data.passiveResourceType, _activeRate);
            if (_consumptionRate > 0f)
                ResourceManager.Instance.RemoveRate(data.consumptionType, -_consumptionRate);
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
