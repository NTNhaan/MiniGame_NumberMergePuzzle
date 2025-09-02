using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;
    [Header("Brick Data")]
    [SerializeField] private BrickSet brickSet; 

    [Header("Pool Settings")]
    [SerializeField] private int preloadCount = 20;

    private readonly Queue<TileView> _pool = new();
    private readonly Dictionary<(int x, int y), TileView> _tiles = new();

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    public GridController Grid => gridController;


    private void Awake()
    {
        if (gridController == null)
            gridController = Object.FindFirstObjectByType<GridController>();
        if (tileContainer == null)
            tileContainer = transform;
        else if (!Application.isPlaying && !tileContainer.gameObject.scene.IsValid())
        {
            tileContainer = transform;
        }
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
        if (tilePrefab == null)
        {
            return null;
        }
        int safety = _pool.Count + 2;
        while (_pool.Count > 0 && safety-- > 0)
        {
            var peek = _pool.Dequeue();
            if (peek == null || peek.Equals(null))
            {
                continue;
            }
            if (!peek.gameObject.activeSelf)
                peek.gameObject.SetActive(true);
            return peek;
        }
        var extra = Instantiate(tilePrefab, tileContainer);
        extra.gameObject.SetActive(true);
        return extra;
    }

    public TileView SpawnTile(int x, int y, int value, Color color)
    {
        if (!IsInside(x, y)) return null;
        if (_tiles.ContainsKey((x, y)))
        {
            return null;
        }

        var cell = gridController.GetCell(x, y);
        if (cell == null)
        {
            return null;
        }

        var tile = GetFromPool();
        tile.transform.SetParent(cell, false);
        tile.Initialize(value, color, x, y);
        ApplySprite(tile, value);
        _tiles[(x, y)] = tile;
        return tile;
    }

    #region Animated Spawn
    [Header("Animation")]
    [SerializeField] private float moveDuration = DataConfig.TILE_MOVE_DURATION;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool _isAnimatingSpawn;

    [Header("Tile Layout In Cell")]
    [SerializeField] private bool centerTileInCell = true;
    [SerializeField] private bool resizeToCell = false;
    [SerializeField] private Vector2 sizePadding = new Vector2(8, 8);

    [Header("Spawn Path")]
    [SerializeField] private bool verticalFromBelow = DataConfig.TILE_VERTICAL_FROM_BELOW;
    [SerializeField] private float fallbackBelowOffset = 120f;
    [SerializeField] private bool startAtColumnBottom = DataConfig.TILE_START_AT_COLUMN_BOTTOM;
    [SerializeField] private bool constantSpeed = DataConfig.TILE_CONSTANT_SPEED;
    [SerializeField] private float pixelsPerSecond = DataConfig.TILE_PIXELS_PER_SECOND;
    [SerializeField] private float minDuration = DataConfig.TILE_MIN_DURATION;
    [SerializeField] private float maxDuration = DataConfig.TILE_MAX_DURATION;

    [Header("Queue -> Column Transition")]
    [SerializeField] private bool twoPhaseFromQueue = DataConfig.TILE_TWO_PHASE_FROM_QUEUE;
    [Range(0.1f, 0.9f)][SerializeField] private float firstPhasePortion = 0.35f;

    [Header("Alignment Fixes")]
    [SerializeField] private bool forcePureVertical = DataConfig.TILE_FORCE_PURE_VERTICAL;
    [SerializeField] private bool verboseAlignmentDebug = false;
    [Header("Two-Phase Variants")]
    [SerializeField] private bool lShapeTwoPhase = DataConfig.TILE_L_SHAPE_TWO_PHASE;
    [SerializeField] private bool preserveQueueStart = DataConfig.TILE_PRESERVE_QUEUE_START;
    [SerializeField] private bool useRootCanvasForQueueStart = DataConfig.TILE_USE_ROOT_CANVAS_FOR_QUEUE_START;

    public bool SpawnTileAnimatedFromQueue(int column, int value, Color color, RectTransform startRect)
    {
        if (_isAnimatingSpawn) { DebugLog("SpawnTileAnimatedFromQueue blocked: đang có animation."); return false; }
        if (gridController == null) { DebugLog("SpawnTileAnimatedFromQueue FAIL: gridController = null"); return false; }
        if (column < 0 || column >= gridController.Columns) { DebugLog($"SpawnTileAnimatedFromQueue FAIL: column {column} ngoài biên."); return false; }

        int targetRow = -1;
        if (startAtColumnBottom)
        {
            for (int y = 0; y < gridController.Rows; y++)
            {
                if (IsEmpty(column, y)) { targetRow = y; break; }
            }
        }
        else
        {
            for (int y = gridController.Rows - 1; y >= 0; y--)
            {
                if (IsEmpty(column, y)) { targetRow = y; break; }
            }
        }
        if (targetRow == -1) { DebugLog($"SpawnTileAnimatedFromQueue FAIL: cột {column} đầy."); return false; }

        var cell = gridController.GetCell(column, targetRow);
        if (cell == null) { DebugLog($"SpawnTileAnimatedFromQueue FAIL: không tìm thấy cell ({column},{targetRow})."); return false; }

        var tile = GetFromPool();
        tile.Initialize(value, color, column, targetRow);
        ApplySprite(tile, value);

        RectTransform animationParent = gridController != null ? gridController.GridParent : (RectTransform)tileContainer;
        if (preserveQueueStart && useRootCanvasForQueueStart && startRect != null && !startRect.Equals(null))
        {
            var rootCanvas = startRect.GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null)
            {
                animationParent = rootCanvas.transform as RectTransform;
            }
        }
        tile.transform.SetParent(animationParent, false);

        var tileRect = (RectTransform)tile.transform;

        if (startRect == null)
        {
            DebugLog("startRect null -> dùng tileContainer làm gốc.");
        }
        else if (startRect.Equals(null))
        {
            DebugLog("startRect reference đã bị destroy (MissingReference). Fallback spawn trực tiếp không anim.");
            tile.transform.SetParent(cell, false);
            tile.Initialize(value, color, column, targetRow);
            _tiles[(column, targetRow)] = tile;
            return true;
        }

        Vector3 startWorld;
        Vector3? midWorld = null;
        if (startRect != null && !startRect.Equals(null))
        {
            startWorld = startRect.TransformPoint(startRect.rect.center);
        }
        else
        {
            startWorld = tileContainer.TransformPoint(Vector3.zero);
        }
        Vector3 targetWorld = cell.TransformPoint(cell.rect.center);

        if (verticalFromBelow)
        {
            if (startAtColumnBottom)
            {
                var bottomCell = gridController.GetCell(column, gridController.Rows - 1);
                Vector3 bottomWorld = bottomCell != null ? bottomCell.TransformPoint(bottomCell.rect.center) : targetWorld;
                if (preserveQueueStart && startRect != null && !startRect.Equals(null))
                {
                    if (forcePureVertical)
                    {
                        startWorld = new Vector3(targetWorld.x, startWorld.y, targetWorld.z);
                        midWorld = null;
                    }
                    else if (twoPhaseFromQueue)
                    {
                        midWorld = lShapeTwoPhase
                            ? new Vector3(targetWorld.x, startWorld.y, targetWorld.z)
                            : bottomWorld;
                    }
                }
                else
                {
                    if (forcePureVertical)
                    {
                        startWorld = new Vector3(bottomWorld.x, bottomWorld.y, bottomWorld.z);
                        midWorld = null;
                    }
                    else if (twoPhaseFromQueue && startRect != null && !startRect.Equals(null))
                    {
                        midWorld = lShapeTwoPhase
                            ? new Vector3(bottomWorld.x, startWorld.y, bottomWorld.z)
                            : bottomWorld;
                    }
                    else
                    {
                        startWorld = new Vector3(targetWorld.x, bottomWorld.y, targetWorld.z);
                    }
                }
            }
            else
            {
                if (startRect == null || startRect.Equals(null))
                {
                    startWorld = targetWorld + new Vector3(0, -fallbackBelowOffset, 0);
                }
                else
                {
                    if (forcePureVertical)
                    {
                        startWorld = new Vector3(targetWorld.x, startWorld.y, targetWorld.z);
                        midWorld = null;
                    }
                    else
                    {
                        startWorld = new Vector3(targetWorld.x, startWorld.y, targetWorld.z);
                    }
                }
            }
        }
        Vector2 localStart = animationParent.InverseTransformPoint(startWorld);
        Vector2 localTarget = animationParent.InverseTransformPoint(targetWorld);
        Vector2? localMid = midWorld.HasValue ? animationParent.InverseTransformPoint(midWorld.Value) : (Vector2?)null;

        tileRect.anchorMin = tileRect.anchorMax = new Vector2(0.5f, 0.5f);
        tileRect.pivot = new Vector2(0.5f, 0.5f);
        tileRect.anchoredPosition = localStart;

        _isAnimatingSpawn = true;
        float distanceY = Mathf.Abs(localTarget.y - localStart.y);
        float distanceX = 0f;
        if (localMid.HasValue && twoPhaseFromQueue && lShapeTwoPhase && !forcePureVertical)
        {
            distanceX = Mathf.Abs(localMid.Value.x - localStart.x);
        }
        float distance = distanceY + distanceX;
        float duration;
        if (constantSpeed)
            duration = Mathf.Clamp(distance / Mathf.Max(10f, pixelsPerSecond), minDuration, maxDuration);
        else
            duration = moveDuration;

        Vector3 debugStartWorld = startWorld;
        Vector3 debugTargetWorld = targetWorld;
        Vector3 debugMidWorld = midWorld ?? Vector3.negativeInfinity;
        StartCoroutine(SpawnMoveCoroutine(tile, column, targetRow, localTarget, duration, animationParent, localMid, debugStartWorld, debugMidWorld, debugTargetWorld));
        return true;
    }
    private System.Collections.IEnumerator SpawnMoveCoroutine(TileView tile, int x, int y, Vector2 target, float duration, RectTransform animationParent, Vector2? mid, Vector3 worldStart, Vector3 worldMid, Vector3 worldTarget)
    {
        var rect = (RectTransform)tile.transform;
        Vector2 from = rect.anchoredPosition;
        float t = 0f;
        bool useTwoPhase = mid.HasValue && twoPhaseFromQueue && startAtColumnBottom && !forcePureVertical;
        bool useLShape = useTwoPhase && lShapeTwoPhase;
        Vector2 midPos = mid ?? Vector2.zero;

        while (t < 1f)
        {
            if (tile == null || rect == null) { DebugLog("Anim aborted: tile bị destroy giữa chừng."); yield break; }
            t += Time.unscaledDeltaTime / duration;
            float clampedT = Mathf.Clamp01(t);
            float ease = moveCurve.Evaluate(clampedT);

            Vector2 pos;
            if (useTwoPhase)
            {
                float split = Mathf.Clamp(firstPhasePortion, 0.05f, 0.95f);
                if (clampedT < split)
                {
                    float localT = clampedT / split;
                    float e1 = moveCurve.Evaluate(localT);
                    if (useLShape)
                    {
                        float newX = Mathf.LerpUnclamped(from.x, midPos.x, e1);
                        pos = new Vector2(newX, from.y);
                    }
                    else
                    {
                        pos = Vector2.LerpUnclamped(from, midPos, e1);
                    }
                }
                else
                {
                    float localT = (clampedT - split) / (1f - split);
                    float e2 = moveCurve.Evaluate(localT);
                    if (useLShape)
                    {
                        float newY = Mathf.LerpUnclamped(midPos.y, target.y, e2);
                        pos = new Vector2(midPos.x, newY);
                    }
                    else
                    {
                        pos = Vector2.LerpUnclamped(midPos, target, e2);
                        if (verticalFromBelow) pos.x = midPos.x;
                    }
                }
            }
            else
            {
                pos = Vector2.LerpUnclamped(from, target, ease);
                if (verticalFromBelow)
                {
                    if (forcePureVertical)
                        pos.x = target.x;
                    else
                        pos.x = from.x;
                }
            }
            rect.anchoredPosition = pos;
            yield return null;
        }

        var cell = gridController.GetCell(x, y);
        if (cell != null)
        {
            Vector3 preReparentWorld = rect.TransformPoint(Vector3.zero);
            rect.SetParent(cell, false);
            SnapTileToCell(rect, cell);
            ApplySprite(tile, tile.Value); // ensure correct sprite after resize
            if (verboseAlignmentDebug)
            {
                Vector3 postReparentWorld = rect.TransformPoint(Vector3.zero);
            }
        }
        if (tile != null)
        {
            _tiles[(x, y)] = tile;
            var baseScale = rect.localScale;
            rect.localScale = baseScale * 1.15f;
            float popT = 0f;
            while (popT < 1f)
            {
                popT += Time.unscaledDeltaTime / 0.15f;
                rect.localScale = Vector3.Lerp(baseScale * 1.15f, baseScale, popT);
                yield return null;
            }
            rect.localScale = baseScale;
            EventManager.TileSpawnAnimationComplete(x, y);
            if (verboseAlignmentDebug)
            {
                Vector3 finalWorld = rect.TransformPoint(Vector3.zero);
                DebugLog($"[ALIGN] Finished tile({x},{y}) finalWorld={finalWorld} targetWorld={worldTarget} deltaWorld={(finalWorld - worldTarget)}");
            }
        }
        _isAnimatingSpawn = false;
    }
    #endregion

    private void SnapTileToCell(RectTransform tileRect, RectTransform cell)
    {
        if (tileRect == null || cell == null) return;
        if (resizeToCell)
        {
            tileRect.anchorMin = tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            var targetSize = cell.rect.size - sizePadding;
            tileRect.sizeDelta = targetSize;
        }
        if (centerTileInCell)
        {
            tileRect.anchorMin = tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.anchoredPosition = Vector2.zero;
        }
    }

    private void DebugLog(string msg)
    {
        if (enableDebug)
            Debug.Log($"[TileSystem] {msg}");
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

    private void ApplySprite(TileView tile, int value)
    {
        if (tile == null || brickSet == null) return;
        var sprite = brickSet.GetSprite(value);
        if (sprite != null) tile.SetSprite(sprite);
    }
}


