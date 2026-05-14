using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("All building types — needed for loading")]
    public List<BuildingData> allBuildings;

    private string SavePath => Path.Combine(Application.persistentDataPath, "neoncity.save");
    private float autoSaveTimer = 0f;
    private const float autoSaveInterval = 60f;

    void Awake()
    {
        Instance = this;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            Save();
    }
#endif

    void Start()
    {
        StartCoroutine(LoadAfterFrame());
    }

    IEnumerator LoadAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        Load();
    }

    void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            Save();
            autoSaveTimer = 0f;
        }
    }

    void OnApplicationQuit()
    {
        Save();
    }

    /// <summary>Fired after each successful save — drives the auto-save indicator toast.</summary>
    public static event System.Action OnSaved;

    public void Save()
    {
        SaveData data = new SaveData();

        foreach (var r in ResourceManager.Instance.resources)
            data.resources.Add(new ResourceSaveData { type = r.type.ToString(), amount = r.amount });

        foreach (var b in Building.All)
        {
            Vector2Int cell = GridManager.Instance.GetGridPosition(b.transform.position);
            data.buildings.Add(new BuildingSaveData
            {
                buildingName = b.data.buildingName,
                gridX = cell.x,
                gridY = cell.y,
                level = b.level
            });
        }

        data.milestoneIndex = MilestoneManager.Instance.CurrentIndex;
        data.sector         = MilestoneManager.Instance.CurrentSector;

        if (DecreeManager.Instance != null)
            data.ownedDecrees = new List<string>(DecreeManager.Instance.OwnedIds);

        Vector3 cam = Camera.main.transform.position;
        data.cameraX = cam.x;
        data.cameraY = cam.y;
        data.cameraZ = cam.z;

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved.");
        OnSaved?.Invoke();
    }

    public void Load()
    {
        if (!File.Exists(SavePath)) return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null) return;

        if (data.saveVersion < 2)
            Debug.Log("[SaveManager] Migrating save v1 → v2 (decrees + sector defaults applied)");

        foreach (var rd in data.resources)
            if (System.Enum.TryParse(rd.type, out ResourceType type))
                ResourceManager.Instance.SetAmount(type, rd.amount);

        MilestoneManager.Instance.LoadFromIndex(data.milestoneIndex);
        MilestoneManager.Instance.SetSector(data.sector > 0 ? data.sector : 1);
        DecreeManager.Instance?.LoadFromSave(data.ownedDecrees);

        Debug.Log($"Loading {data.buildings.Count} buildings...");
        foreach (var bd in data.buildings)
        {
            BuildingData buildingData = allBuildings.Find(b => b.buildingName == bd.buildingName);
            if (buildingData == null)
            {
                Debug.LogWarning($"BuildingData not found for: '{bd.buildingName}'");
                continue;
            }
            Building placed = BuildingPlacer.Instance.PlaceDirectly(buildingData, new Vector2Int(bd.gridX, bd.gridY));
            if (placed != null && bd.level > 1)
                placed.SetLevel(bd.level);
        }

        Camera.main.transform.position = new Vector3(data.cameraX, data.cameraY, data.cameraZ);
        Debug.Log("Game loaded.");
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        Debug.Log("Save deleted.");
    }

    public void NewGame()
    {
        DeleteSave();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
