using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 6;

    [Header("References")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private GameObject cellPrefab;

    [Header("Layout (UI)")]
    [SerializeField] private Vector2 cellSize = new Vector2(120, 120);
    [SerializeField] private Vector2 spacing = new Vector2(10, 10);
    [SerializeField] private bool useGridLayoutGroup = true;

    private GridLayoutGroup _gridLayout;
    private readonly List<RectTransform> _spawned = new();

    // Public read-only access for other systems (TileSystem, SpawnQueue)
    public int Columns => columns;
    public int Rows => rows;

    private void Awake()
    {
        SetupLayout();
    }

    private void Start()
    {
        RebuildGrid();
    }

    private void SetupLayout()
    {
        if (useGridLayoutGroup && gridParent != null)
        {
            _gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            if (_gridLayout == null)
                _gridLayout = gridParent.gameObject.AddComponent<GridLayoutGroup>();

            _gridLayout.cellSize = cellSize;
            _gridLayout.spacing = spacing;
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayout.childAlignment = TextAnchor.UpperLeft;
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = columns;
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (gridParent == null)
        {
            Debug.LogError("GridController: gridParent chưa được gán.");
            return;
        }
        if (cellPrefab == null)
        {
            Debug.LogError("GridController: cellPrefab chưa được gán.");
            return;
        }
        ClearGrid();
        if (useGridLayoutGroup && _gridLayout != null)
        {
            _gridLayout.cellSize = cellSize;
            _gridLayout.spacing = spacing;
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = columns;
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var go = Instantiate(cellPrefab, gridParent);
                go.name = $"Cell_{x}_{y}";

                var rect = go.transform as RectTransform;
                if (!useGridLayoutGroup)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    rect.sizeDelta = cellSize;
                    rect.anchoredPosition = new Vector2(
                        x * (cellSize.x + spacing.x),
                        -y * (cellSize.y + spacing.y)
                    );
                }
                _spawned.Add(rect);
            }
        }
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var r = _spawned[i];
            if (r != null)
            {
                if (Application.isPlaying) Destroy(r.gameObject);
                else DestroyImmediate(r.gameObject);
            }
        }
        _spawned.Clear();
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            var child = gridParent.GetChild(i);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }
    public RectTransform GetCell(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return null;
        int index = y * columns + x;
        if (index < 0 || index >= _spawned.Count) return null;
        return _spawned[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize.x = Mathf.Max(1, cellSize.x);
        cellSize.y = Mathf.Max(1, cellSize.y);

        if (useGridLayoutGroup && gridParent != null)
        {
            _gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            if (_gridLayout != null)
            {
                _gridLayout.cellSize = cellSize;
                _gridLayout.spacing = spacing;
                _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                _gridLayout.constraintCount = columns;
            }
        }
    }
#endif
}

