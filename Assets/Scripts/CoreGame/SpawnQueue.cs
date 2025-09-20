using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if false
public class MissionController { public static MissionController Instance; public int CurrentMission; }
#endif

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
    [Header("Brick Data")]
    [SerializeField] private BrickSet brickSet;
    [Header("Mission Fallback")]
    [SerializeField] private int fallbackStartingMission = 256;

    private readonly List<TileView> _queueVisuals = new();
    private readonly List<Vector3> _baseScales = new();
    private readonly Queue<int> _values = new();

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    [System.Serializable]
    private struct SpawnThreshold
    {
        public int value;          
        public int requiredMission; 
    }

    [System.Serializable]
    private struct SpawnWeight
    {
        public int value; 
        public float weight; 
    }
    [Header("Spawn Restrictions & Weights")]
    [SerializeField, Tooltip("Minimum mission required for certain spawn values.")] private SpawnThreshold[] spawnThresholds = new SpawnThreshold[0];
    [SerializeField, Tooltip("Custom weights for spawn values (after filtering by mission). Higher = more frequent.")] private SpawnWeight[] spawnWeights = new SpawnWeight[0];
    [SerializeField, Tooltip("If true, will auto-populate a sensible default threshold/weight config when arrays empty.")] private bool autoConfigureDefaults = true;
    [SerializeField, Tooltip("Bias multiplier applied to very small values to keep them common.")] private float smallValueBias = 1.0f;
    [SerializeField, Tooltip("Penalty multiplier applied to larger values (>= largeValueStart) to keep them rare.")] private float largeValuePenalty = 0.35f;
    [SerializeField, Tooltip("Values >= this are considered 'large' for penalty.")] private int largeValueStart = 64;

    [Header("Queue Animation")]
    [SerializeField] private float newItemPopDuration = DataConfig.QUEUE_NEW_ITEM_POP_DURATION;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateQueue = true;
    [SerializeField] private float activeScale = DataConfig.QUEUE_ACTIVE_SCALE;
    [SerializeField] private float inactiveScale = DataConfig.QUEUE_INACTIVE_SCALE;
    [SerializeField] private float scaleLerpSpeed = DataConfig.QUEUE_SCALE_LERP_SPEED;
    [Header("Active Visual Offset")]
    [SerializeField] private float activeYOffset = DataConfig.QUEUE_ACTIVE_Y_OFFSET;
    [SerializeField] private bool offsetActiveOnly = true;
    [SerializeField] private bool applyYOffsetInLateUpdate = true;
    [Header("Debug Scale")]
    [SerializeField] private bool debugActiveScale = false;
    [SerializeField] private bool normalizePrefabScale = true;

    private int cachedCountPrev = -1;

    private TileView _movingTileVisual;
    private TileView _consumedVisual;
    private bool _freezeHighlight;
    private int _currentActiveIndex = -1;
    private Vector2 _activeBasePos;

    private void Awake()
    {
        if (tileSystem == null)
            tileSystem = FindFirstObjectByType<TileSystem>();
        EnsureDefaultSpawnConfig();
        EventManager.OnTileSpawnAnimationComplete += HandleSpawnAnimationComplete;
        EventManager.OnMissionChange += HandleMissionChanged;
        EventManager.OnSpawnValueUnlocked += HandleSpawnValueUnlocked;
        BuildQueueSlots();
        if (Application.isPlaying)
        {
            FillQueue();
            RefreshVisuals();
            SyncRuntimeQueue();
            StartCoroutine(DelayedMissionQueueSync());
        }
    }

    private void OnDestroy()
    {
        EventManager.OnTileSpawnAnimationComplete -= HandleSpawnAnimationComplete;
        EventManager.OnMissionChange -= HandleMissionChanged;
        EventManager.OnSpawnValueUnlocked -= HandleSpawnValueUnlocked;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
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
            ApplyImmediateScales();
            return;
        }
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
    }

    [ContextMenu("Rebuild Queue Slots")]
    private void ContextRebuildQueue()
    {
        BuildQueueSlots();
        FillQueue();
        RefreshVisuals();
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
    }
