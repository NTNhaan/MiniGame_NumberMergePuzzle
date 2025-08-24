using Audio;
using Popup;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePopup : PopUpBase
{
    [Header("Pause Menu")]
    [SerializeField] private Image imgStatus;
    [SerializeField] private Sprite[] sprStatusPause;
    private bool isPause = false;
    public bool IsPause => isPause;

    #region Overrides Func
    public override void ShowPopUp(float posY, float duration, UnityAction onComplete = null)
    {
        ShowCover(0.5f, () =>
        {
            base.ShowPopUp(posY, duration, onComplete);
        });
    }
    public override void HidePopUp(float posY, float duration, UnityAction onComplete = null)
    {

        base.HidePopUp(posY, duration, () =>
        {
            onComplete?.Invoke();
            tfmPopup.gameObject.SetActive(false);
            HideCover();
        });
    }

    public override void ShowCover(float duration = 0.5f, UnityAction onComplete = null)
    {
        base.ShowCover(duration, onComplete);
    }


    public override void HideCover(UnityAction onComplete = null)
    {
        base.HideCover(onComplete);
    }
    #endregion


    public void OnClickContinueGame()
    {
        HidePopUp(-1800f, 1f, () =>
        {
            isPause = false;
            // GameManager.isGameOver = false;
            // GameManager.isTutorial = true;
            SetStatePause(false);
            AudioController.Instance.PlaySoundButtonClick();
        });
    }
    public void OnClickLoadMainMenu()
    {
        HidePopUp(-1800f, 1f, () =>
        {
            isPause = false;
            Time.timeScale = 1;
            GameManager.isCounting = false;
            GameManager.isGameOver = false;
            GameManager.isTutorial = true;
            AudioController.Instance.PlaySoundButtonClick();
            SceneManager.LoadScene("MainScene");
        });
    }

    public void OnClickPauseButton()
    {
        Debug.Log("Pause Button Clicked");
        isPause = true;
        SetStatePause(isPause);
        PopupController.Instance.ShowPausePopUp();
    }
    public void SetStatePause(bool isPause)
    {
        if (isPause)
        {
            AudioController.Instance.SetVolumeMusic(false);
            imgStatus.sprite = sprStatusPause[0];
        }
        else
        {
            AudioController.Instance.SetVolumeMusic(true);
            imgStatus.sprite = sprStatusPause[1];
        }
    }
}
