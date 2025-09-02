using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu phát sinh trong một ván chơi hiện tại (không lưu PlayerPrefs). Reset khi bắt đầu game mới.
/// </summary>
public static class InGameData
{
    public static GameState GAME_STATE = GameState.MainMenu;

    // Điểm và tiến trình
    public static int CurrentScore = 0;
    public static int TurnCount = 0;

    // Hàng đợi ô sắp spawn (giá trị), đồng bộ với SpawnQueue.
    public static readonly List<int> CurrentQueueValues = new();

    // Tạm lưu trạng thái đang anim spawn để UI hoặc input khóa.
    public static bool IsSpawningTile = false;

    public static void ResetRuntime()
    {
        CurrentScore = 0;
        TurnCount = 0;
        CurrentQueueValues.Clear();
        IsSpawningTile = false;
        GAME_STATE = GameState.GamePlay; // chuyển vào trạng thái chơi
    }
}
