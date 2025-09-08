using UnityEngine;
using Audio;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using Setting;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Data;
public class GamePlayScene : SceneBase
{
    void Start()
    {
        SettingCtrl.Instance.InitSetting();
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
