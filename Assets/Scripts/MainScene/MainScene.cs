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

    [Header("=====Highest Block MainScene=====")]
    [SerializeField] private Text highestBlockText;
    [SerializeField] private Image highestBlockImg;

    void Start()
    {
        if (DBController.Instance != null && highestBlockText != null)
        {
            int highest = DBController.Instance.HIGHEST_MISSION_BLOCK;
            int display = highest > 0 ? highest : 256;
            highestBlockText.text = display.ToString();
            if (highestBlockImg != null && MissionController.Instance != null)
            {
                var sp = MissionController.Instance.GetCurrentMissionSprite();
                if (sp != null) highestBlockImg.sprite = sp;
            }
        }
        SettingCtrl.Instance.InitSetting();
        AudioController.Instance.PlayOpenClosePopup();
        AudioController.Instance.PlayBackroundMusicGameplay();
    }
    private void OnEnable()
    {
        EventManager.OnMissionChange += HandleMissionChangeMainScene;
    }
    private void OnDisable()
    {
        EventManager.OnMissionChange -= HandleMissionChangeMainScene;
    }

    private void HandleMissionChangeMainScene(int newMissionTarget)
    {
        if (DBController.Instance == null) return;
        int achieved = newMissionTarget / 2;
        if (achieved > DBController.Instance.HIGHEST_MISSION_BLOCK)
        {
            DBController.Instance.HIGHEST_MISSION_BLOCK = achieved;
        }
        if (highestBlockText != null)
        {
            int highest = DBController.Instance.HIGHEST_MISSION_BLOCK;
            highestBlockText.text = (highest > 0 ? highest : 256).ToString();
        }
        if (highestBlockImg != null && MissionController.Instance != null)
        {
            var sp = MissionController.Instance.GetCurrentMissionSprite();
            if (sp != null) highestBlockImg.sprite = sp;
        }
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
