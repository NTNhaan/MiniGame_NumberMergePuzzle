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

    [Header("Booster Controller")]
    [SerializeField] private GameObject panelBooster;
    [SerializeField] private BoosterController boosterController;

    [Header("Setting UI")]
    [SerializeField] private Image imgSound;
    [SerializeField] private Image imgVibration;
    [SerializeField] private ButtonType[] sprtSound;
    [SerializeField] private ButtonType[] sprtVibrate;
    void Start()
    {
        SettingCtrl.Instance.InitSetting();
        if (boosterController == null)
        {
            boosterController = FindFirstObjectByType<BoosterController>();
            if (boosterController == null)
            {
                var go = new GameObject("BoosterController");
                boosterController = go.AddComponent<BoosterController>();
            }
        }
        boosterController.SetPanel(panelBooster);
        if (panelBooster != null) panelBooster.SetActive(false);
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
        DBController.Instance.SOUND = !DBController.Instance.SOUND;
        AudioController.Instance.SetVolumeSound(DBController.Instance.SOUND);
        SettingCtrl.Instance.UpdateSettingImage(imgSound, sprtSound, DBController.Instance.SOUND);
    }
    public void ClickVibrationButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        DBController.Instance.VIBRATE = !DBController.Instance.VIBRATE;
        AudioController.Instance.SetVolumeSound(DBController.Instance.VIBRATE);
        SettingCtrl.Instance.UpdateSettingImage(imgVibration, sprtVibrate, DBController.Instance.VIBRATE);
    }
    public void ClickMusicButton()
    {
        AudioController.Instance.PlaySoundButtonClick();
        SettingCtrl.Instance.SetMusic();
    }

    #region Booster
    public void OnClickBoosterDestroy()
    {
        AudioController.Instance.PlaySoundButtonClick();
        boosterController?.ActivateDestroy();
    }
    public void OnClickBoosterSwap()
    {
        AudioController.Instance.PlaySoundButtonClick();
        boosterController?.ActivateSwap();
    }
    public void OnClickBoosterMerge()
    {
        AudioController.Instance.PlaySoundButtonClick();
        boosterController?.ActivateMerge();
    }
    public void OnClickBoosterCancel()
    {
        AudioController.Instance.PlaySoundButtonClick();
        boosterController?.Cancel();
    }
    #endregion
    public void OnClickLoadHomeMenu()
    {
        SceneManager.LoadSceneAsync("MainScene");
    }
}
