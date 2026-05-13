using UnityEngine;

[CreateAssetMenu(fileName = "DroneData", menuName = "NeonCity/Drone Data")]
public class DroneData : ScriptableObject
{
    public string droneName = "Basic Drone";
    public float moveSpeed = 3f;
    public float carryCapacity = 20f;
    public ResourceType resourceType = ResourceType.Scrap;
    public GameObject prefab;
    public Color droneColor = new Color(0.05f, 0.35f, 1f);

    [Header("Battery")]
    [Tooltip("Battery units drained per second while flying (0–100 scale)")]
    public float batteryDrainRate = 4f;
}
