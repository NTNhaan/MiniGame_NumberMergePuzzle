using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DefaultNamespace;

#if false
public class MissionController { public static MissionController Instance; public int CurrentMission; public void TryUnlock(int v){} }
public class BoosterTileClick : UnityEngine.MonoBehaviour {}
#endif

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
    [SerializeField] private bool optimizeClusters = true; // giảm GC khi tìm cluster

    [Header("Scoring")]
    [SerializeField] private bool addScoreOnMerge = true;
    [SerializeField] private ScoreAwardMode scoreMode = ScoreAwardMode.NewValue;
    [SerializeField] private int clusterBonusMultiplier = 0;

    [Header("Animator Character")]
    [SerializeField] private Animator mewAnimator;
    private bool _smileRunning;
    private enum ScoreAwardMode
    {
        NewValue,
        GainedValue,
        BaseValueTimesCluster,
        SumOfMergedTiles
    }

    public GridController Grid => gridController;


    #region Animated Spawn
    [Header("Animation")]
    [SerializeField] private float moveDuration = DataConfig.TILE_MOVE_DURATION;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool _isAnimatingSpawn;
    private bool _isMerging;
    public bool Busy => _isAnimatingSpawn || _isMerging;
    public bool IsBusy() => Busy;

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

    [Header("Booster Swap Settings")]
    [Tooltip("Delay (seconds) before checking auto-merge after a booster Swap.")]
    [SerializeField] private float swapMergeDelay = 0.4f;
    [SerializeField] private bool preserveQueueStart = DataConfig.TILE_PRESERVE_QUEUE_START;
    [SerializeField] private bool useRootCanvasForQueueStart = DataConfig.TILE_USE_ROOT_CANVAS_FOR_QUEUE_START;

    [Header("Merge Timing")]
    [Tooltip("Duration (seconds) for each absorb animation step in a merge chain.")]
    [SerializeField] private float mergeAbsorbDuration = 0.25f;
    [Tooltip("Delay (seconds) inserted after each merge iteration (except maybe last) to keep pace consistent.")]
    [SerializeField] private float mergeInterDelay = 0.08f;
    [Tooltip("If true, enforce same duration regardless of delta hiccups by using unscaled time.")]
    [SerializeField] private bool mergeUseUnscaledTime = true;
    [Tooltip("If true, log start of each merge iteration for tuning.")]
    [SerializeField] private bool debugMergeTiming = false;
    [Tooltip("Automatically attempt merges after any column compaction/move.")]
    [SerializeField] private bool autoMergeAfterCompaction = true;
    private bool _pendingAutoMerge;
    // Reusable buffers
    private readonly List<RectTransform> _mergeMovers = new(32);
    private readonly Dictionary<RectTransform, Vector3> _mergeStartScale = new();
    private readonly Dictionary<RectTransform, Vector3> _mergeStartPos = new();
    private readonly List<TileView> _compactionTemp = new(32);
    private readonly HashSet<int> _pendingCompactionColumns = new();
    private bool _compactionQueued;
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

    private void Start()
    {
        // Register any pre-placed tiles in the grid (from prefab/scene)
        RegisterExistingTilesInScene();
        // If board full at start, only game over if no merge is possible
        if (IsBoardStuck())
        {
            var popup = FindFirstObjectByType<PopupController>();
            if (popup != null) popup.ShowGameOverPopUp();
        }
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
            if (peek.transform.localScale.sqrMagnitude < 0.0001f)
                peek.transform.localScale = Vector3.one;
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
        EnsureBoosterClickable(tile);
        _tiles[(x, y)] = tile;
        return tile;
    }

    public bool SpawnTileAnimatedFromQueue(int column, int value, Color color, RectTransform startRect)
    {
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
        if (targetRow < 0)
        {
            // Column is full
            return false;
        }
        var cell = gridController.GetCell(column, targetRow);

        var tile = GetFromPool();
        tile.Initialize(value, color, column, targetRow);
        ApplySprite(tile, value);
        EnsureBoosterClickable(tile);

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

        if (startRect.Equals(null))
        {
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
            ApplySprite(tile, tile.Value);
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
            if (Audio.AudioController.Instance != null)
                Audio.AudioController.Instance.PlayPutDownShape();
            // If board looks full, verify no merges exist before game over
            if (IsBoardStuck())
            {
                var popup = FindFirstObjectByType<PopupController>();
                if (popup != null) popup.ShowGameOverPopUp();
            }
            // Ensure column compaction after spawn to push tiles to top
            ScheduleCompactionColumn(x);
            if (verboseAlignmentDebug)
            {
                Vector3 finalWorld = rect.TransformPoint(Vector3.zero);
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

    private void OnEnable()
    {
        EventManager.OnTileSpawnAnimationComplete += HandleTileSpawnedForMerge;
    }

    private void OnDisable()
    {
        EventManager.OnTileSpawnAnimationComplete -= HandleTileSpawnedForMerge;
    }

    private void HandleTileSpawnedForMerge(int x, int y)
    {
        var tile = GetTile(x, y);
        if (tile == null) return;
        int value = tile.Value;
        var cluster = GetClusterPositions(x, y, value);
        if (cluster.Count < 2) return;
        if (_isMerging) return;
        StartCoroutine(MergeClusterChain(tile));
    }

    private List<(int x, int y)> CollectClusterDFS(int sx, int sy, int targetValue)
    {
        var results = new List<(int x, int y)>();
        var visited = new HashSet<(int, int)>();
        void DFS(int cx, int cy)
        {
            if (!IsInside(cx, cy)) return;
            if (visited.Contains((cx, cy))) return;
            var t = GetTile(cx, cy);
            if (t == null || t.Value != targetValue) return;
            visited.Add((cx, cy));
            results.Add((cx, cy));
            DFS(cx + 1, cy);
            DFS(cx - 1, cy);
            DFS(cx, cy + 1);
            DFS(cx, cy - 1);
        }
        DFS(sx, sy);
        return results;
    }

    // Optimized non-alloc style cluster finder (reuse buffers)
    private readonly List<(int x, int y)> _clusterTemp = new(64);
    private bool[,] _visitedGrid;
    private Stack<(int x, int y)> _dfsStack;
    private void EnsureVisitedBuffers()
    {
        if (gridController == null) return;
        if (_visitedGrid == null || _visitedGrid.GetLength(0) != gridController.Columns || _visitedGrid.GetLength(1) != gridController.Rows)
        {
            _visitedGrid = new bool[gridController.Columns, gridController.Rows];
        }
        if (_dfsStack == null) _dfsStack = new Stack<(int x, int y)>(64);
    }
    private List<(int x, int y)> GetClusterPositions(int sx, int sy, int targetValue)
    {
        if (!optimizeClusters) return CollectClusterDFS(sx, sy, targetValue);
        EnsureVisitedBuffers();
        _clusterTemp.Clear();
        if (_visitedGrid == null) return _clusterTemp;
        for (int i = 0; i < gridController.Columns; i++)
            for (int j = 0; j < gridController.Rows; j++)
                _visitedGrid[i, j] = false;
        _dfsStack.Clear();
        if (!IsInside(sx, sy)) return _clusterTemp;
        _dfsStack.Push((sx, sy));
        while (_dfsStack.Count > 0)
        {
            var (cx, cy) = _dfsStack.Pop();
            if (!IsInside(cx, cy)) continue;
            if (_visitedGrid[cx, cy]) continue;
            var t = GetTile(cx, cy);
            if (t == null || t.Value != targetValue) continue;
            _visitedGrid[cx, cy] = true;
            _clusterTemp.Add((cx, cy));
            _dfsStack.Push((cx + 1, cy));
            _dfsStack.Push((cx - 1, cy));
            _dfsStack.Push((cx, cy + 1));
            _dfsStack.Push((cx, cy - 1));
        }
        return _clusterTemp;
    }

    private IEnumerator MergeClusterChain(TileView baseTile)
    {
        if (baseTile == null) yield break;
        _isMerging = true;
        int safety = 64;
        while (safety-- > 0)
        {
            var cluster = GetClusterPositions(baseTile.X, baseTile.Y, baseTile.Value);
            if (cluster.Count < 2) break;
            TileView anchor = baseTile;
            foreach (var pos in cluster)
            {
                if (_tiles.TryGetValue((pos.x, pos.y), out var cand) && cand != null)
                {
                    bool better = false;
                    if (cand.Y > anchor.Y) better = true;
                    else if (cand.Y == anchor.Y && cand.X < anchor.X) better = true;
                    if (better) anchor = cand;
                }
            }
            // Choose anchor as the tile with most orthogonal neighbors within the cluster (center of L/T shapes)
            {
                var clusterSet = new HashSet<(int, int)>();
                foreach (var p in cluster) clusterSet.Add((p.x, p.y));
                TileView best = anchor;
                int bestDeg = -1;
                foreach (var pos in cluster)
                {
                    int deg = 0;
                    if (clusterSet.Contains((pos.x + 1, pos.y))) deg++;
                    if (clusterSet.Contains((pos.x - 1, pos.y))) deg++;
                    if (clusterSet.Contains((pos.x, pos.y + 1))) deg++;
                    if (clusterSet.Contains((pos.x, pos.y - 1))) deg++;
                    if (_tiles.TryGetValue((pos.x, pos.y), out var tv) && tv != null)
                    {
                        if (deg > bestDeg)
                        {
                            best = tv; bestDeg = deg;
                        }
                        else if (deg == bestDeg)
                        {
                            // Tie-breaker: prefer lower (greater Y), then leftmost (smaller X)
                            bool better = false;
                            if (tv.Y > best.Y) better = true;
                            else if (tv.Y == best.Y && tv.X < best.X) better = true;
                            if (better) { best = tv; }
                        }
                    }
                }
                anchor = best;
                baseTile = anchor;
            }
            var anchorRect = (RectTransform)anchor.transform;
            if (addScoreOnMerge && mewAnimator != null && !_smileRunning)
            {
                StartCoroutine(PlaySmileAnimation("SmileAnim"));
            }
            Vector3 anchorPos = anchorRect.position;
            float absorbDuration = Mathf.Max(0.05f, mergeAbsorbDuration);
            _mergeMovers.Clear();
            _mergeStartScale.Clear();
            _mergeStartPos.Clear();
            for (int ci = 0; ci < cluster.Count; ci++)
            {
                var pos = cluster[ci];
                if (_tiles.TryGetValue((pos.x, pos.y), out var tv) && tv != null)
                {
                    var r = (RectTransform)tv.transform;
                    _mergeStartScale[r] = r.localScale;
                    _mergeStartPos[r] = r.position;
                    if (tv != anchor) _mergeMovers.Add(r);
                }
            }
            if (debugMergeTiming)
                DebugLog($"Merge iteration start value={anchor.Value} clusterSize={cluster.Count} duration={absorbDuration:F2}");
            float t = 0f;
            while (t < 1f)
            {
                float dt = mergeUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt / absorbDuration;
                float e = Mathf.Clamp01(t);
                e = Mathf.SmoothStep(0, 1, e);
                float pulse = Mathf.Sin(e * Mathf.PI);
                anchorRect.localScale = Vector3.Lerp(_mergeStartScale[anchorRect], _mergeStartScale[anchorRect] * 1.15f, pulse);
                for (int mi = 0; mi < _mergeMovers.Count; mi++)
                {
                    var r = _mergeMovers[mi];
                    if (r == null) continue;
                    Vector3 s = _mergeStartPos[r];
                    Vector3 target = anchorPos;
                    // Always move in a straight line towards the anchor
                    Vector3 newPos = Vector3.LerpUnclamped(s, target, e);
                    r.position = newPos;
                    r.localScale = Vector3.Lerp(_mergeStartScale[r], Vector3.zero, e);
                }
                yield return null;
            }
            anchorRect.position = anchorPos;
            anchorRect.localScale = _mergeStartScale[anchorRect];
            foreach (var pos in cluster)
            {
                if (pos.x == anchor.X && pos.y == anchor.Y) continue;
                if (_tiles.TryGetValue((pos.x, pos.y), out var mergeTile) && mergeTile != null)
                {
                    _tiles.Remove((pos.x, pos.y));
                    ReturnToPool(mergeTile);
                }
            }
            int oldValue = anchor.Value;
            int newValue = oldValue * 2;
            anchor.UpdateValue(newValue);
            ApplySprite(anchor, newValue);
            if (Audio.AudioController.Instance != null)
                Audio.AudioController.Instance.PlayMergeSound();
            if (addScoreOnMerge)
            {
                AwardScore(oldValue, newValue, cluster.Count);
            }

            var mc = MissionController.Instance != null ? MissionController.Instance : MissionController.Ensure();
            if (mc != null)
            {
                mc.TryUnlock(newValue);
            }

            yield return MoveTileUpwards(anchor);
            // Inter-merge pacing delay (skip if chain finished)
            if (mergeInterDelay > 0f)
            {
                float wait = 0f;
                while (wait < mergeInterDelay)
                {
                    float dt = mergeUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    wait += dt;
                    yield return null;
                }
            }
        }
        yield return CompactAllColumns();
        // After all merges/compactions, only end if stuck (full & no merge)
        if (IsBoardStuck())
        {
            var popup = FindFirstObjectByType<PopupController>();
            if (popup != null) popup.ShowGameOverPopUp();
        }
        _isMerging = false;
    }

    // Returns true if the board is full AND there is no adjacent pair that can merge
    private bool IsBoardStuck()
    {
        if (HasAnyEmptyCell()) return false; // not full -> not stuck
        return !HasAnyMergeAvailable();
    }

    private bool HasAnyMergeAvailable()
    {
        if (gridController == null) return false;
        // Check each tile's right and up neighbor (avoid double check)
        for (int x = 0; x < gridController.Columns; x++)
        {
            for (int y = 0; y < gridController.Rows; y++)
            {
                var tile = GetTile(x, y);
                if (tile == null) continue;
                int v = tile.Value;
                // Right neighbor
                if (x + 1 < gridController.Columns)
                {
                    var right = GetTile(x + 1, y);
                    if (right != null && right.Value == v) return true;
                }
                // Up neighbor
                if (y + 1 < gridController.Rows)
                {
                    var up = GetTile(x, y + 1);
                    if (up != null && up.Value == v) return true;
                }
            }
        }
        return false;
    }

    private int GetHighestEmptyRow(int col)
    {
        for (int y = 0; y < gridController.Rows; y++)
        {
            if (!_tiles.ContainsKey((col, y))) return y;
        }
        return -1;
    }

    private System.Collections.IEnumerator MoveTileUpwards(TileView tile)
    {
        if (tile == null) yield break;
        int currentY = tile.Y;
        int targetY = GetHighestEmptyRow(tile.X);
        if (targetY < 0 || targetY >= currentY) yield break;
        _tiles.Remove((tile.X, currentY));
        _tiles[(tile.X, targetY)] = tile;
        tile.SetGridPosition(tile.X, targetY);
        var targetCell = gridController.GetCell(tile.X, targetY);
        if (targetCell != null)
        {
            var rect = (RectTransform)tile.transform;
            Vector3 start = rect.position;
            Vector3 end = targetCell.TransformPoint(targetCell.rect.center);
            float moveT = 0f; float moveDur = 0.22f;
            while (moveT < 1f)
            {
                moveT += Time.unscaledDeltaTime / moveDur;
                float ee = Mathf.SmoothStep(0, 1, Mathf.Clamp01(moveT));
                rect.position = Vector3.Lerp(start, end, ee);
                yield return null;
            }
            rect.SetParent(targetCell, false);
            SnapTileToCell(rect, targetCell);
        }
    }

    private IEnumerator CompactAllColumns()
    {
        if (gridController == null) yield break;
        for (int col = 0; col < gridController.Columns; col++)
        {
            yield return CompactColumn(col);
        }
        if (autoMergeAfterCompaction) ScheduleAutoMergeScan();
    }

    private IEnumerator CompactColumn(int col)
    {
        _compactionTemp.Clear();
        for (int y = 0; y < gridController.Rows; y++)
        {
            if (_tiles.TryGetValue((col, y), out var t) && t != null) _compactionTemp.Add(t);
        }
        if (_compactionTemp.Count == 0) yield break;
        if (_compactionTemp.Count == 1)
        {
            var only = _compactionTemp[0];
            if (only.Y != 0) yield return MoveTileToRow(only, 0);
            yield break;
        }
        int nextY = 0;
        for (int i = 0; i < _compactionTemp.Count; i++)
        {
            var tile = _compactionTemp[i];
            if (tile.Y != nextY) yield return MoveTileToRow(tile, nextY);
            nextY++;
        }
        if (autoMergeAfterCompaction) ScheduleAutoMergeScan();
    }

    private void ScheduleCompactionColumn(int col)
    {
        if (gridController == null) return;
        if (col < 0 || col >= gridController.Columns) return;
        _pendingCompactionColumns.Add(col);
        if (!_compactionQueued)
        {
            _compactionQueued = true;
            StartCoroutine(ProcessCompactionsEndOfFrame());
        }
    }

    private IEnumerator ProcessCompactionsEndOfFrame()
    {
        yield return null; // coalesce same-frame requests
        _compactionQueued = false;
        var cols = new List<int>(_pendingCompactionColumns);
        _pendingCompactionColumns.Clear();
        for (int i = 0; i < cols.Count; i++)
        {
            yield return CompactColumn(cols[i]);
        }
    }

    private IEnumerator MoveTileToRow(TileView tile, int targetY)
    {
        if (tile == null) yield break;
        int startY = tile.Y;
        if (startY == targetY) yield break;
        int x = tile.X;
        if (_tiles.ContainsKey((x, startY))) _tiles.Remove((x, startY));
        _tiles[(x, targetY)] = tile;
        tile.SetGridPosition(x, targetY);
        var targetCell = gridController.GetCell(x, targetY);
        if (targetCell != null)
        {
            var rect = (RectTransform)tile.transform;
            Vector3 start = rect.position;
            Vector3 end = targetCell.TransformPoint(targetCell.rect.center);
            float t = 0f; float dur = 0.18f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;
                float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
                rect.position = Vector3.Lerp(start, end, e);
                yield return null;
            }
            rect.SetParent(targetCell, false);
            SnapTileToCell(rect, targetCell);
        }
    }

    public void MergeAllClustersFullPass()
    {
        if (_isMerging) return;
        StartCoroutine(MergeAllClustersCoroutine());
    }

    private System.Collections.IEnumerator MergeAllClustersCoroutine()
    {
        _isMerging = true;
        bool changed;
        int safety = 128;
        do
        {
            changed = false;
            var keys = new List<(int x, int y)>(_tiles.Keys);
            foreach (var k in keys)
            {
                if (!_tiles.ContainsKey(k)) continue;
                var tile = _tiles[k];
                if (tile == null) continue;
                var cluster = GetClusterPositions(tile.X, tile.Y, tile.Value);
                if (cluster.Count < 2) continue;
                yield return MergeClusterChain(tile);
                changed = true;
                break;
            }
            yield return null;
        } while (changed && safety-- > 0);
        _isMerging = false;
    }

    public bool IsInside(int x, int y) => x >= 0 && x < gridController.Columns && y >= 0 && y < gridController.Rows;

    public bool IsEmpty(int x, int y) => !_tiles.ContainsKey((x, y));

    public bool HasEmptyInColumn(int column)
    {
        if (gridController == null) return false;
        if (column < 0 || column >= gridController.Columns) return false;
        for (int y = 0; y < gridController.Rows; y++)
        {
            if (IsEmpty(column, y)) return true;
        }
        return false;
    }

    public bool HasAnyEmptyCell()
    {
        if (gridController == null) return false;
        for (int x = 0; x < gridController.Columns; x++)
        {
            for (int y = 0; y < gridController.Rows; y++)
            {
                if (IsEmpty(x, y)) return true;
            }
        }
        return false;
    }

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
        tile.transform.localScale = Vector3.one;
        _pool.Enqueue(tile);
    }

    private void ApplySprite(TileView tile, int value)
    {
        if (tile == null || brickSet == null) return;
        var sprite = brickSet.GetSprite(value);
        if (sprite != null) tile.SetSprite(sprite);
    }

    private void RegisterExistingTilesInScene()
    {
        if (gridController == null) return;
        // Clear any stale dict
        _tiles.Clear();
        for (int y = 0; y < gridController.Rows; y++)
        {
            for (int x = 0; x < gridController.Columns; x++)
            {
                var cell = gridController.GetCell(x, y);
                if (cell == null) continue;
                // Expect at most one TileView per cell
                var tv = cell.GetComponentInChildren<TileView>();
                if (tv == null) continue;
                tv.SetGridPosition(x, y);
                ApplySprite(tv, tv.Value);
                _tiles[(x, y)] = tv;
            }
        }
    }

    private void EnsureBoosterClickable(TileView tile)
    {
        if (tile == null) return;
        if (tile.GetComponent<BoosterTileClick>() == null)
        {
            tile.gameObject.AddComponent<BoosterTileClick>();
        }
    }

    // ===== Booster public APIs =====
    public bool TryDestroyTile(TileView tile)
    {
        if (tile == null) return false;
        if (_isMerging || _isAnimatingSpawn) return false;
        if (!_tiles.ContainsKey((tile.X, tile.Y))) return false;
        _tiles.Remove((tile.X, tile.Y));
        ReturnToPool(tile);
        StartCoroutine(CompactAllColumns());
        return true;
    }

    public bool TrySwapTiles(TileView a, TileView b)
    {
        if (a == null || b == null) return false;
        if (_isMerging || _isAnimatingSpawn) return false;
        if (!_tiles.ContainsKey((a.X, a.Y)) || !_tiles.ContainsKey((b.X, b.Y))) return false;
        // Hoán đổi giá trị (và sprite) giữa 2 tile, toạ độ giữ nguyên.
        int tempVal = a.Value;
        a.UpdateValue(b.Value);
        ApplySprite(a, a.Value);
        b.UpdateValue(tempVal);
        ApplySprite(b, b.Value);
        // Đợi một khoảng ngắn rồi mới kiểm tra merge để user thấy rõ thao tác swap.
        StartCoroutine(SwapCheckAfterDelay(a, b));
        return true;
    }

    public bool TryMergePair(TileView a, TileView b)
    {
        if (a == null || b == null) return false;
        if (a == b) return false;
        if (_isMerging || _isAnimatingSpawn) return false;
        if (a.Value != b.Value) return false;
        if (!_tiles.ContainsKey((a.X, a.Y)) || !_tiles.ContainsKey((b.X, b.Y))) return false;

        // Thực hiện merge cặp và chạy chuỗi xử lý hoàn tất + auto-chain
        StartCoroutine(BoosterMergeFollowUp(a, b));
        return true;
    }

    private IEnumerator BoosterMergeFollowUp(TileView a, TileView b)
    {
        // Merge logic: keep 'a' as anchor, remove 'b'
        int oldVal = a.Value;
        int newVal = oldVal * 2;
        _tiles.Remove((b.X, b.Y));
        ReturnToPool(b);
        a.UpdateValue(newVal);
        ApplySprite(a, newVal);
        if (Audio.AudioController.Instance != null)
            Audio.AudioController.Instance.PlayMergeSound();
        // mission & score
        var mc = MissionController.Instance != null ? MissionController.Instance : MissionController.Ensure();
        mc?.TryUnlock(newVal);
        if (ScoreController.Instance != null)
        {
            ScoreController.Instance.AddPoints(newVal);
        }
        // Complete movement and compaction first
        yield return MoveTileUpwards(a);
        yield return CompactAllColumns();
        // Then auto-merge if the new tile has an adjacent identical neighbor
        var cluster = GetClusterPositions(a.X, a.Y, a.Value);
        if (cluster.Count >= 2 && !_isMerging)
        {
            yield return MergeClusterChain(a);
        }
    }

    private void AwardScore(int oldValue, int newValue, int clusterSize)
    {
        if (ScoreController.Instance == null) return;
        int points = 0;
        switch (scoreMode)
        {
            case ScoreAwardMode.NewValue:
                points = newValue;
                break;
            case ScoreAwardMode.GainedValue:
                points = newValue - oldValue;
                break;
            case ScoreAwardMode.BaseValueTimesCluster:
                points = oldValue * clusterSize;
                break;
            case ScoreAwardMode.SumOfMergedTiles:
                points = oldValue * clusterSize;
                break;
        }
        if (clusterBonusMultiplier > 0 && clusterSize > 1)
        {
            points += (clusterSize - 1) * clusterBonusMultiplier;
        }
        if (points > 0)
            ScoreController.Instance.AddPoints(points);
    }

    private IEnumerator PlaySmileAnimation(string stateName, float enterTimeout = 1.5f, float maxDuration = 3f)
    {
        if (mewAnimator == null || _smileRunning) yield break;
        _smileRunning = true;
        mewAnimator.SetBool("isSmile", true);
        float start = Time.unscaledTime;
        bool entered = false;
        while (Time.unscaledTime - start < enterTimeout)
        {
            var info = mewAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName)) { entered = true; break; }
            yield return null;
        }
        if (!entered)
        {
            DebugLog($"SmileAnim không vào state trong {enterTimeout}s");
            mewAnimator.SetBool("isSmile", false);
            _smileRunning = false;
            yield break;
        }
        start = Time.unscaledTime;
        while (Time.unscaledTime - start < maxDuration)
        {
            var info = mewAnimator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName)) break;
            if (info.normalizedTime >= 1f && !info.loop) break;
            yield return null;
        }
        mewAnimator.SetBool("isSmile", false);
        _smileRunning = false;
    }

    // Delay check after swap so player perceives the swap before merge animation fires
    private IEnumerator SwapCheckAfterDelay(TileView a, TileView b)
    {
        float t = 0f;
        while (t < swapMergeDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_isMerging || _isAnimatingSpawn) yield break;
        // Validate tiles still exist and haven't changed value drastically (optional)
        if (a == null || b == null) yield break;
        if (!_tiles.ContainsKey((a.X, a.Y)) || !_tiles.ContainsKey((b.X, b.Y))) yield break;
        bool shouldMerge = false;
        var clusterA = GetClusterPositions(a.X, a.Y, a.Value);
        if (clusterA.Count >= 2) shouldMerge = true;
        var clusterB = GetClusterPositions(b.X, b.Y, b.Value);
        if (clusterB.Count >= 2) shouldMerge = true;
        if (shouldMerge && !_isMerging)
        {
            if (enableDebug)
                DebugLog($"Swap delayed merge triggered (delay={swapMergeDelay:F2}) A({a.X},{a.Y})={a.Value} B({b.X},{b.Y})={b.Value}");
            MergeAllClustersFullPass();
        }
    }

    private void ScheduleAutoMergeScan()
    {
        if (_pendingAutoMerge) return; // đã có lịch
        _pendingAutoMerge = true; // đánh dấu trước
        StartCoroutine(AutoMergeScanNextFrame());
    }

    private IEnumerator AutoMergeScanNextFrame()
    {
        // Đảm bảo dictionary đã cập nhật vị trí (frame tiếp theo)
        yield return null;
        // Chờ đến khi không còn spawn hoặc merge đang chạy để tránh bỏ sót cụm
        int safety = 180; // ~3s ở 60fps
        while ((_isMerging || _isAnimatingSpawn) && safety-- > 0)
            yield return null;
        _pendingAutoMerge = false; // sắp chạy scan thực
        if (_isMerging || _isAnimatingSpawn) yield break; // vẫn đang bận -> bỏ (scan khác sẽ được schedule nếu còn compaction)
        var keys = new List<(int x, int y)>(_tiles.Keys);
        foreach (var k in keys)
        {
            if (!_tiles.ContainsKey(k)) continue;
            var tv = _tiles[k]; if (tv == null) continue;
            var cluster = GetClusterPositions(tv.X, tv.Y, tv.Value);
            if (cluster.Count >= 2)
            {
                StartCoroutine(MergeClusterChain(tv));
                // Nếu còn khả năng xuất hiện cụm khác sau chain này, lên lịch lại.
                if (autoMergeAfterCompaction)
                {
                    ScheduleAutoMergeScan();
                }
                break;
            }
        }
    }
}


