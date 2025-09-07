using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DefaultNamespace; // ScoreController

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

    [Header("Scoring")]
    [SerializeField] private bool addScoreOnMerge = true;
    [SerializeField] private ScoreAwardMode scoreMode = ScoreAwardMode.NewValue;
    [SerializeField] private int clusterBonusMultiplier = 0;

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
        var cell = gridController.GetCell(column, targetRow);

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
        var cluster = CollectClusterDFS(x, y, value);
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

    private System.Collections.IEnumerator MergeClusterChain(TileView baseTile)
    {
        if (baseTile == null) yield break;
        _isMerging = true;
        int safety = 64;
        while (safety-- > 0)
        {
            var cluster = CollectClusterDFS(baseTile.X, baseTile.Y, baseTile.Value);
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
            if (anchor != baseTile)
            {
                baseTile = anchor;
            }
            var anchorRect = (RectTransform)anchor.transform;
            Vector3 anchorPos = anchorRect.position;
            float absorbDuration = 0.18f;
            var movers = new List<RectTransform>();
            var startScale = new Dictionary<RectTransform, Vector3>();
            var startPos = new Dictionary<RectTransform, Vector3>();
            foreach (var pos in cluster)
            {
                if (_tiles.TryGetValue((pos.x, pos.y), out var tv) && tv != null)
                {
                    var r = (RectTransform)tv.transform;
                    startScale[r] = r.localScale;
                    startPos[r] = r.position;
                    if (tv != anchor) movers.Add(r);
                }
            }
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / absorbDuration;
                float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
                float pulse = Mathf.Sin(e * Mathf.PI);
                anchorRect.localScale = Vector3.Lerp(startScale[anchorRect], startScale[anchorRect] * 1.15f, pulse);
                foreach (var r in movers)
                {
                    if (r == null) continue;
                    Vector3 s = startPos[r];
                    Vector3 target = anchorPos;
                    Vector3 mid1 = s;
                    Vector3 mid2 = s;
                    bool sameX = Mathf.Abs(s.x - target.x) < 0.01f;
                    bool sameY = Mathf.Abs(s.y - target.y) < 0.01f;
                    Vector3 newPos;
                    if (sameX || sameY)
                    {
                        newPos = Vector3.Lerp(s, target, e);
                    }
                    else
                    {
                        float split = 0.5f;
                        if (e < split)
                        {
                            float lerp1 = e / split;
                            float nx = Mathf.Lerp(s.x, target.x, lerp1);
                            newPos = new Vector3(nx, s.y, s.z);
                        }
                        else
                        {
                            float lerp2 = (e - split) / (1f - split);
                            float ny = Mathf.Lerp(s.y, target.y, lerp2);
                            newPos = new Vector3(target.x, ny, s.z);
                        }
                    }
                    r.position = newPos;
                    r.localScale = Vector3.Lerp(startScale[r], Vector3.zero, e);
                }
                yield return null;
            }
            anchorRect.position = anchorPos;
            anchorRect.localScale = startScale[anchorRect];
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
            if (addScoreOnMerge)
            {
                AwardScore(oldValue, newValue, cluster.Count);
            }

            yield return MoveTileUpwards(anchor);
            yield return null;
        }
        _isMerging = false;
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
                var cluster = CollectClusterDFS(tile.X, tile.Y, tile.Value);
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
}


