using Audio;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using Setting;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Data;
public class MainScene : SceneBase
{

    [Header("=====HightScore MainScene=====")]
    [SerializeField] private Text hightScoreText;
    void Start()
    {
        int hightScore = DBController.Instance.BEST_SCORE;
        hightScoreText.text = hightScore.ToString();
        SettingCtrl.Instance.InitSetting();
        AudioController.Instance.PlayOpenClosePopup();
    }
    #region Override Methods
    public override void ShowScreen(UnityAction onComplete)
    {
        LoadUI();
        base.ShowScreen(onComplete);
    }

    public override void HideScreen(UnityAction onComplete)
    {
        base.HideScreen(onComplete);
    }

    public override void LoadUI()
    {
        base.LoadUI();
    }

    // public void UpdateCoinUI()
    // {
    //     txtCoin.text = $"{DBController.Instance.COIN}";
    // }
    #endregion

    public void ClickStartButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        InGameData.GAME_STATE = GameState.GamePlay;
        SceneManager.LoadScene("GamePlayScene");
        // if (!DBController.Instance.TUTORIAL_COMPLETED)
        // {
        //     SceneManager.LoadScene("TutorialScene");
        // }
        // else
        // {
        //     InGameData.GAME_STATE = GameState.GamePlay;
        //     SceneManager.LoadScene("GamePlayScene");
        // }
    }
    public void ClickSoundButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        SettingCtrl.Instance.SetSound();
    }
    public void ClickVibrationButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        SettingCtrl.Instance.SetVibration();
    }
    public void ClickMusicButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        SettingCtrl.Instance.SetMusic();
    }
}
