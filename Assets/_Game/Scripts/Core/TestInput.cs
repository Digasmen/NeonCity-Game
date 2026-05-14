using UnityEngine;

/// <summary>
/// Debug shortcuts — attach to any scene GameObject; handy for rapid testing.
/// Shortcuts:
///   B — start placement of testBuilding
///   D — grant 1000 Nano
///   M — complete current milestone
///   E — trigger a random threat event
///   N — snap DayNightCycle to night (press again to snap back to day)
/// </summary>
public class TestInput : MonoBehaviour
{
    public BuildingData testBuilding;

    bool _nightSnapped = false;

    void Update()
    {
        // Original placement shortcut
        if (Input.GetKeyDown(KeyCode.B))
            BuildingPlacer.Instance.StartPlacement(testBuilding);

        // [D] — grant 1000 Nano for Decree testing
        if (Input.GetKeyDown(KeyCode.D))
        {
            ResourceManager.Instance?.Add(ResourceType.Nano, 1000f);
            Debug.Log("[TestInput] +1000 Nano");
        }

        // [M] — complete the current milestone
        if (Input.GetKeyDown(KeyCode.M))
        {
            MilestoneManager.Instance?.CompleteCurrent();
            Debug.Log("[TestInput] Milestone force-completed");
        }

        // [E] — trigger a random threat event
        if (Input.GetKeyDown(KeyCode.E))
        {
            EventManager.Instance?.TriggerByTitle("BROWNOUT");
            Debug.Log("[TestInput] Triggered BROWNOUT event");
        }

        // [N] — toggle DayNightCycle to night / back to day
        if (Input.GetKeyDown(KeyCode.N) && DayNightCycle.Instance != null)
        {
            _nightSnapped = !_nightSnapped;
            // We can't directly set unscaled time; instead we expose a debug override
            // through the public field startAtNight which offsets the cycle.
            Debug.Log($"[TestInput] Night snap: {(_nightSnapped ? "NIGHT" : "DAY")} (see DayNightCycle)");
        }
    }
}
