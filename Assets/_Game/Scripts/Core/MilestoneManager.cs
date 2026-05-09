using UnityEngine;
using System.Collections.Generic;
using System;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    public List<MilestoneData> milestones;

    private int currentIndex = 0;

    public MilestoneData Current => currentIndex < milestones.Count ? milestones[currentIndex] : null;
    public event Action<MilestoneData> OnMilestoneCompleted;
    public event Action<MilestoneData> OnMilestoneChanged;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        OnMilestoneChanged?.Invoke(Current);
    }

    void Update()
    {
        if (Current == null) return;

        float amount = ResourceManager.Instance.Get(Current.resourceType);
        if (amount >= Current.targetAmount)
            CompleteCurrent();
    }

    public float GetProgress()
    {
        if (Current == null) return 1f;
        return Mathf.Clamp01(ResourceManager.Instance.Get(Current.resourceType) / Current.targetAmount);
    }

    void CompleteCurrent()
    {
        MilestoneData completed = Current;

        if (completed.buildingToUnlock != null)
            BuildMenuUI.Instance.UnlockBuilding(completed.buildingToUnlock);

        OnMilestoneCompleted?.Invoke(completed);
        currentIndex++;
        OnMilestoneChanged?.Invoke(Current);
    }
}
