using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "NeonCity/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public int width = 1;
    public int height = 1;
    public int scrapCost = 100;
    public Sprite icon;
    public DroneData droneData;

    [Header("Passive Production")]
    public ResourceType passiveResourceType;
    public float passiveRatePerMinute = 0f;
}
