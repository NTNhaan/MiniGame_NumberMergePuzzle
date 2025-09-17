using Data;
using UnityEngine;
using System.Collections.Generic;

public class MissionController : Singleton<MissionController>
{
    [SerializeField] private int startingMission = 256;
    [SerializeField] private bool autoDoubleNext = false;
    [SerializeField] private int maxMissionCap = 65536;
    [Header("Sprites")]
    [SerializeField] private BrickSet brickSet;

    public int CurrentMission { get; private set; }
    public int StartingMission => startingMission;

    private readonly HashSet<int> _unlockedValues = new();
    public IReadOnlyCollection<int> UnlockedValues => _unlockedValues;
    private int _highestUnlockedCached;

    public static MissionController Ensure()
    {
        var go = new GameObject("MissionController(Auto)");
        DontDestroyOnLoad(go);
        var mc = go.AddComponent<MissionController>();
        return mc;
    }

    private void Awake()
    {
        int savedHighest = 0;
        if (Data.DBController.Instance != null)
        {
            savedHighest = Data.DBController.Instance.HIGHEST_MISSION_BLOCK;
        }
        if (CurrentMission <= 0) CurrentMission = startingMission;
        if (savedHighest >= startingMission)
        {
            int next = GetNextMissionValue(savedHighest);
            if (next > 0) CurrentMission = next;
        }
        BuildInitialUnlocked(savedHighest);
        EventManager.MissionChanged(CurrentMission);
    }

    public void TryUnlock(int producedValue)
    {
        if (producedValue <= 0) return;
        if (!_unlockedValues.Contains(producedValue))
        {
            if (producedValue < CurrentMission || producedValue == CurrentMission)
            {
                _unlockedValues.Add(producedValue);
                if (producedValue > _highestUnlockedCached) _highestUnlockedCached = producedValue;
                EventManager.SpawnValueUnlocked(producedValue);
            }
        }
        if (producedValue < CurrentMission) return;
        int achieved = CurrentMission;
        if (Data.DBController.Instance != null && achieved > Data.DBController.Instance.HIGHEST_MISSION_BLOCK)
        {
            Data.DBController.Instance.HIGHEST_MISSION_BLOCK = achieved;
        }
        int nextMission = GetNextMissionValue(achieved);
        if (nextMission <= 0) return;
        CurrentMission = nextMission;
        EventManager.MissionChanged(CurrentMission);
        PopupController.Instance.ShowLevelUpPopup();
        // // var popup = FindFirstObjectByType<PopupController>();
        // if (popup != null)
        // {
        //     popup.ShowLevelUpPopup();
        // }
    }

    public Sprite GetCurrentMissionSprite()
    {
        if (brickSet == null) return null;
        return brickSet.GetSprite(CurrentMission);
    }

    public int GetPreviousMissionValue(int current)
    {
        int candidate = -1;
        if (brickSet != null && brickSet.bricks != null && brickSet.bricks.Count > 0)
        {
            for (int i = 0; i < brickSet.bricks.Count; i++)
            {
                int val = brickSet.bricks[i].number;
                if (val < current)
                {
                    if (candidate < 0 || val > candidate) candidate = val;
                }
            }
        }
        if (candidate < 0 && DataConfig.ALLOWED_VALUES != null && DataConfig.ALLOWED_VALUES.Length > 0)
        {
            for (int i = 0; i < DataConfig.ALLOWED_VALUES.Length; i++)
            {
                int val = DataConfig.ALLOWED_VALUES[i];
                if (val < current)
                {
                    if (candidate < 0 || val > candidate) candidate = val;
                }
            }
        }
        if (candidate < 0)
        {
            int half = current / 2;
            if (half > 0) candidate = half;
        }
        return candidate > 0 ? candidate : 256;
    }

    public int GetNextMissionValue(int current)
    {
        int candidate = -1;
        if (brickSet != null && brickSet.bricks != null && brickSet.bricks.Count > 0)
        {
            for (int i = 0; i < brickSet.bricks.Count; i++)
            {
                int val = brickSet.bricks[i].number;
                if (val > current)
                {
                    if (candidate < 0 || val < candidate) candidate = val;
                }
            }
        }
        if (candidate < 0 && DataConfig.ALLOWED_VALUES != null && DataConfig.ALLOWED_VALUES.Length > 0)
        {
            for (int i = 0; i < DataConfig.ALLOWED_VALUES.Length; i++)
            {
                int val = DataConfig.ALLOWED_VALUES[i];
                if (val > current)
                {
                    if (candidate < 0 || val < candidate) candidate = val;
                }
            }
        }
        if (candidate < 0)
        {
            long next = (long)current * 2L;
            if (next <= maxMissionCap) candidate = (int)next;
        }
        if (candidate > maxMissionCap) candidate = maxMissionCap;
        return candidate;
    }

    private void BuildInitialUnlocked(int savedHighestAchievedMission)
    {
        _unlockedValues.Clear();
        var seeds = new List<int>();
        void AddSeed(int v)
        {
            if (v > 0 && !seeds.Contains(v) && v < CurrentMission) seeds.Add(v);
        }
        AddSeed(2); AddSeed(4); AddSeed(8);
        if (brickSet != null && brickSet.bricks != null && brickSet.bricks.Count > 0)
        {
            seeds.RemoveAll(s => !brickSet.Contains(s));
        }
        if (seeds.Count == 0)
        {
            var ordered = new List<int>();
            if (brickSet != null && brickSet.bricks != null)
            {
                foreach (var b in brickSet.bricks)
                {
                    if (b.number < CurrentMission) ordered.Add(b.number);
                }
            }
            else if (DataConfig.ALLOWED_VALUES != null)
            {
                foreach (var v in DataConfig.ALLOWED_VALUES)
                {
                    if (v < CurrentMission) ordered.Add(v);
                }
            }
            ordered.Sort();
            for (int i = 0; i < ordered.Count && seeds.Count < 3; i++)
            {
                AddSeed(ordered[i]);
            }
        }
        foreach (var s in seeds) _unlockedValues.Add(s);
        _highestUnlockedCached = 0;
        foreach (var s in seeds) if (s > _highestUnlockedCached) _highestUnlockedCached = s;
    }
}
