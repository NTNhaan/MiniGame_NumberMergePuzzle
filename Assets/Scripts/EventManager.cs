using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static event UnityAction<int> OnAddPoints;
    public static void AddPoints(int points) => OnAddPoints?.Invoke(points);
    public static event UnityAction<int> OnHPchanged;
    public static void HPChanged(int hp) => OnHPchanged?.Invoke(hp);
    public static event UnityAction OnInitData;
    public static void Initatata() => OnInitData?.Invoke();
    public static event UnityAction<int, int> OnTileSpawnAnimationComplete;
    public static void TileSpawnAnimationComplete(int x, int y) => OnTileSpawnAnimationComplete?.Invoke(x, y);
    public static event UnityAction<int> OnMissionChange;
    public static void MissionChanged(int mission) => OnMissionChange?.Invoke(mission);
    // Khi một giá trị mới được merge lần đầu và được mở khoá để spawn
    public static event UnityAction<int> OnSpawnValueUnlocked;
    public static void SpawnValueUnlocked(int value) => OnSpawnValueUnlocked?.Invoke(value);

    // Coin changes (DBController updates, IAP rewards, boosters spend)
    public static event UnityAction<int> OnCoinChanged;
    public static void CoinChanged(int coins) => OnCoinChanged?.Invoke(coins);

    // Alias event specifically requested (OnCoinsChanged) – dispatches alongside OnCoinChanged for compatibility
    public static event UnityAction<int> OnCoinsChanged;
    public static void CoinsChanged(int coins)
    {
        OnCoinsChanged?.Invoke(coins);
        CoinChanged(coins); // ensure legacy listeners still get updates
    }
}
