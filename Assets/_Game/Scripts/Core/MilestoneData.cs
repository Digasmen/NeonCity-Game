using UnityEngine;

[CreateAssetMenu(fileName = "MilestoneData", menuName = "NeonCity/Milestone")]
public class MilestoneData : ScriptableObject
{
    public string description;
    public ResourceType resourceType;
    public float targetAmount;
    public BuildingData buildingToUnlock;
    public string completionMessage;
}
