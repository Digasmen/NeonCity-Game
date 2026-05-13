using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [Header("Placement")]
    public LayerMask groundLayer;

    private BuildingData currentData;
    private GameObject preview;
    private bool isPlacing;
    private int _rotationSteps;

    private Building _movingBuilding;
    private Vector2Int _movingOriginalCell;
    private Quaternion _movingOriginalRot;
    private bool _skipNextTap;
    private bool _previewWasValid = true;

    private static readonly string[] _dropBuildings = { "Shelter", "Clinic" };

    public bool IsInteracting => isPlacing || _movingBuilding != null;

    void Awake() { Instance = this; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && IsInteracting)
            _rotationSteps = (_rotationSteps + 1) % 4;

        // Eat the click that triggered a mode change via UI button
        bool rawClick = Input.GetMouseButtonDown(0);
        if (_skipNextTap && rawClick) { _skipNextTap = false; return; }
        if (_skipNextTap && !rawClick) _skipNextTap = false;

        bool tap    = rawClick && !IsPointerOverUI();
        bool tapIOS = Input.GetMouseButtonUp(0) && !CameraController.Instance.IsPanning && !IsPointerOverUI();
        bool cancel = Input.GetMouseButtonDown(1);

        Vector2Int cell = GetCellUnderMouse();
        Quaternion rot  = Quaternion.Euler(0, _rotationSteps * 90f, 0);

        if (isPlacing)
        {
            if (preview != null)
            {
                preview.transform.position = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
                preview.transform.rotation = rot;

                // Tint green when placement is valid, red when blocked
                bool valid = GridManager.Instance.IsCellFree(cell.x, cell.y)
                          && FogManager.Instance.IsCellRevealed(cell.x, cell.y)
                          && ResourceManager.Instance.CanAfford(ResourceType.Scrap, currentData.scrapCost);
                if (valid != _previewWasValid)
                {
                    _previewWasValid = valid;
                    SetPreviewTint(valid
                        ? new Color(0.55f, 1.00f, 0.55f, 0.55f)
                        : new Color(1.00f, 0.30f, 0.30f, 0.55f));
                }
            }
            if (tap || tapIOS) TryPlace(cell);
            if (cancel) CancelPlacement();
            return;
        }

        if (_movingBuilding != null)
        {
            _movingBuilding.transform.position = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
            _movingBuilding.transform.rotation = rot;
            if (tap || tapIOS) ConfirmMove(cell);
            if (cancel) CancelMove();
            return;
        }

        // Idle — tap a building to open its popup
        if (tap || tapIOS)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Building b = hit.collider.GetComponentInParent<Building>();
                if (b != null) { BuildingPopup.Instance.Show(b); return; }
            }
            BuildingPopup.Instance.Hide();
        }
    }

    public void StartPlacement(BuildingData data)
    {
        CancelPlacement();
        BuildingPopup.Instance?.Hide();
        currentData = data;
        isPlacing = true;
        _rotationSteps = 0;

        _previewWasValid = true;   // reset so first frame computes fresh tint

        preview = new GameObject("Preview_" + data.buildingName);
        var meshPrefab = data.GetMeshForLevel(1);
        if (meshPrefab != null)
        {
            var meshChild = Instantiate(meshPrefab, preview.transform);
            meshChild.transform.localPosition = Vector3.zero;
            meshChild.transform.localRotation = Quaternion.identity;
            foreach (var col in meshChild.GetComponentsInChildren<Collider>())
                col.enabled = false;
            // Match the auto-scale applied in Building.SpawnMeshChild so the ghost = placed size.
            var previewRenderers = meshChild.GetComponentsInChildren<Renderer>();
            if (previewRenderers.Length > 0)
            {
                Bounds b = previewRenderers[0].bounds;
                for (int i = 1; i < previewRenderers.Length; i++) b.Encapsulate(previewRenderers[i].bounds);
                float footprint = Mathf.Max(b.size.x, b.size.z);
                if (footprint > 0.001f)
                    meshChild.transform.localScale = Vector3.one *
                        (GridManager.Instance.cellSize * 0.9f / footprint);
            }
        }
        else
        {
            var pb = preview.AddComponent<ProceduralBuilding>();
            pb.Build(data.buildingName);
            var col = preview.GetComponent<Collider>();
            if (col) col.enabled = false;
        }
        SetAlpha(preview, 0.55f);
    }

    void TryPlace(Vector2Int cell)
    {
        if (!GridManager.Instance.IsCellFree(cell.x, cell.y)) return;
        if (!FogManager.Instance.IsCellRevealed(cell.x, cell.y)) return;
        if (!ResourceManager.Instance.CanAfford(ResourceType.Scrap, currentData.scrapCost))
        {
            Debug.Log($"Not enough Scrap. Need {currentData.scrapCost}.");
            return;
        }
        if (currentData.populationRequired > 0 &&
            !ResourceManager.Instance.CanAfford(ResourceType.Population, currentData.populationRequired))
        {
            Debug.Log($"Need {currentData.populationRequired} Population.");
            return;
        }

        ResourceManager.Instance.Spend(ResourceType.Scrap, currentData.scrapCost);
        if (preview != null) { Destroy(preview); preview = null; }

        Vector3 worldPos = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
        Quaternion rot = Quaternion.Euler(0, _rotationSteps * 90f, 0);
        GameObject placed = CreatePlaceholder(worldPos);
        placed.transform.rotation = rot;
        placed.name = currentData.buildingName;

        Building building = placed.GetComponent<Building>();
        if (building == null) building = placed.AddComponent<Building>();
        if (currentData.meshVariants == null || currentData.meshVariants.Length == 0)
            placed.AddComponent<ProceduralBuilding>();
        building.Initialize(currentData);
        building.gridCell = cell;

        GridManager.Instance.SetOccupied(cell.x, cell.y, true);
        GridManager.Instance.RegisterBuilding(cell, building);

        // Refresh adjacency bonuses for this building and its 4 neighbours
        RefreshAdjacencyAround(cell, building);

        FogManager.Instance.RevealAround(cell, FogManager.Instance.buildingRevealRadius);
        SoundManager.Instance.PlayBuildPlace();
        ParticleManager.Instance?.PlayBuildPlace(worldPos);

        if (System.Array.IndexOf(_dropBuildings, currentData.buildingName) >= 0)
            StartCoroutine(DropAnimation(placed, worldPos));

        isPlacing = false;
        currentData = null;
    }

    public Building PlaceDirectly(BuildingData data, Vector2Int cell)
    {
        if (!GridManager.Instance.IsCellFree(cell.x, cell.y)) return null;

        Vector3 worldPos = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
        GameObject placed = CreatePlaceholder(worldPos);
        placed.name = data.buildingName;

        Building building = placed.GetComponent<Building>();
        if (building == null) building = placed.AddComponent<Building>();
        if (data.meshVariants == null || data.meshVariants.Length == 0)
            placed.AddComponent<ProceduralBuilding>();
        building.Initialize(data);
        building.gridCell = cell;

        GridManager.Instance.SetOccupied(cell.x, cell.y, true);
        GridManager.Instance.RegisterBuilding(cell, building);
        RefreshAdjacencyAround(cell, building);

        FogManager.Instance.RevealAround(cell, FogManager.Instance.buildingRevealRadius);
        return building;
    }

    public void SelectForMove(Building building)
    {
        _skipNextTap = true;
        _movingBuilding = building;
        _movingOriginalCell = building.gridCell;
        _movingOriginalRot = building.transform.rotation;
        _rotationSteps = Mathf.RoundToInt(building.transform.eulerAngles.y / 90f) % 4;
        GridManager.Instance.SetOccupied(_movingOriginalCell.x, _movingOriginalCell.y, false);
        SetAlpha(building.gameObject, 0.55f);
    }

    void ConfirmMove(Vector2Int cell)
    {
        if (!GridManager.Instance.IsCellFree(cell.x, cell.y)) return;
        if (!FogManager.Instance.IsCellRevealed(cell.x, cell.y)) return;

        Vector3 worldPos = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
        _movingBuilding.transform.position = worldPos;
        _movingBuilding.transform.rotation = Quaternion.Euler(0, _rotationSteps * 90f, 0);

        // Update registry: unregister old cell, register new
        GridManager.Instance.UnregisterBuilding(_movingBuilding.gridCell);
        _movingBuilding.gridCell = cell;
        GridManager.Instance.SetOccupied(cell.x, cell.y, true);
        GridManager.Instance.RegisterBuilding(cell, _movingBuilding);
        RefreshAdjacencyAround(cell, _movingBuilding);

        FogManager.Instance.RevealAround(cell, FogManager.Instance.buildingRevealRadius);
        SetAlpha(_movingBuilding.gameObject, 1f);
        _movingBuilding = null;
    }

    void CancelMove()
    {
        if (_movingBuilding == null) return;
        _movingBuilding.transform.position = GridManager.Instance.GetWorldPosition(_movingOriginalCell.x, _movingOriginalCell.y);
        _movingBuilding.transform.rotation = _movingOriginalRot;
        GridManager.Instance.SetOccupied(_movingOriginalCell.x, _movingOriginalCell.y, true);
        SetAlpha(_movingBuilding.gameObject, 1f);
        _movingBuilding = null;
    }

    void CancelPlacement()
    {
        if (preview != null) Destroy(preview);
        preview = null;
        isPlacing = false;
        currentData = null;
    }

    IEnumerator DropAnimation(GameObject building, Vector3 finalPos)
    {
        // Drop phase — ease-in fall from above
        float dropDuration = 0.32f;
        float elapsed = 0f;
        Vector3 startPos = finalPos + Vector3.up * 5f;
        building.transform.position = startPos;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            building.transform.position = Vector3.Lerp(startPos, finalPos, t * t);
            yield return null;
        }
        building.transform.position = finalPos;

        // Squish phase — flatten on impact
        float squishDuration = 0.1f;
        elapsed = 0f;
        while (elapsed < squishDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / squishDuration;
            building.transform.localScale = new Vector3(
                Mathf.Lerp(1.25f, 1f, t),
                Mathf.Lerp(0.65f, 1f, t),
                Mathf.Lerp(1.25f, 1f, t));
            yield return null;
        }

        // Small bounce
        float bounceDuration = 0.18f;
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.18f;
            building.transform.position = finalPos + Vector3.up * bounce;
            yield return null;
        }

        building.transform.position = finalPos;
        building.transform.localScale = Vector3.one;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
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

    GameObject CreatePlaceholder(Vector3 pos)
    {
        GameObject go = new GameObject("Building");
        go.transform.position = pos;
        return go;
    }

    /// <summary>Recalculates adjacency bonuses for a building and its 4 cardinal neighbours.</summary>
    static void RefreshAdjacencyAround(Vector2Int cell, Building center)
    {
        center.RefreshAdjacency();
        foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Building nb = GridManager.Instance.GetBuildingAt(cell + d);
            nb?.RefreshAdjacency();
        }
    }

    void SetPreviewTint(Color tint)
    {
        if (preview == null) return;
        foreach (var r in preview.GetComponentsInChildren<Renderer>())
            foreach (var mat in r.materials)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend",   0f);
                mat.SetFloat("_ZWrite",  0f);
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                mat.color = tint;
            }
    }

    void SetAlpha(GameObject obj, float alpha)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in r.materials)
            {
                if (alpha < 1f)
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.renderQueue = 3000;
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                else
                {
                    mat.SetFloat("_Surface", 0f);
                    mat.SetFloat("_ZWrite", 1f);
                    mat.renderQueue = 2000;
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                c.a = alpha;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
            }
        }
    }
}
