using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "NeonCity/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;

    [Header("Appearance")]
    [Tooltip("Accent/glow color used by UI labels and popups. For procedural buildings this is overridden by ProceduralBuilding.GlowColor at runtime.")]
    public Color glowColor = Color.cyan;

    [Header("Mesh Variants")]
    [Tooltip("FBX prefabs for this building. meshVariantLevels[i] = min level to activate meshVariants[i]. Leave empty to use procedural geometry.")]
    public GameObject[] meshVariants;
    [Tooltip("Min level required to activate the corresponding mesh variant (same length as meshVariants). e.g. {1, 3, 5}")]
    public int[] meshVariantLevels;
    [Tooltip("Optional albedo texture override per variant (same index as meshVariants). Leave a slot null to keep the FBX's own material.")]
    public Texture2D[] textureVariants;

    /// <summary>Returns the FBX prefab whose level threshold is the highest one still <= level,
    /// or null if no variants are configured (falls back to procedural).</summary>
    public GameObject GetMeshForLevel(int level)
    {
        if (meshVariants == null || meshVariants.Length == 0) return null;
        GameObject best = null;
        int bestThreshold = 0;
        for (int i = 0; i < meshVariants.Length; i++)
        {
            int threshold = (meshVariantLevels != null && i < meshVariantLevels.Length)
                ? meshVariantLevels[i] : 1;
            if (level >= threshold && threshold >= bestThreshold)
            {
                best = meshVariants[i];
                bestThreshold = threshold;
            }
        }
        return best;
    }

    /// <summary>Returns the index of the active variant for the given level, or -1 if none.</summary>
    public int GetVariantIndexForLevel(int level)
    {
        if (meshVariants == null || meshVariants.Length == 0) return -1;
        int bestIdx = -1;
        int bestThreshold = 0;
        for (int i = 0; i < meshVariants.Length; i++)
        {
            int threshold = (meshVariantLevels != null && i < meshVariantLevels.Length)
                ? meshVariantLevels[i] : 1;
            if (level >= threshold && threshold >= bestThreshold)
            {
                bestIdx = i;
                bestThreshold = threshold;
            }
        }
        return bestIdx;
    }

    public int width  = 1;
    public int height = 1;
    public int scrapCost = 100;
    public Sprite icon;
    public DroneData droneData;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Unlock")]
    public bool lockedAtStart = false;   // hidden until a milestone calls UnlockBuilding()

    [Header("Requirements")]
    public int populationRequired = 0;

    [Header("Passive Production")]
    public ResourceType passiveResourceType;
    public float passiveRatePerMinute = 0f;

    [Header("Secondary Passive (optional)")]
    [Tooltip("Some late-game buildings (e.g. Bioreactor) produce two resources at once. Leave rate at 0 to disable.")]
    public ResourceType secondaryPassiveType;
    public float secondaryPassivePerMinute = 0f;

    [Header("Resource Consumption")]
    [Tooltip("Resource this building drains over time (e.g. Energy for a Data Tower).")]
    public ResourceType consumptionType;
    [Min(0f)] public float consumptionRatePerMinute = 0f;

    [Header("Adjacency Bonus")]
    [Tooltip("Name of a neighboring building that boosts this building's production.")]
    public string adjacencyBuildingType = "";
    [Range(1f, 3f)] public float adjacencyBonus = 1.5f;

    [Header("Upgrades")]
    public int maxLevel = 5;
    public int[]   upgradeCostScrap = { 150, 300, 200, 400 };
    public int[]   upgradeCostNano  = {   0,   0,  50, 120 };
    public float[] rateMultipliers  = { 1f, 1.6f, 2.5f, 3.5f, 5f };
}
