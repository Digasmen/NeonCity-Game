using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int saveVersion = 2;
    public List<ResourceSaveData> resources = new();
    public List<BuildingSaveData> buildings = new();
    public int milestoneIndex;
    public float cameraX;
    public float cameraY;
    public float cameraZ;

    // ── v2: Decrees + Sector ────────────────────────────────────────────
    public List<string> ownedDecrees = new();
    public int sector = 1;
}

[System.Serializable]
public class ResourceSaveData
{
    public string type;
    public float amount;
}

[System.Serializable]
public class BuildingSaveData
{
    public string buildingName;
    public int gridX;
    public int gridY;
    public int level = 1;
}
