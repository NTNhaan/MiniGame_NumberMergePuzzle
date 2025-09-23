using System;
using Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenController : MonoBehaviour
{
    [SerializeField] private MainScene mainScreen;

    public MainScene MainScreen
    {
        get => mainScreen;
    }

    private void Start()
    {
        InGameData.GAME_STATE = GameState.MainMenu;
    }
    public void OnClickPlayBtn()
    {
        mainScreen.HideScreen(() => { mainScreen.ClickStartButton(); });
    }

    public void OnClickSoundButton()
    {
        mainScreen.ClickSoundButton();
    }

    public void OnClickVibrationButton()
    {
        mainScreen.ClickVibrationButton();
    }

    public void OnClickMusicButton()
    {
        mainScreen.ClickMusicButton();
    }



    public void ChangeScreen(SceneManager screen)
    {
        // HideCurScreen(() =>
        // {
        //     ShowScreen(screen);
        // });
    }
}
public enum GameState
{
    MainMenu = 0,
    GamePlay = 1,
    Tutorial = 2,
}