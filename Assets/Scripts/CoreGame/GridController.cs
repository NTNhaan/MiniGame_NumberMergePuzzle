using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int columns = DataConfig.GRID_COLUMNS;
    [SerializeField] private int rows = DataConfig.GRID_ROWS;

    [Header("References")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private GameObject cellPrefab;

    [Header("Layout (UI)")]
    [SerializeField] private Vector2 cellSize = default;
    [SerializeField] private Vector2 spacing = default;
    [SerializeField] private bool useGridLayoutGroup = DataConfig.GRID_USE_LAYOUT_GROUP;

    private GridLayoutGroup _gridLayout;
    private readonly List<RectTransform> _spawned = new();
    public System.Action<int> OnColumnSelected;

    [Header("Auto Spawn (Fallback)")]
    [SerializeField] private bool autoSpawnOnClick = true;
    [SerializeField] private SpawnQueue autoSpawnQueue;
    [SerializeField] private bool debugAuto = true;

    public int Columns => columns;
    public int Rows => rows;
    public RectTransform GridParent => gridParent;

    private void Awake()
    {
        ValidateParent();
        if (cellSize == default || cellSize.sqrMagnitude < 1f) cellSize = DataConfig.GRID_CELL_SIZE;
        if (spacing == default && DataConfig.GRID_CELL_SPACING != Vector2.zero) spacing = DataConfig.GRID_CELL_SPACING;
        SetupLayout();
        if (autoSpawnQueue == null)
        {
            autoSpawnQueue = FindFirstObjectByType<SpawnQueue>();
            if (debugAuto && autoSpawnQueue != null) Debug.Log("[GridController] Auto found SpawnQueue for fallback");
        }
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

    private void ValidateParent()
    {
        if (gridParent == null)
        {
            gridParent = GetComponent<RectTransform>();
        }

        if (gridParent != null)
        {
            if (!Application.isPlaying && !gridParent.gameObject.scene.IsValid())
            {
                gridParent = GetComponent<RectTransform>();
            }
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (gridParent == null)
        {
            return;
        }
        if (cellPrefab == null)
        {
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
                var cellComp = go.GetComponent<GridCell>();
                if (cellComp == null) cellComp = go.AddComponent<GridCell>();
                cellComp.Init(x, y, this);
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

    public void OnCellClicked(GridCell cell)
    {
        bool hadListeners = OnColumnSelected != null;
        OnColumnSelected?.Invoke(cell.X);

        if (!hadListeners && autoSpawnOnClick && autoSpawnQueue != null)
        {
            bool ok = autoSpawnQueue.SpawnIntoColumn(cell.X);
        }
        // else if (!hadListeners && autoSpawnOnClick && autoSpawnQueue == null && debugAuto)
        // {
        //     Debug.LogWarning("[GridController] No listeners & no autoSpawnQueue reference. Cannot spawn.");
        // }
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

