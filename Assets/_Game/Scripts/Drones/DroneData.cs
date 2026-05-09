using UnityEngine;

[CreateAssetMenu(fileName = "DroneData", menuName = "NeonCity/Drone Data")]
public class DroneData : ScriptableObject
{
    public string droneName = "Basic Drone";
    public float moveSpeed = 3f;
    public float carryCapacity = 20f;
    public ResourceType resourceType = ResourceType.Scrap;
    public GameObject prefab;
    public Color glowColor = new Color(0.3f, 0.8f, 1f);
}
