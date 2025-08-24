using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maintains a queue (size 3) of upcoming tiles. Displays them in UI slots and
/// can spawn (place) one into a chosen column on the grid at the first empty row from bottom.
/// </summary>
public class SpawnQueue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileSystem tileSystem;
    [SerializeField] private RectTransform queueContainer; // parent of queue slots
    [SerializeField] private TileView queueTilePrefab;      // lightweight tile view for preview (can reuse main prefab)
    [SerializeField] private int queueSize = 3;

    [Header("Value Generation")]
    [SerializeField] private int baseValue = 2; // starting smallest value
    [SerializeField] private int[] allowedValues = { 2, 4, 8, 16, 32 };
    [SerializeField] private Color defaultColor = Color.white;

    private readonly List<TileView> _queueVisuals = new();
    private readonly Queue<int> _values = new();

    private void Awake()
    {
        if (tileSystem == null)
            tileSystem = FindFirstObjectByType<TileSystem>();
        BuildQueueSlots();
        FillQueue();
        RefreshVisuals();
    }

    private void BuildQueueSlots()
    {
        if (queueContainer == null || queueTilePrefab == null) return;
        // Clear existing children
        for (int i = queueContainer.childCount - 1; i >= 0; i--)
        {
            var c = queueContainer.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
        }
        _queueVisuals.Clear();
        for (int i = 0; i < queueSize; i++)
        {
            var t = Instantiate(queueTilePrefab, queueContainer);
            t.name = $"QueueTile_{i}";
            _queueVisuals.Add(t);
        }
    }

    private void FillQueue()
    {
        while (_values.Count < queueSize)
        {
            _values.Enqueue(GenerateValue());
        }
    }

    private int GenerateValue()
    {
        if (allowedValues != null && allowedValues.Length > 0)
        {
            int idx = Random.Range(0, allowedValues.Length);
            return allowedValues[idx];
        }
        return baseValue;
    }

    private void RefreshVisuals()
    {
        int i = 0;
        foreach (var val in _values)
        {
            if (i >= _queueVisuals.Count) break;
            var view = _queueVisuals[i];
            view.Initialize(val, defaultColor, -1, -1); // not on grid yet
            i++;
        }
        // Hide leftover visuals if any
        for (; i < _queueVisuals.Count; i++)
        {
            var view = _queueVisuals[i];
            view.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Take the first value in queue and place a tile into a column (x) at the lowest empty row.
    /// Returns true if successful.
    /// </summary>
    public bool SpawnIntoColumn(int column)
    {
        if (tileSystem == null || tileSystem.Grid == null) return false;
        if (column < 0 || column >= tileSystem.Grid.Columns) return false;
        if (_values.Count == 0) FillQueue();

        int value = _values.Dequeue();
        // Find lowest empty row from bottom (rows-1 downward)
        for (int y = tileSystem.Grid.Rows - 1; y >= 0; y--)
        {
            if (tileSystem.IsEmpty(column, y))
            {
                tileSystem.SpawnTile(column, y, value, defaultColor);
                FillQueue();
                RefreshVisuals();
                return true;
            }
        }
        // Column full -> push value back? For now just Discard and warn.
        Debug.LogWarning($"Column {column} full. Cannot place tile.");
        _values.Enqueue(value); // return it at end so queue length consistent
        RefreshVisuals();
        return false;
    }
}

