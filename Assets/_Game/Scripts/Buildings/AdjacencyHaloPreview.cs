using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placement-time visual: shows a thin world-space ring around every existing
/// building that would synergize with the building currently being placed.
/// Either direction of the adjacency pair counts — both "new gets bonus from
/// existing" and "existing gets bonus from new" trigger a halo.
/// Owned by BuildingPlacer: created in StartPlacement, destroyed on Cancel/TryPlace.
/// </summary>
public class AdjacencyHaloPreview : MonoBehaviour
{
    const int CircleSegments = 32;
    const float Radius       = 0.55f;
    const float LineWidth    = 0.04f;
    const float PulseSpeed   = 3.5f;     // rad/s

    readonly List<LineRenderer> _halos = new();
    float _pulseT;
    Material _sharedMat;

    public void ShowFor(BuildingData newData)
    {
        Clear();
        if (newData == null) return;

        foreach (var b in Building.All)
        {
            if (b == null || b.data == null) continue;
            bool bAffectsNew = !string.IsNullOrEmpty(newData.adjacencyBuildingType)
                            && b.data.buildingName == newData.adjacencyBuildingType;
            bool newAffectsB = !string.IsNullOrEmpty(b.data.adjacencyBuildingType)
                            && newData.buildingName == b.data.adjacencyBuildingType;
            if (!bAffectsNew && !newAffectsB) continue;

            CreateHalo(b.transform.position, b.data.glowColor);
        }
    }

    void CreateHalo(Vector3 worldCenter, Color color)
    {
        var go = new GameObject("AdjacencyHalo");
        go.transform.SetParent(transform, true);
        go.transform.position = worldCenter + Vector3.up * 0.02f;

        var lr = go.AddComponent<LineRenderer>();
        lr.loop          = true;
        lr.positionCount = CircleSegments;
        lr.useWorldSpace = false;
        lr.startWidth    = LineWidth;
        lr.endWidth      = LineWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;

        if (_sharedMat == null)
            _sharedMat = new Material(Shader.Find("Sprites/Default"));
        lr.material = _sharedMat;

        Color c = color; c.a = 0.6f;
        lr.startColor = c;
        lr.endColor   = c;

        for (int i = 0; i < CircleSegments; i++)
        {
            float a = (i / (float)CircleSegments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, 0f, Mathf.Sin(a) * Radius));
        }

        _halos.Add(lr);
    }

    void Update()
    {
        _pulseT += Time.deltaTime;
        float pulse = 0.4f + 0.4f * (Mathf.Sin(_pulseT * PulseSpeed) * 0.5f + 0.5f);
        for (int i = _halos.Count - 1; i >= 0; i--)
        {
            var lr = _halos[i];
            if (lr == null) { _halos.RemoveAt(i); continue; }
            var c = lr.startColor; c.a = pulse;
            lr.startColor = c;
            lr.endColor   = c;
        }
    }

    public void Clear()
    {
        foreach (var lr in _halos)
            if (lr != null) Destroy(lr.gameObject);
        _halos.Clear();
    }

    void OnDestroy() => Clear();
}
