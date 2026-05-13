using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place on any building to make it a drone charging station.
/// Drones automatically seek the nearest station when battery drops low.
/// </summary>
public class ChargingStation : MonoBehaviour
{
    [Tooltip("Battery units restored per second when a drone is in range")]
    public float chargeRate = 35f;

    [Tooltip("World-unit radius in which drones receive fast charging")]
    public float chargeRadius = 4f;

    // ── Static registry ───────────────────────────────────────────────────

    private static readonly List<ChargingStation> _all = new();

    void OnEnable()  => _all.Add(this);
    void OnDisable() => _all.Remove(this);
    void OnDestroy() => _all.Remove(this);

    /// <summary>Returns the nearest ChargingStation in the scene, or null if none.</summary>
    public static ChargingStation FindNearest(Vector3 pos)
    {
        ChargingStation best = null;
        float minDist = float.MaxValue;
        foreach (var s in _all)
        {
            if (s == null) continue;
            float d = Vector3.Distance(pos, s.transform.position);
            if (d < minDist) { minDist = d; best = s; }
        }
        return best;
    }

    /// <summary>True when <paramref name="pos"/> is within the charging radius.</summary>
    public bool InRange(Vector3 pos) =>
        Vector3.Distance(pos, transform.position) < chargeRadius;

    // ── Gizmo ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 0.5f, 0.25f);
        Gizmos.DrawSphere(transform.position, chargeRadius);
        Gizmos.color = new Color(0.1f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, chargeRadius);
    }
}
