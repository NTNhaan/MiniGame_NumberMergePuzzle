using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnQueue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileSystem tileSystem;
    [SerializeField] private RectTransform queueContainer;
    [SerializeField] private TileView queueTilePrefab;
    [SerializeField] private int queueSize = DataConfig.QUEUE_SIZE;

    [Header("Value Generation")]
    [SerializeField] private int baseValue = DataConfig.BASE_VALUE;
    [SerializeField] private int[] allowedValues = null;
    [SerializeField] private Color defaultColor = default;

    private readonly List<TileView> _queueVisuals = new();
    private readonly List<Vector3> _baseScales = new();
    private readonly Queue<int> _values = new();

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    [Header("Queue Animation")]
    [SerializeField] private float newItemPopDuration = DataConfig.QUEUE_NEW_ITEM_POP_DURATION;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateQueue = true;
    [SerializeField] private float activeScale = DataConfig.QUEUE_ACTIVE_SCALE; // scale cho block sẵn sàng lên board (bên phải)
    [SerializeField] private float inactiveScale = DataConfig.QUEUE_INACTIVE_SCALE;
    [SerializeField] private float scaleLerpSpeed = DataConfig.QUEUE_SCALE_LERP_SPEED; // tốc độ mượt scale
    [Header("Active Visual Offset")]
    [SerializeField] private float activeYOffset = DataConfig.QUEUE_ACTIVE_Y_OFFSET;
    [SerializeField] private bool offsetActiveOnly = true;
    [SerializeField] private bool applyYOffsetInLateUpdate = true;
    [Header("Debug Scale")]
    [SerializeField] private bool debugActiveScale = false;
    [SerializeField] private bool normalizePrefabScale = true;

    private int cachedCountPrev = -1; // để trigger update scale frame

    private TileView _movingTileVisual; // tile visual đang bay (tạm ẩn khỏi queue list scale)
    private TileView _consumedVisual; // visual đã bị lấy ra (ẩn)
    private bool _freezeHighlight; // khóa cập nhật highlight tới khi anim xong
    private int _currentActiveIndex = -1;
    private Vector2 _activeBasePos; // vị trí gốc (không offset) của ô active

    private void Awake()
    {
        if (tileSystem == null)
            tileSystem = FindFirstObjectByType<TileSystem>();
        EventManager.OnTileSpawnAnimationComplete += HandleSpawnAnimationComplete;
        BuildQueueSlots();
        // Chỉ khởi tạo giá trị khi đang Play để tránh "spawn" số trong Edit Mode
        if (Application.isPlaying)
        {
            FillQueue();
            RefreshVisuals();
            SyncRuntimeQueue();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tránh Destroy/Instantiate trong OnValidate để không sinh duplicate & warning
        if (!Application.isPlaying)
        {
            // Chỉ đồng bộ lại list nếu số lượng child thay đổi (ví dụ user nhân bản thủ công)
            if (queueContainer != null)
            {
                _queueVisuals.Clear();
                _baseScales.Clear();
                for (int i = 0; i < queueContainer.childCount; i++)
                {
                    var child = queueContainer.GetChild(i).GetComponent<TileView>();
                    if (child == null) continue;
                    _queueVisuals.Add(child);
                    if (normalizePrefabScale) child.transform.localScale = Vector3.one;
                    _baseScales.Add(child.transform.localScale);
                }
            }
            // Không fill queue (giữ trống editor), chỉ áp dụng scale placeholder
            ApplyImmediateScales();
            return;
        }
        // Đang play: có thể áp dụng scale trực tiếp
        ApplyImmediateScales();
    }

    [ContextMenu("Force Normalize Scales")]
    private void ForceNormalizeScales()
    {
        if (queueContainer == null) return;
        normalizePrefabScale = true;
        _baseScales.Clear();
        for (int i = 0; i < queueContainer.childCount; i++)
        {
            var child = queueContainer.GetChild(i) as RectTransform;
            if (child == null) continue;
            child.localScale = Vector3.one;
            if (i < _baseScales.Count) _baseScales[i] = child.localScale; else _baseScales.Add(child.localScale);
        }
        ApplyImmediateScales();
        DebugLog("ForceNormalizeScales executed.");
    }

    [ContextMenu("Rebuild Queue Slots")]
    private void ContextRebuildQueue()
    {
        BuildQueueSlots();
        FillQueue();
        RefreshVisuals();
        DebugLog("Queue slots rebuilt via context menu.");
    }

    [ContextMenu("Editor/Clear Queue Slots (No Rebuild)")]
    private void EditorClearQueueSlots()
    {
        if (queueContainer == null) return;
        var toRemove = new List<GameObject>();
        for (int i = 0; i < queueContainer.childCount; i++)
        {
            toRemove.Add(queueContainer.GetChild(i).gameObject);
        }
        foreach (var go in toRemove)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
        _queueVisuals.Clear();
        _baseScales.Clear();
        DebugLog("Editor cleared queue children.");
    }
#endif

    private bool _pendingQueueShift;
    private int _pendingInsertedValue;

    private void BuildQueueSlots()
    {
        if (queueContainer == null || queueTilePrefab == null) return;
        for (int i = queueContainer.childCount - 1; i >= 0; i--)
        {
            var c = queueContainer.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
        }
        _queueVisuals.Clear();
        _baseScales.Clear();
        for (int i = 0; i < queueSize; i++)
        {
            var t = Instantiate(queueTilePrefab, queueContainer);
            t.name = $"QueueTile_{i}";
            if (normalizePrefabScale)
                t.transform.localScale = Vector3.one; // reset để tránh scale chồng
            _queueVisuals.Add(t);
            _baseScales.Add(t.transform.localScale);
        }
    }

    private void FillQueue()
    {
        if (!Application.isPlaying) return; // tránh tạo giá trị khi ở Edit Mode
        if (allowedValues == null || allowedValues.Length == 0)
            allowedValues = DataConfig.ALLOWED_VALUES;
        if (defaultColor == default)
            defaultColor = DataConfig.DEFAULT_TILE_COLOR;
        while (_values.Count < queueSize)
        {
            int v = GenerateValue();
            _values.Enqueue(v);
            DebugLog($"Enqueue value {v}");
        }
        SyncRuntimeQueue();
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
        if (!Application.isPlaying)
        {
            // Trong Edit Mode: chỉ đảm bảo số lượng slot đúng & scale chuẩn hoá, không gán giá trị
            ApplyImmediateScales();
            return;
        }
        int i = 0;
        foreach (var val in _values)
        {
            if (i >= _queueVisuals.Count) break;
            var view = _queueVisuals[i];
            if (view != null)
            {
                if (!view.gameObject.activeSelf) view.gameObject.SetActive(true); // đảm bảo bật lại nếu đã ẩn
                view.Initialize(val, defaultColor, -1, -1);
            }
            i++;
        }
        for (; i < _queueVisuals.Count; i++)
        {
            var view = _queueVisuals[i];
            view.gameObject.SetActive(false);
        }
        cachedCountPrev = _values.Count;
        // Gán scale ngay lập tức (lerp mượt ở Update)
        ApplyImmediateScales();
        SyncRuntimeQueue();
    }

    public bool SpawnIntoColumn(int column)
    {
        if (!Application.isPlaying) return false; // không spawn khi chưa Play
        if (tileSystem == null || tileSystem.Grid == null) return false;
        if (column < 0 || column >= tileSystem.Grid.Columns) return false;
        if (_values.Count == 0) FillQueue();
        // Lấy phần tử bên phải (cuối) làm item spawn
        int value = 0;
        int originalCount = _values.Count;
        if (originalCount > 0)
        {
            var temp = new List<int>(_values); // bảo toàn thứ tự trái -> phải
            value = temp[^1];
            temp.RemoveAt(temp.Count - 1); // bỏ phần tử cuối (đã dùng)
            _values.Clear();
            foreach (var v in temp) _values.Enqueue(v);
        }

        RectTransform startRect = null;
        _movingTileVisual = null;
        if (_queueVisuals.Count > 0)
        {
            // Index của visual đã tiêu thụ chính là originalCount-1 (rightmost trước khi remove)
            int consumedIndex = Mathf.Clamp(originalCount - 1, 0, _queueVisuals.Count - 1);
            var consumed = _queueVisuals[consumedIndex];
            if (consumed != null)
            {
                startRect = (RectTransform)consumed.transform;
                _movingTileVisual = null; // chúng ta tạo tile mới để bay, không tái dùng visual này
                _consumedVisual = consumed;
                // Ẩn visual khỏi queue trong khi tile bay
                _consumedVisual.gameObject.SetActive(false);
            }
        }
        DebugLog($"Attempt spawn (rightmost) value:{value} into column:{column} startRectNull={startRect == null}");
        bool started = tileSystem.SpawnTileAnimatedFromQueue(column, value, defaultColor, startRect);
        if (!started)
        {
            Debug.LogWarning($"Không thể spawn vào cột {column} (cột đầy hoặc đang anim).");
            _values.Enqueue(value); // trả lại giá trị
            RefreshVisuals();
            return false;
        }
        // Ẩn tạm khỏi hệ thống scale bằng cách tạm thời loại ra (disable scaler Update cho item bay)
        _freezeHighlight = true; // không scale item tiếp theo cho tới khi anim xong
        // Trì hoãn dịch queue cho tới khi animation spawn hoàn tất
        _pendingInsertedValue = GenerateValue();
        _pendingQueueShift = true;
        SyncRuntimeQueue();
        return true;
    }

    private void HandleSpawnAnimationComplete(int x, int y)
    {
        if (!Application.isPlaying) return; // an toàn khi thoát Play
        if (!_pendingQueueShift) return;
        _pendingQueueShift = false;
        _movingTileVisual = null; // kết thúc bay
        _consumedVisual = null; // đã shift xong
        int newVal = _pendingInsertedValue;
        var afterTemp = new List<int>(_values);
        _values.Clear();
        _values.Enqueue(newVal);
        foreach (var v in afterTemp) _values.Enqueue(v);
        if (animateQueue)
            StartCoroutine(AnimateQueueRefresh(newVal));
        else
            RefreshVisuals();
        DebugLog($"Queue shifted after anim complete. Inserted {newVal}");
        _freezeHighlight = false;
        SyncRuntimeQueue();
    }

    private void OnDestroy()
    {
        EventManager.OnTileSpawnAnimationComplete -= HandleSpawnAnimationComplete;
    }

    private System.Collections.IEnumerator AnimateQueueRefresh(int newVal)
    {
        // Cập nhật văn bản trước để người chơi thấy dịch
        RefreshVisuals();
        // Pop effect cho ô mới (bên trái - index 0)
        if (_queueVisuals.Count == 0) yield break;
        var newView = _queueVisuals[0];
        if (newView == null) yield break;
        var rt = (RectTransform)newView.transform;
        Vector3 baseScale = rt.localScale;
        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / newItemPopDuration;
            float e = popCurve.Evaluate(Mathf.Clamp01(t));
            rt.localScale = Vector3.LerpUnclamped(Vector3.zero, baseScale, e);
            yield return null;
        }
        rt.localScale = baseScale;
        // Sau pop cần đảm bảo block phải vẫn giữ scale active
        ApplyImmediateScales();
    }

    private void Update()
    {
        // Smooth scale chuyển đổi (trong trường hợp giá trị thay đổi)
        if (_queueVisuals.Count == 0) return;
        if (_freezeHighlight) return; // giữ nguyên scale hiện tại
        int activeIndex = Mathf.Min(_values.Count - 1, _queueVisuals.Count - 1); // rightmost đang sẵn sàng
        for (int i = 0; i < _queueVisuals.Count; i++)
        {
            var v = _queueVisuals[i];
            if (v == null || !v.gameObject.activeSelf) continue;
            float target = (i == activeIndex) ? activeScale : inactiveScale;
            var rt = (RectTransform)v.transform;
            Vector3 baseScale = (i < _baseScales.Count) ? _baseScales[i] : Vector3.one;
            Vector3 desired = baseScale * target;
            rt.localScale = Vector3.Lerp(rt.localScale, desired, Time.unscaledDeltaTime * scaleLerpSpeed);
            // if (debugActiveScale && i == activeIndex)
            // {
            //     Debug.Log($"[SpawnQueue] Active scale target={target} current={rt.localScale} configuredActiveScale={activeScale}");
            // }
        }
    }

    private void LateUpdate()
    {
        if (!applyYOffsetInLateUpdate) return;
        if (_queueVisuals.Count == 0) return;
        if (_freezeHighlight) return;
        int activeIndex = Mathf.Min(_values.Count - 1, _queueVisuals.Count - 1);
        if (activeIndex < 0) return;
        if (activeIndex != _currentActiveIndex)
        {
            // Revert old active (nếu cần)
            if (_currentActiveIndex >= 0 && _currentActiveIndex < _queueVisuals.Count && !offsetActiveOnly)
            {
                var prevRT = (RectTransform)_queueVisuals[_currentActiveIndex].transform;
                var p = prevRT.anchoredPosition; p.y = 0f; prevRT.anchoredPosition = p;
            }
            var newRT = (RectTransform)_queueVisuals[activeIndex].transform;
            _activeBasePos = newRT.anchoredPosition; // chụp vị trí gốc trước offset
            _currentActiveIndex = activeIndex;
        }
        // Cập nhật basePos nếu layout thay đổi (phát hiện nếu y khác với base + offset)
        var rtActive = (RectTransform)_queueVisuals[activeIndex].transform;
        float expectedY = _activeBasePos.y + activeYOffset;
        // Nếu layout vừa reset (chênh lệch lớn và không bằng expected) -> cập nhật basePos
        if (Mathf.Abs(rtActive.anchoredPosition.y - expectedY) > 0.001f && Mathf.Abs(rtActive.anchoredPosition.y - _activeBasePos.y) < Mathf.Abs(activeYOffset) + 0.01f)
        {
            _activeBasePos = rtActive.anchoredPosition; // layout đã reset về base
        }
        var posActive = rtActive.anchoredPosition; posActive.y = _activeBasePos.y + activeYOffset; rtActive.anchoredPosition = posActive;
        if (!offsetActiveOnly)
        {
            for (int i = 0; i < _queueVisuals.Count; i++)
            {
                if (i == activeIndex) continue;
                var rt = (RectTransform)_queueVisuals[i].transform;
                var p = rt.anchoredPosition; if (Mathf.Abs(p.y) > 0.01f) { p.y = 0f; rt.anchoredPosition = p; }
            }
        }
    }

    private void ApplyImmediateScales()
    {
        if (_queueVisuals.Count == 0) return;
        int activeIndex = Mathf.Min(_values.Count - 1, _queueVisuals.Count - 1);
        for (int i = 0; i < _queueVisuals.Count; i++)
        {
            var v = _queueVisuals[i];
            if (v == null) continue;
            if (!v.gameObject.activeSelf) continue;
            var rt = (RectTransform)v.transform;
            float setScale = (i == activeIndex) ? activeScale : inactiveScale;
            Vector3 baseScale = (i < _baseScales.Count) ? _baseScales[i] : Vector3.one;
            rt.localScale = baseScale * setScale;
            if (debugActiveScale && i == activeIndex)
            {
                Debug.Log($"[SpawnQueue] ApplyImmediateScales activeIndex scale={setScale}");
            }
            // Y offset không áp dụng ở đây nữa (chuyển sang LateUpdate)
        }
    }

    private void SyncRuntimeQueue()
    {
        if (!Application.isPlaying) return;
        InGameData.CurrentQueueValues.Clear();
        foreach (var v in _values)
            InGameData.CurrentQueueValues.Add(v);
    }

    private void DebugLog(string msg)
    {
        if (enableDebug)
            Debug.Log($"[SpawnQueue] {msg}");
    }
}

