using Audio;
using UnityEngine;
using Data;
using Popup;

public class PopupController : Singleton<PopupController>
{
    [Header("Lose Popup")]
    [SerializeField] private LosePopup popUpGameOver;
    
    [Header("Pause Popup")]
    [SerializeField] private PausePopup popUpPauseGame;
    
    [Header("Tutorial Popup")]
    [SerializeField] private TutorialPopup popUpTutorial;
    
    public PausePopup pausePopup => popUpPauseGame;
    
    #region TutorialPopup
    [ContextMenu("Show Tutorial Popup")]
    public void ShowTutorialPopup()
    {
        AudioController.Instance.PlayPopupOpenSound();
        popUpTutorial.ShowPopUp(100f, .6f);
    }

    [ContextMenu("Hide Tutorial Popup")]
    public void HideTutorialPopup()
    {
        AudioController.Instance.PlayPopupCloseSound();
        popUpTutorial.HidePopUp(-1800f, .6f);
    }
    #endregion
    
    
    
    #region LosePopup
    [ContextMenu("Show GameOver Popup")]
    public void ShowGameOverPopUp()
    {
        AudioController.Instance.PlayPopupOpenSound();
        popUpGameOver.ShowPopUp(100f, .6f);
    }
    [ContextMenu("Hide GameOver Popup")]
    public void HideGameOverPopUp()
    {
        AudioController.Instance.PlayPopupCloseSound();
        popUpGameOver.ShowPopUp(-1800f, .6f);
    }
    #endregion
    
    #region PausePopup
    [ContextMenu("Show Pause Popup")]
    public void ShowPausePopUp()
    {
        AudioController.Instance.PlayPopupOpenSound();
        popUpPauseGame.ShowPopUp(100f, .6f);
    }
    [ContextMenu("Hide Pause Popup")]
    public void HidePausePopUp()
    {
        AudioController.Instance.PlayPopupCloseSound();
        popUpPauseGame.HidePopUp(-1800f, .6f);
    }
    #endregion
}
