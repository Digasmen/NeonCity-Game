using UnityEngine;

[CreateAssetMenu(fileName = "MilestoneData", menuName = "NeonCity/Milestone")]
public class MilestoneData : ScriptableObject
{
    public string description;
    public ResourceType resourceType;
    public float targetAmount;

    [Header("Optional second condition")]
    public bool hasSecondCondition;
    public ResourceType secondResourceType;
    public float secondTargetAmount;

    public BuildingData buildingToUnlock;
    public string completionMessage;
}
