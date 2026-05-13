using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingData))]
public class BuildingDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var data = (BuildingData)target;

        int meshCount  = data.meshVariants     != null ? data.meshVariants.Length     : 0;
        int levelCount = data.meshVariantLevels != null ? data.meshVariantLevels.Length : 0;
        int texCount   = data.textureVariants   != null ? data.textureVariants.Length   : 0;

        // Nothing to validate for procedural buildings
        if (meshCount == 0) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Variant Validation", EditorStyles.boldLabel);

        // Level threshold mismatch — silently defaults to 1, almost never intentional
        if (levelCount != meshCount)
            EditorGUILayout.HelpBox(
                $"meshVariantLevels has {levelCount} entr{(levelCount == 1 ? "y" : "ies")} " +
                $"but meshVariants has {meshCount}. " +
                "Missing threshold entries default to level 1 — variants may activate unexpectedly.",
                MessageType.Warning);

        // Texture mismatch — missing slots just keep the FBX's own material, which can be intentional
        if (texCount > 0 && texCount != meshCount)
            EditorGUILayout.HelpBox(
                $"textureVariants has {texCount} entr{(texCount == 1 ? "y" : "ies")} " +
                $"but meshVariants has {meshCount}. " +
                "Extra texture slots are ignored; missing slots keep the FBX's own materials.",
                MessageType.Info);

        // Per-level activation table — uses the same runtime logic as GetVariantIndexForLevel
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Active mesh per level:", EditorStyles.miniBoldLabel);

        for (int lvl = 1; lvl <= data.maxLevel; lvl++)
        {
            int idx = data.GetVariantIndexForLevel(lvl);
            string meshName = "(none — procedural)";
            if (idx >= 0)
                meshName = (data.meshVariants[idx] != null)
                    ? data.meshVariants[idx].name
                    : "(slot is empty)";

            EditorGUILayout.LabelField($"  Level {lvl}", meshName, EditorStyles.miniLabel);
        }
    }
}
