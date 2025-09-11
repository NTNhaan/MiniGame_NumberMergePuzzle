using System.Collections.Generic;
using UnityEngine;

public static class InGameData
{
    public static GameState GAME_STATE = GameState.MainMenu;

    public static int CurrentScore = 0;
    public static int TurnCount = 0;

    public static readonly List<int> CurrentQueueValues = new();

    public static bool IsSpawningTile = false;

    public static void ResetRuntime()
    {
        CurrentScore = 0;
        TurnCount = 0;
        CurrentQueueValues.Clear();
        IsSpawningTile = false;
        GAME_STATE = GameState.GamePlay; 
    }
}
