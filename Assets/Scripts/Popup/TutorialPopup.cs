using Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Popup
{
    public class TutorialPopup : PopUpBase
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
        
        public void OnClickContinueGamePlay()
        {
            HidePopUp(-1800f, 0.5f, () =>
            {
                GameManager.isTutorial = true;
                GameManager.isCounting = false;
                DBController.Instance.TUTORIAL_COMPLETED = true;
                InGameData.GAME_STATE = GameState.GamePlay;
                SceneManager.LoadScene("GamePlayScene");
            });
        }
    }
}