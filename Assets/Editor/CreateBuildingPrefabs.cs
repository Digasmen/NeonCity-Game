using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// NeonCity/Create Building Prefabs
///
/// For every labeled FBX model in Assets/_Game/Art/Models/:
///   1. Creates a lightweight wrapper prefab in Assets/_Game/Prefabs/Buildings/Meshes/
///   2. Finds the BuildingData asset whose meshVariants[] already references the raw FBX
///      and replaces that slot with the new prefab reference.
///
/// Run via: NeonCity → Create Building Prefabs (menu bar in Unity Editor)
/// Safe to re-run — skips FBX files whose wrapper prefab already exists.
/// </summary>
public static class CreateBuildingPrefabs
{
    const string ModelsFolder   = "Assets/_Game/Art/Models";
    const string PrefabsFolder  = "Assets/_Game/Prefabs/Buildings/Meshes";
    const string BuildingAssets = "Assets/_Game/ScriptableObjects/Buildings";

    // ── Table: FBX base-name → BuildingData asset name ───────────────────────
    // The script finds which meshVariants[] slot currently holds the FBX reference
    // and swaps it for the wrapper prefab automatically — no hardcoded indices.
    static readonly (string fbxName, string buildingAssetName)[] FbxToBuilding =
    {
        ("scrapcollector1", "Scrap Collector"),
        ("scrapcollector2", "Scrap Collector"),
        ("scrapcollector3", "Scrap Collector"),
        ("hospital1",       "Clinic"),
        ("hospital2",       "Clinic"),
        ("hospital3",       "Clinic"),
        ("shelter1",        "Shelter"),
        ("shelter2",        "Shelter"),
        ("shelter3",        "Shelter"),
        ("bioreactor1",     "Bioreactor"),
    };

    // ──────────────────────────────────────────────────────────────────────────

    [MenuItem("NeonCity/Create Building Prefabs")]
    public static void Run()
    {
        // Ensure output folder exists
        EnsureFolder(PrefabsFolder);

        int created  = 0;
        int skipped  = 0;
        int rewired  = 0;

        foreach (var (fbxName, buildingName) in FbxToBuilding)
        {
            string fbxPath    = $"{ModelsFolder}/{fbxName}.fbx";
            string prefabPath = $"{PrefabsFolder}/{fbxName}_mesh.prefab";

            // ── Load the FBX ──────────────────────────────────────────────
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"[CreateBuildingPrefabs] FBX not found — skipping: {fbxPath}");
                skipped++;
                continue;
            }

            // ── Create wrapper prefab (skip if already done) ───────────────
            GameObject wrapperPrefab;
            if (File.Exists(Path.Combine(Application.dataPath, "..", prefabPath)))
            {
                wrapperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                skipped++;
            }
            else
            {
                // Build an empty root → FBX child
                var root      = new GameObject(fbxName + "_mesh");
                var fbxChild  = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
                fbxChild.transform.localPosition = Vector3.zero;
                fbxChild.transform.localRotation = Quaternion.identity;

                wrapperPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                created++;
                Debug.Log($"[CreateBuildingPrefabs] Created: {prefabPath}");
            }

            if (wrapperPrefab == null) continue;

            // ── Rewire BuildingData slot ───────────────────────────────────
            string assetPath  = $"{BuildingAssets}/{buildingName}.asset";
            var    buildingData = AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
            if (buildingData == null)
            {
                Debug.LogWarning($"[CreateBuildingPrefabs] BuildingData not found: {assetPath}");
                continue;
            }

            if (buildingData.meshVariants == null) continue;

            bool slotUpdated = false;
            for (int i = 0; i < buildingData.meshVariants.Length; i++)
            {
                var slot = buildingData.meshVariants[i];
                if (slot == null) continue;

                // Match by comparing the asset path — handles both direct FBX refs and old prefab refs
                string slotPath = AssetDatabase.GetAssetPath(slot);
                string normSlot = slotPath.Replace('\\', '/').ToLowerInvariant();
                string normFbx  = fbxPath.Replace('\\', '/').ToLowerInvariant();

                if (normSlot == normFbx || normSlot.Contains(fbxName.ToLowerInvariant()))
                {
                    buildingData.meshVariants[i] = wrapperPrefab;
                    EditorUtility.SetDirty(buildingData);
                    rewired++;
                    slotUpdated = true;
                    Debug.Log($"[CreateBuildingPrefabs] Rewired {buildingName}.meshVariants[{i}] → {fbxName}_mesh");
                    break;
                }
            }

            if (!slotUpdated)
                Debug.LogWarning($"[CreateBuildingPrefabs] No matching slot found in {buildingName} for {fbxName}. " +
                                 "Assign it manually via the Inspector.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Create Building Prefabs",
            $"Done!\n\n" +
            $"  Prefabs created : {created}\n" +
            $"  Already existed : {skipped}\n" +
            $"  BuildingData slots rewired: {rewired}",
            "OK");
    }

    // ──────────────────────────────────────────────────────────────────────────

    static void EnsureFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        string parent = Path.GetDirectoryName(fullPath)?.Replace('\\', '/') ?? "Assets";
        string child  = Path.GetFileName(fullPath);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, child);
    }
}
