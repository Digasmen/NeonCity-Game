using UnityEngine;
using System.Collections.Generic;
using System;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    public List<MilestoneData> milestones;

    private int currentIndex = 0;

    public int CurrentIndex => currentIndex;
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

    public void LoadFromIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, milestones.Count);
        // Silently unlock all buildings from completed milestones
        for (int i = 0; i < currentIndex; i++)
            if (milestones[i].buildingToUnlock != null)
                BuildMenuUI.Instance.UnlockBuilding(milestones[i].buildingToUnlock);
        OnMilestoneChanged?.Invoke(Current);
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
