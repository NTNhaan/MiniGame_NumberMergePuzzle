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
    [SerializeField] private Vector2 cellSize = default; // lấy từ DataConfig nếu (0,0)
    [SerializeField] private Vector2 spacing = default;  // lấy từ DataConfig nếu (0,0)
    [SerializeField] private bool useGridLayoutGroup = DataConfig.GRID_USE_LAYOUT_GROUP;

    private GridLayoutGroup _gridLayout;
    private readonly List<RectTransform> _spawned = new();
    public System.Action<int> OnColumnSelected; // callback: column index

    [Header("Auto Spawn (Fallback)")]
    [SerializeField] private bool autoSpawnOnClick = true; // nếu không ai subscribe sẽ tự spawn
    [SerializeField] private SpawnQueue autoSpawnQueue; // tham chiếu SpawnQueue
    [SerializeField] private bool debugAuto = true;

    public int Columns => columns;
    public int Rows => rows;
    public RectTransform GridParent => gridParent;

    private void Awake()
    {
        ValidateParent();
        // Áp dụng cấu hình tĩnh nếu chưa set thủ công trong inspector
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
            Debug.Log("[GridController] gridParent null -> auto assign self RectTransform");
        }

        if (gridParent != null)
        {
            // Nếu object không có scene hợp lệ (scene.name is null hoặc rỗng) => có thể là prefab asset (trong editor)
            if (!Application.isPlaying && !gridParent.gameObject.scene.IsValid())
            {
                Debug.LogWarning("[GridController] gridParent có vẻ là prefab asset (scene invalid). Đổi sang self để tránh lỗi Instantiate parent persistent.");
                gridParent = GetComponent<RectTransform>();
            }
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
                // Gắn script GridCell để lưu toạ độ + click
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

    // Được gọi từ GridCell khi click
    public void OnCellClicked(GridCell cell)
    {
        // Trả về column từ cell.X
        bool hadListeners = OnColumnSelected != null;
        OnColumnSelected?.Invoke(cell.X);
        Debug.Log($"[GridController] Click column {cell.X} (type {cell.ColumnType}) listeners={(hadListeners ? OnColumnSelected.GetInvocationList().Length : 0)}");

        if (!hadListeners && autoSpawnOnClick && autoSpawnQueue != null)
        {
            bool ok = autoSpawnQueue.SpawnIntoColumn(cell.X);
            if (debugAuto) Debug.Log($"[GridController] Fallback auto spawn column {cell.X} result={ok}");
        }
        else if (!hadListeners && autoSpawnOnClick && autoSpawnQueue == null && debugAuto)
        {
            Debug.LogWarning("[GridController] No listeners & no autoSpawnQueue reference. Cannot spawn.");
        }
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

