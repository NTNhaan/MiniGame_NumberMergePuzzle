using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private TileView tilePrefab; // Script component on the tile prefab
    [SerializeField] private Transform tileContainer; // parent for instantiated tiles (optional)

    [Header("Pool Settings")]
    [SerializeField] private int preloadCount = 20;

    private readonly Queue<TileView> _pool = new();
    private readonly Dictionary<(int x, int y), TileView> _tiles = new();

    public GridController Grid => gridController;

    private void Awake()
    {
        if (gridController == null)
            gridController = Object.FindFirstObjectByType<GridController>();
        if (tileContainer == null)
            tileContainer = transform;
        Preload();
    }

    private void Preload()
    {
        if (tilePrefab == null) return;
        for (int i = 0; i < preloadCount; i++)
        {
            var t = Instantiate(tilePrefab, tileContainer);
            t.gameObject.SetActive(false);
            _pool.Enqueue(t);
        }
    }

    private TileView GetFromPool()
    {
        if (_pool.Count == 0)
        {
            var extra = Instantiate(tilePrefab, tileContainer);
            extra.gameObject.SetActive(false);
            _pool.Enqueue(extra);
        }
        var tile = _pool.Dequeue();
        tile.gameObject.SetActive(true);
        return tile;
    }

    public TileView SpawnTile(int x, int y, int value, Color color)
    {
        if (!IsInside(x, y)) return null;
        if (_tiles.ContainsKey((x, y)))
        {
            Debug.LogWarning($"Ô ({x},{y}) đã có tile.");
            return null;
        }

        var cell = gridController.GetCell(x, y);
        if (cell == null)
        {
            Debug.LogError($"Không tìm thấy cell ({x},{y}).");
            return null;
        }

        var tile = GetFromPool();
        tile.transform.SetParent(cell, false);
        tile.Initialize(value, color, x, y);
        _tiles[(x, y)] = tile;
        return tile;
    }

    public bool IsInside(int x, int y) => x >= 0 && x < gridController.Columns && y >= 0 && y < gridController.Rows;

    public bool IsEmpty(int x, int y) => !_tiles.ContainsKey((x, y));

    public TileView GetTile(int x, int y)
    {
        _tiles.TryGetValue((x, y), out var t);
        return t;
    }

    public void RemoveTile(int x, int y)
    {
        if (_tiles.TryGetValue((x, y), out var t))
        {
            _tiles.Remove((x, y));
            ReturnToPool(t);
        }
    }

    private void ReturnToPool(TileView tile)
    {
        tile.gameObject.SetActive(false);
        tile.transform.SetParent(tileContainer, false);
        _pool.Enqueue(tile);
    }
}


