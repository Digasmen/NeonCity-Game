using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour
{
    // ── Global registry (used by DroneHUD) ────────────────────────────────
    static readonly List<Drone> _all = new();
    public static IReadOnlyList<Drone> All => _all;
    void OnEnable()  => _all.Add(this);
    void OnDisable() => _all.Remove(this);

    public enum State
    {
        MovingToTarget,
        Collecting,
        ReturningHome,
        Depositing,
        MovingToCharger,
        Charging
    }

    /// <summary>Set true at runtime (via Inspector or code) to log every state transition.</summary>
    public bool debugLog = false;

    void LogState(string note)
    {
        if (!debugLog) return;
        Debug.Log($"[Drone {(data != null ? data.droneName : "?")}] " +
                  $"{currentState} bat:{battery:0}% carry:{carriedAmount:0} — {note}");
    }

    [Header("Config")]
    public DroneData data;
    public Transform homePoint;
    public Transform targetPoint;

    [Header("State")]
    public State currentState = State.MovingToTarget;
    public float carriedAmount = 0f;

    [Header("Battery")]
    [Range(0f, 100f)] public float battery = 100f;

    // ── Private ───────────────────────────────────────────────────────────

    private float actionTimer = 0f;
    private const float ActionDuration         = 0.5f;
    private const float LowBatteryThreshold    = 25f;   // start seeking charger
    private const float HomeFallbackChargeRate = 8f;    // slow trickle when far from any station

    private State     _stateBeforeCharge = State.MovingToTarget;
    private Transform _chargerTarget;                   // station or homePoint

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (data == null || homePoint == null || targetPoint == null) return;

        switch (currentState)
        {
            // ── Normal work cycle ────────────────────────────────────────

            case State.MovingToTarget:
                DrainBattery();
                if (battery <= LowBatteryThreshold) { InterruptForCharge(); LogState("low battery"); break; }
                MoveTowards(targetPoint.position);
                if (ReachedPoint(targetPoint.position))
                {
                    currentState = State.Collecting;
                    actionTimer  = ActionDuration;
                    LogState("reached target, collecting");
                }
                break;

            case State.Collecting:
                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    carriedAmount = data.carryCapacity;
                    currentState  = State.ReturningHome;
                }
                break;

            case State.ReturningHome:
                DrainBattery();
                if (battery <= LowBatteryThreshold) { InterruptForCharge(); break; }
                MoveTowards(homePoint.position);
                if (ReachedPoint(homePoint.position))
                {
                    currentState = State.Depositing;
                    actionTimer  = ActionDuration;
                }
                break;

            case State.Depositing:
                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    // Collect fires OnResourceCollected so FloatingText can spawn
                    Vector3 depositPos = homePoint != null ? homePoint.position : transform.position;
                    ResourceManager.Instance.Collect(data.resourceType, carriedAmount, depositPos);
                    LogState($"deposited {carriedAmount:0} {data.resourceType}");
                    carriedAmount = 0f;
                    currentState  = State.MovingToTarget;
                }
                break;

            // ── Charging cycle ───────────────────────────────────────────

            case State.MovingToCharger:
                if (_chargerTarget == null) { currentState = _stateBeforeCharge; break; }

                DrainBattery();

                if (battery <= 0f)
                {
                    // Dead mid-flight — stop and charge in place via fallback trickle
                    currentState = State.Charging;
                    break;
                }

                MoveTowards(_chargerTarget.position);

                if (ReachedPoint(_chargerTarget.position))
                    currentState = State.Charging;
                break;

            case State.Charging:
                ChargeBattery();

                if (battery >= 90f)
                {
                    // If we emergency-charged before reaching the station, fly there first
                    if (_chargerTarget != null && !ReachedPoint(_chargerTarget.position))
                    {
                        currentState = State.MovingToCharger;
                    }
                    else
                    {
                        currentState  = _stateBeforeCharge;
                        _chargerTarget = null;
                    }
                }
                break;
        }
    }

    // ── Battery helpers ───────────────────────────────────────────────────

    void DrainBattery()
    {
        battery = Mathf.Max(battery - data.batteryDrainRate * Time.deltaTime, 0f);
    }

    void ChargeBattery()
    {
        ChargingStation nearest = ChargingStation.FindNearest(transform.position);
        float rate = (nearest != null && nearest.InRange(transform.position))
            ? nearest.chargeRate
            : HomeFallbackChargeRate;
        battery = Mathf.Min(battery + rate * Time.deltaTime, 100f);
    }

    void InterruptForCharge()
    {
        _stateBeforeCharge = currentState;

        ChargingStation nearest = ChargingStation.FindNearest(transform.position);
        _chargerTarget = nearest != null ? nearest.transform : homePoint;

        currentState = State.MovingToCharger;
    }

    // ── Movement helpers ──────────────────────────────────────────────────

    void MoveTowards(Vector3 destination)
    {
        Vector3 target    = new Vector3(destination.x, transform.position.y, destination.z);
        Vector3 direction = (target - transform.position).normalized;

        transform.position = Vector3.MoveTowards(
            transform.position, target, data.moveSpeed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    bool ReachedPoint(Vector3 point)
    {
        return Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(point.x, point.z)) < 0.15f;
    }
}
