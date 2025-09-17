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
        // Initialize highest text and image. If DB not set, derive from mission state.
        int displayInit = 256;
        if (MissionController.Instance != null)
        {
            displayInit = Mathf.Max(256, MissionController.Instance.StartingMission);
        }
        if (DBController.Instance != null)
        {
            int highest = DBController.Instance.HIGHEST_MISSION_BLOCK;
            if (highest > 0) displayInit = highest; else DBController.Instance.HIGHEST_MISSION_BLOCK = displayInit;
        }
        // For UI consistency, show the current mission number in the text (matches image)
        if (highestBlockText != null)
        {
            int currentMission = (MissionController.Instance != null) ? MissionController.Instance.CurrentMission : displayInit;
            highestBlockText.text = currentMission.ToString();
        }
        if (highestBlockImg != null && MissionController.Instance != null)
        {
            var sp = MissionController.Instance.GetCurrentMissionSprite();
            if (sp != null) highestBlockImg.sprite = sp;
        }
        SettingCtrl.Instance.InitSetting();
        AudioController.Instance.PlayOpenClosePopup();
        AudioController.Instance.PlayBackroundMusicGameplay();
        if (DBController.Instance != null)
            Debug.Log($"CheckDataShape {DBController.Instance.HIGHEST_MISSION_BLOCK}");
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
        int achieved = (MissionController.Instance != null)
            ? MissionController.Instance.GetPreviousMissionValue(newMissionTarget)
            : newMissionTarget / 2;
        // Persist highest if DB is available
        if (DBController.Instance != null)
        {
            if (achieved > DBController.Instance.HIGHEST_MISSION_BLOCK)
            {
                DBController.Instance.HIGHEST_MISSION_BLOCK = achieved;
            }
        }
        // Update the text to show the CURRENT mission (matches image)
        if (highestBlockText != null)
        {
            highestBlockText.text = newMissionTarget.ToString();
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
        // SceneManager.LoadScene("GamePlayScene");
        SceneController.Instance?.ChangeScene(SceneType.GamePlayScene);

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
    public void OnClickShowIAP()
    {
        ShopController.Instance?.ShowShop();
    }
}
