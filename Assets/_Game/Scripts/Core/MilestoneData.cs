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

    [Header("Optional building-count condition")]
    [Tooltip("If set, milestone also requires this many buildings of buildingCountName to exist.")]
    public int buildingCountTarget;
    public string buildingCountName;

    public BuildingData buildingToUnlock;
    public string completionMessage;
}
