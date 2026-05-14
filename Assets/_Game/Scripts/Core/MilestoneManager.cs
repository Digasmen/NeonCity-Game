using UnityEngine;
using System.Collections.Generic;
using System;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    public List<MilestoneData> milestones;

    private int currentIndex = 0;
    private int currentSector = 1;

    public int CurrentIndex => currentIndex;
    public int CurrentSector => currentSector;
    public MilestoneData Current => currentIndex < milestones.Count ? milestones[currentIndex] : null;
    public event Action<MilestoneData> OnMilestoneCompleted;
    public event Action<MilestoneData> OnMilestoneChanged;
    public event Action OnGameWon;
    public event Action<int> OnSectorAdvanced;

    /// <summary>Sector boundaries — milestone index at which each sector begins.
    /// Sector 1 = milestones 0–4 (M1–M5); Sector 2 = milestones 5–7 (M6–M8). Extend as needed.</summary>
    static readonly int[] _sectorStartIndex = { 0, 5 };

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

        bool primary = ResourceManager.Instance.Get(Current.resourceType) >= Current.targetAmount;
        bool secondary = !Current.hasSecondCondition ||
            ResourceManager.Instance.Get(Current.secondResourceType) >= Current.secondTargetAmount;
        bool buildingCount = string.IsNullOrEmpty(Current.buildingCountName) ||
                             CountBuildings(Current.buildingCountName) >= Current.buildingCountTarget;
        if (primary && secondary && buildingCount)
            CompleteCurrent();
    }

    static int CountBuildings(string name)
    {
        int n = 0;
        foreach (var b in Building.All)
            if (b != null && b.data != null && b.data.buildingName == name) n++;
        return n;
    }

    public float GetProgress()
    {
        if (Current == null) return 1f;
        return Mathf.Clamp01(ResourceManager.Instance.Get(Current.resourceType) / Current.targetAmount);
    }

    public void LoadFromIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, milestones.Count);
        currentSector = SectorForIndex(currentIndex);
        // Silently unlock all buildings from completed milestones
        for (int i = 0; i < currentIndex; i++)
            if (milestones[i].buildingToUnlock != null)
                BuildMenuUI.Instance.UnlockBuilding(milestones[i].buildingToUnlock);
        OnMilestoneChanged?.Invoke(Current);
    }

    public void SetSector(int sector)
    {
        currentSector = Mathf.Max(1, sector);
    }

    /// <summary>Used by VictoryScreen "Continue" button — advances current milestone
    /// past the end of the current sector into the next sector.</summary>
    public void AdvanceToSector(int targetSector)
    {
        if (targetSector <= currentSector) return;
        if (targetSector - 1 < _sectorStartIndex.Length)
        {
            currentIndex = Mathf.Clamp(_sectorStartIndex[targetSector - 1], 0, milestones.Count);
            currentSector = targetSector;
            OnSectorAdvanced?.Invoke(currentSector);
            OnMilestoneChanged?.Invoke(Current);
        }
    }

    static int SectorForIndex(int idx)
    {
        int sector = 1;
        for (int i = 0; i < _sectorStartIndex.Length; i++)
            if (idx >= _sectorStartIndex[i]) sector = i + 1;
        return sector;
    }

    /// <summary>True if currentIndex sits on a sector boundary (i.e. previous sector's
    /// final milestone just completed and the next sector is waiting to start).</summary>
    public bool IsAtSectorBoundary()
    {
        if (currentIndex >= milestones.Count) return false;
        for (int i = 1; i < _sectorStartIndex.Length; i++)
            if (_sectorStartIndex[i] == currentIndex) return true;
        return false;
    }

    public int NextSector => currentSector + 1;

    public void CompleteCurrent()
    {
        MilestoneData completed = Current;

        if (completed.buildingToUnlock != null)
            BuildMenuUI.Instance.UnlockBuilding(completed.buildingToUnlock);

        OnMilestoneCompleted?.Invoke(completed);
        currentIndex++;

        // Sector-end check: if we just finished the last milestone of a sector,
        // fire OnGameWon for the VictoryScreen and remain at currentIndex so the
        // "Continue to Sector N+1" button can advance via AdvanceToSector().
        int nextSectorIdx = -1;
        for (int i = 0; i < _sectorStartIndex.Length; i++)
            if (_sectorStartIndex[i] == currentIndex) { nextSectorIdx = i; break; }

        if (currentIndex >= milestones.Count)
        {
            OnGameWon?.Invoke();
        }
        else if (nextSectorIdx > 0)
        {
            // Crossed into a new sector boundary. Fire OnGameWon so VictoryScreen
            // shows, but don't auto-advance — wait for user to click "Continue".
            OnGameWon?.Invoke();
        }
        else
        {
            OnMilestoneChanged?.Invoke(Current);
        }
    }
}
