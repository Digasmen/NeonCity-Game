using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [Header("Placement")]
    public LayerMask groundLayer;

    private BuildingData currentData;
    private GameObject preview;
    private bool isPlacing;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!isPlacing) return;

        Vector2Int cell = GetCellUnderMouse();

        if (preview != null)
            preview.transform.position = GridManager.Instance.GetWorldPosition(cell.x, cell.y);

        if (Input.GetMouseButtonDown(0))
            TryPlace(cell);

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();
    }

    public void StartPlacement(BuildingData data)
    {
        CancelPlacement();
        currentData = data;
        isPlacing = true;

        if (data.prefab != null)
        {
            preview = Instantiate(data.prefab);
            SetPreviewAlpha(preview, 0.5f);
        }
    }

    void TryPlace(Vector2Int cell)
    {
        if (!GridManager.Instance.IsCellFree(cell.x, cell.y)) return;

        if (preview != null) { Destroy(preview); preview = null; }

        Vector3 worldPos = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
        GameObject placed = Instantiate(currentData.prefab, worldPos, Quaternion.identity);
        placed.name = currentData.buildingName;

        Building building = placed.GetComponent<Building>();
        if (building == null) building = placed.AddComponent<Building>();
        building.Initialize(currentData);

        GridManager.Instance.SetOccupied(cell.x, cell.y, true);
        isPlacing = false;
        currentData = null;
    }

    void CancelPlacement()
    {
        if (preview != null) Destroy(preview);
        preview = null;
        isPlacing = false;
        currentData = null;
    }

    Vector2Int GetCellUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            return GridManager.Instance.GetGridPosition(point);
        }
        return Vector2Int.zero;
    }

    void SetPreviewAlpha(GameObject obj, float alpha)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }
}