#endif

    private bool _pendingQueueShift;
    private int _pendingInsertedValue;
    [Header("Input Lock")]
    [SerializeField] private bool lockDuringSpawnAndMerge = true;
    private bool _inputLocked;

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
                t.transform.localScale = Vector3.one;
            _queueVisuals.Add(t);
            _baseScales.Add(t.transform.localScale);
        }
    }

    private void FillQueue()
    {
        if (!Application.isPlaying) return;
        if (allowedValues == null || allowedValues.Length == 0)
            allowedValues = DataConfig.ALLOWED_VALUES;
        if (defaultColor == default)
            defaultColor = DataConfig.DEFAULT_TILE_COLOR;
        while (_values.Count < queueSize)
        {
            int v = GenerateValue();
            _values.Enqueue(v);
        }
        SyncRuntimeQueue();
    }

    private int GenerateValue()
    {
        int missionCap = (MissionController.Instance != null && MissionController.Instance.CurrentMission > 0)
            ? MissionController.Instance.CurrentMission
            : fallbackStartingMission;

        var mc = MissionController.Instance;
        List<int> candidates = new();
        if (mc != null && mc.UnlockedValues != null)
        {
            foreach (var v in mc.UnlockedValues)
            {
                if (v < missionCap && PassThreshold(v, missionCap))
                    candidates.Add(v);
            }
        }

        if (candidates.Count == 0)
        {
            int[] seedCandidates = { 2, 4, 8, 16 };
            foreach (var s in seedCandidates)
            {
                if (s < missionCap && PassThreshold(s, missionCap)) candidates.Add(s);
            }
        }

        if (candidates.Count == 0)
        {
            int fb = baseValue;
            while ((fb >= missionCap || !PassThreshold(fb, missionCap)) && fb > 2)
                fb /= 2;
            if (fb < 2) fb = 2;
            candidates.Add(fb);
        }

        float total = 0f;
        var weightsTmp = new List<float>(candidates.Count);
        foreach (var v in candidates)
        {
            float w = GetWeightFor(v);
            weightsTmp.Add(w);
            total += w;
        }
        float r = Random.value * total;
        float accum = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            accum += weightsTmp[i];
            if (r <= accum)
            {
                if (enableDebug)
                {
                    Debug.Log($"[SpawnQueue] Weighted pick missionCap={missionCap} candidates=[{string.Join(",", candidates)}] weights=[{string.Join(",", weightsTmp)}] => {candidates[i]}");
                }
                return candidates[i];
            }
        }
        return candidates[^1];
    }

    private bool PassThreshold(int value, int missionCap)
    {
        if (spawnThresholds == null) return true;
        for (int i = 0; i < spawnThresholds.Length; i++)
        {
            if (spawnThresholds[i].value == value)
            {
                return missionCap >= spawnThresholds[i].requiredMission;
            }
        }
        return true;
    }

    private float GetWeightFor(int value)
    {
        if (spawnWeights != null)
        {
            for (int i = 0; i < spawnWeights.Length; i++)
            {
                if (spawnWeights[i].value == value)
                {
                    float w = spawnWeights[i].weight;
                    if (value >= largeValueStart) w *= largeValuePenalty;
                    if (value <= 8) w *= smallValueBias;
                    if (w <= 0f) w = 0.01f;
                    return w;
                }
            }
        }
        float baseW = 1f / Mathf.Max(1, Mathf.Log(value, 2)); 
        if (value >= largeValueStart) baseW *= largeValuePenalty;
        if (value <= 8) baseW *= smallValueBias;
        if (baseW <= 0f) baseW = 0.01f;
        return baseW;
    }

    private void HandleMissionChanged(int newMissionTarget)
    {
        if (!Application.isPlaying) return;
        RefilterQueueForMission(newMissionTarget);
    }

    private void RefilterQueueForMission(int missionTarget)
    {
        if (missionTarget <= 0)
        {
            missionTarget = (MissionController.Instance != null && MissionController.Instance.StartingMission > 0)
                ? MissionController.Instance.StartingMission
                : fallbackStartingMission;
        }
        var kept = new Queue<int>();
        var mc = MissionController.Instance;
        foreach (var v in _values)
        {
            if (v < missionTarget && (mc == null || mc.UnlockedValues.Contains(v))) kept.Enqueue(v);
        }
        _values.Clear();
        foreach (var v in kept) _values.Enqueue(v);
        while (_values.Count < queueSize)
        {
            _values.Enqueue(GenerateValue());
        }
        RefreshVisuals();
        SyncRuntimeQueue();
        if (enableDebug)
        {
            Debug.Log($"[SpawnQueue] Refiltered queue for mission {missionTarget}. Values: {string.Join(",", _values)}");
        }
    }

    private void HandleSpawnValueUnlocked(int value)
    {
        if (!Application.isPlaying) return;
        int missionCap = MissionController.Instance != null ? MissionController.Instance.CurrentMission : fallbackStartingMission;
        RefilterQueueForMission(missionCap);
    }

    private System.Collections.IEnumerator DelayedMissionQueueSync()
    {
        yield return null; yield return null;
        if (MissionController.Instance != null)
        {
            HandleMissionChanged(MissionController.Instance.CurrentMission);
        }
    }

    private void RefreshVisuals()
    {
        if (!Application.isPlaying)
        {
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
                if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                view.Initialize(val, defaultColor, -1, -1);
                if (brickSet != null)
                {
                    var sprite = brickSet.GetSprite(val);
                    view.SetSprite(sprite);
                }
            }
            i++;
        }
        for (; i < _queueVisuals.Count; i++)
        {
            var view = _queueVisuals[i];
            view.gameObject.SetActive(false);
        }
        cachedCountPrev = _values.Count;
        ApplyImmediateScales();
        SyncRuntimeQueue();
    }

    public bool SpawnIntoColumn(int column)
    {
        if (!Application.isPlaying) return false;
        if (tileSystem == null || tileSystem.Grid == null) return false;
        if (column < 0 || column >= tileSystem.Grid.Columns) return false;
        if (lockDuringSpawnAndMerge && _inputLocked) return false;
        if (BoosterController.Instance != null && BoosterController.Instance.IsActive) return false;
        if (_values.Count == 0) FillQueue();
        int value = 0;
        int originalCount = _values.Count;
        if (originalCount > 0)
        {
            var temp = new List<int>(_values);
            value = temp[^1];
            temp.RemoveAt(temp.Count - 1);
            _values.Clear();
            foreach (var v in temp) _values.Enqueue(v);
        }

        RectTransform startRect = null;
        _movingTileVisual = null;
        if (_queueVisuals.Count > 0)
        {
            int consumedIndex = Mathf.Clamp(originalCount - 1, 0, _queueVisuals.Count - 1);
            var consumed = _queueVisuals[consumedIndex];
            if (consumed != null)
            {
                startRect = (RectTransform)consumed.transform;
                _movingTileVisual = null;
                _consumedVisual = consumed;
                _consumedVisual.gameObject.SetActive(false);
            }
        }
        bool started = tileSystem.SpawnTileAnimatedFromQueue(column, value, defaultColor, startRect);
        if (!started)
        {
            _values.Enqueue(value);
            RefreshVisuals();
            if (tileSystem != null && !tileSystem.HasAnyEmptyCell())
            {
                var popup = FindFirstObjectByType<PopupController>();
                if (popup != null)
                {
                    popup.ShowGameOverPopUp();
                }
            }
            return false;
        }
        if (lockDuringSpawnAndMerge) _inputLocked = true;
        _freezeHighlight = true;
        _pendingInsertedValue = GenerateValue();
        _pendingQueueShift = true;
        SyncRuntimeQueue();
        return true;
    }

    private void HandleSpawnAnimationComplete(int x, int y)
    {
        if (!Application.isPlaying) return;
        if (!_pendingQueueShift) return;
        _pendingQueueShift = false;
        _movingTileVisual = null;
        _consumedVisual = null;
        int newVal = _pendingInsertedValue;
        var afterTemp = new List<int>(_values);
        _values.Clear();
        _values.Enqueue(newVal);
        foreach (var v in afterTemp) _values.Enqueue(v);
        if (animateQueue)
            StartCoroutine(AnimateQueueRefresh(newVal));
        else
            RefreshVisuals();
        _freezeHighlight = false;
        SyncRuntimeQueue();
        if (lockDuringSpawnAndMerge)
            StartCoroutine(WaitForMergeUnlock());
    }

    private System.Collections.IEnumerator WaitForMergeUnlock()
    {
        while (tileSystem != null && tileSystem.Busy)
            yield return null;
        _inputLocked = false;
    }


    private System.Collections.IEnumerator AnimateQueueRefresh(int newVal)
    {
        RefreshVisuals();
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
        ApplyImmediateScales();
    }

    private void Update()
    {
        if (_queueVisuals.Count == 0) return;
        if (_freezeHighlight) return;
        int activeIndex = Mathf.Min(_values.Count - 1, _queueVisuals.Count - 1);
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
            if (_currentActiveIndex >= 0 && _currentActiveIndex < _queueVisuals.Count && !offsetActiveOnly)
            {
                var prevRT = (RectTransform)_queueVisuals[_currentActiveIndex].transform;
                var p = prevRT.anchoredPosition; p.y = 0f; prevRT.anchoredPosition = p;
            }
            var newRT = (RectTransform)_queueVisuals[activeIndex].transform;
            _activeBasePos = newRT.anchoredPosition;
            _currentActiveIndex = activeIndex;
        }
        var rtActive = (RectTransform)_queueVisuals[activeIndex].transform;
        float expectedY = _activeBasePos.y + activeYOffset;
        if (Mathf.Abs(rtActive.anchoredPosition.y - expectedY) > 0.001f && Mathf.Abs(rtActive.anchoredPosition.y - _activeBasePos.y) < Mathf.Abs(activeYOffset) + 0.01f)
        {
            _activeBasePos = rtActive.anchoredPosition;
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
            // if (debugActiveScale && i == activeIndex)
            // {
            //     Debug.Log($"[SpawnQueue] ApplyImmediateScales activeIndex scale={setScale}");
            // }
        }
    }

    private void SyncRuntimeQueue()
    {
        if (!Application.isPlaying) return;
        InGameData.CurrentQueueValues.Clear();
        foreach (var v in _values)
            InGameData.CurrentQueueValues.Add(v);
    }

    private void EnsureDefaultSpawnConfig()
    {
        if (!autoConfigureDefaults) return;
        bool needThresholds = (spawnThresholds == null || spawnThresholds.Length == 0);
        bool needWeights = (spawnWeights == null || spawnWeights.Length == 0);
        if (!needThresholds && !needWeights) return;

        if (needThresholds)
        {
            spawnThresholds = new[]
            {
                new SpawnThreshold{ value = 2, requiredMission = 4 },
                new SpawnThreshold{ value = 4, requiredMission = 8 },
                new SpawnThreshold{ value = 8, requiredMission = 16 },
                new SpawnThreshold{ value = 16, requiredMission = 32 },
                new SpawnThreshold{ value = 32, requiredMission = 128 },
                new SpawnThreshold{ value = 64, requiredMission = 256 }, 
                new SpawnThreshold{ value = 128, requiredMission = 512 },
                new SpawnThreshold{ value = 256, requiredMission = 1024 },
                new SpawnThreshold{ value = 512, requiredMission = 2048 },
            };
        }
        if (needWeights)
        {
            spawnWeights = new[]
            {
                new SpawnWeight{ value = 2, weight = 30f },
                new SpawnWeight{ value = 4, weight = 22f },
                new SpawnWeight{ value = 8, weight = 16f },
                new SpawnWeight{ value = 16, weight = 10f },
                new SpawnWeight{ value = 32, weight = 5f },
                new SpawnWeight{ value = 64, weight = 2.2f },
                new SpawnWeight{ value = 128, weight = 1.2f },
                new SpawnWeight{ value = 256, weight = 0.6f },
                new SpawnWeight{ value = 512, weight = 0.3f },
            };
        }
    }
}

