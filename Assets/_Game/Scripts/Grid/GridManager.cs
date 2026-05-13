using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int   width    = 20;
    public int   height   = 20;
    public float cellSize = 1f;

    private bool[,]                             occupied;
    private readonly Dictionary<Vector2Int, Building> _buildingAt = new();

    void Awake()
    {
        Instance = this;
        occupied = new bool[width, height];
    }

    // ── World / grid conversion ────────────────────────────────────────────

    public Vector3 GetWorldPosition(int x, int y)
        => new Vector3(x * cellSize, 0, y * cellSize);

    public Vector2Int GetGridPosition(Vector3 worldPos)
        => new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize));

    // ── Occupancy ──────────────────────────────────────────────────────────

    public bool IsInsideGrid(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height;

    public bool IsCellFree(int x, int y)
        => IsInsideGrid(x, y) && !occupied[x, y];

    public void SetOccupied(int x, int y, bool value)
    {
        if (IsInsideGrid(x, y))
            occupied[x, y] = value;
    }

    // ── Building registry (for adjacency lookups) ─────────────────────────

    public void RegisterBuilding(Vector2Int cell, Building building)
        => _buildingAt[cell] = building;

    public void UnregisterBuilding(Vector2Int cell)
        => _buildingAt.Remove(cell);

    /// <summary>Returns the Building at the given grid cell, or null if empty.</summary>
    public Building GetBuildingAt(Vector2Int cell)
        => _buildingAt.TryGetValue(cell, out var b) ? b : null;

    // ── Gizmos ─────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
            Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0, cellSize) * 0.95f);
        }
    }
}
