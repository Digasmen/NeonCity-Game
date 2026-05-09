using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    private bool[,] occupied;

    void Awake()
    {
        Instance = this;
        occupied = new bool[width, height];
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize, 0, y * cellSize);
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsCellFree(int x, int y)
    {
        return IsInsideGrid(x, y) && !occupied[x, y];
    }

    public void SetOccupied(int x, int y, bool value)
    {
        if (IsInsideGrid(x, y))
            occupied[x, y] = value;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0, cellSize) * 0.95f);
            }
        }
    }
}
