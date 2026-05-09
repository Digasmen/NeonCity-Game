using UnityEngine;
using System.Collections;

public class FogManager : MonoBehaviour
{
    public static FogManager Instance { get; private set; }

    [Header("Settings")]
    public int startRevealRadius = 5;
    public int buildingRevealRadius = 6;

    private bool[,] revealed;
    private GameObject[,] fogTiles;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateFog();
        RevealAround(new Vector2Int(3, 3), startRevealRadius);

        // Always reveal around resource nodes so they're never hidden
        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            Vector2Int cell = GridManager.Instance.GetGridPosition(node.transform.position);
            RevealAround(cell, 3);
        }
    }

    void CreateFog()
    {
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;
        revealed = new bool[w, h];
        fogTiles = new GameObject[w, h];

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.02f, 0.02f, 0.06f, 0.96f);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.transform.SetParent(transform);
                tile.transform.position = new Vector3(x, 0.05f, y);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = Vector3.one * 0.99f;
                tile.GetComponent<Renderer>().sharedMaterial = mat;
                Destroy(tile.GetComponent<MeshCollider>());
                tile.name = $"Fog_{x}_{y}";
                fogTiles[x, y] = tile;
            }
        }
    }

    public void RevealAround(Vector2Int center, int radius)
    {
        for (int x = center.x - radius; x <= center.x + radius; x++)
            for (int y = center.y - radius; y <= center.y + radius; y++)
                if (GridManager.Instance.IsInsideGrid(x, y))
                    if (Vector2Int.Distance(center, new Vector2Int(x, y)) <= radius)
                        RevealCell(x, y);
    }

    void RevealCell(int x, int y)
    {
        if (revealed[x, y]) return;
        revealed[x, y] = true;
        if (fogTiles[x, y] != null)
            StartCoroutine(FadeOut(fogTiles[x, y], x, y));
    }

    IEnumerator FadeOut(GameObject tile, int x, int y)
    {
        Renderer r = tile.GetComponent<Renderer>();
        Material mat = new Material(r.sharedMaterial);
        r.material = mat;

        float duration = 0.6f;
        float elapsed = 0f;
        Color c = mat.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0.96f, 0f, elapsed / duration);
            mat.color = c;
            yield return null;
        }

        Destroy(tile);
        fogTiles[x, y] = null;
    }

    public bool IsCellRevealed(int x, int y)
    {
        if (!GridManager.Instance.IsInsideGrid(x, y)) return false;
        return revealed[x, y];
    }
}
