using Audio;
using Popup;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class LosePopup : PopUpBase
{

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
    
    public void OnClickRestartGame()
    {
        HidePopUp(-1800f, 0.5f, () =>
        {
            Time.timeScale = 1;
            GameManager.isCounting = false;
            GameManager.isGameOver = false;
            GameManager.isTutorial = true;
            AudioController.Instance.PlaySoundButtonClick();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    public void OnClickLoadMainMenu()
    {
        HidePopUp(-1800f, 0.5f, () =>
        {
            Time.timeScale = 1;
            GameManager.isCounting = false;
            GameManager.isGameOver = false;
            GameManager.isTutorial = true;
            AudioController.Instance.PlaySoundButtonClick();
            SceneManager.LoadScene("MainScene");
        });
    }
}
