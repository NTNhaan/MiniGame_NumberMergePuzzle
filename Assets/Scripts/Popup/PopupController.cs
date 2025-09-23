using Audio;
using UnityEngine;
using Data;
using Popup;
using DG.Tweening;

public class PopupController : Singleton<PopupController>
{
    [Header("Lose Popup")]
    [SerializeField] private LosePopup popUpGameOver;

    [Header("Pause Popup")]
    [SerializeField] private PausePopup popUpPauseGame;

    [Header("Tutorial Popup")]
    [SerializeField] private TutorialPopup popUpTutorial;

    // [Header("ShopIAP Popup")]
    // [SerializeField] private ShopIAPPopUp shopIAPPopUp;

    [Header("Level Up Screen")]
    [SerializeField] private GameObject panelLevelUp;
    [SerializeField] private float levelUpFadeDuration = 0.5f;
    public PausePopup pausePopup => popUpPauseGame;

    private CanvasGroup _levelUpCanvasGroup;
    // True when ShowLevelUpPopup fade has completed and panel is fully visible
    public bool LevelUpShown { get; private set; } = false;

    private void Awake()
    {
        // Prepare level-up CanvasGroup and initial hidden state
        if (panelLevelUp != null)
        {
            _levelUpCanvasGroup = panelLevelUp.GetComponent<CanvasGroup>();
            if (_levelUpCanvasGroup == null)
                _levelUpCanvasGroup = panelLevelUp.AddComponent<CanvasGroup>();
            _levelUpCanvasGroup.alpha = 0f;
            _levelUpCanvasGroup.interactable = false;
            _levelUpCanvasGroup.blocksRaycasts = false;
            panelLevelUp.SetActive(false);
        }
    }

    // PopupController only exposes show/hide; no mission handling here.

    #region TutorialPopup
    [ContextMenu("Show Tutorial Popup")]
    public void ShowTutorialPopup()
    {
        AudioController.Instance.PlayOpenClosePopup();
        popUpTutorial.ShowPopUp(100f, .6f);
    }

    [ContextMenu("Hide Tutorial Popup")]
    public void HideTutorialPopup()
    {
        AudioController.Instance.PlayOpenClosePopup();
        popUpTutorial.HidePopUp(-1800f, .6f);
    }
    #endregion



    #region LosePopup
    [ContextMenu("Show GameOver Popup")]
    public void ShowGameOverPopUp()
    {
        Debug.Log($"CheckGameOverrPopup");
        AudioController.Instance.PlayOpenClosePopup();
        popUpGameOver.ShowPopUp(100f, .6f);
    }
    [ContextMenu("Hide GameOver Popup")]
    public void HideGameOverPopUp()
    {
        AudioController.Instance.PlayOpenClosePopup();
        popUpGameOver.HidePopUp(-1800f, .6f);
    }
    #endregion

    #region PausePopup
    [ContextMenu("Show Pause Popup")]
    public void ShowPausePopUp()
    {
        AudioController.Instance.PlayOpenClosePopup();
        popUpPauseGame.ShowPopUp(100f, .6f);
    }
    [ContextMenu("Hide Pause Popup")]
    public void HidePausePopUp()
    {
        AudioController.Instance.PlayOpenClosePopup();
        popUpPauseGame.HidePopUp(-1800f, .6f);
    }
    #endregion


    #region ShopIAP
    [ContextMenu("Show ShopIAP Popup")]
    public void ShowShopIAPPopUp()
    {
        AudioController.Instance.PlayOpenClosePopup();
        ShopController.Instance?.ShowShop();
    }
    [ContextMenu("Hide ShopIAP Popup")]
    public void HideShopIAPPopUp()
    {
        AudioController.Instance.PlayOpenClosePopup();
        ShopController.Instance?.HideShop();
    }
    #endregion

    #region Level Up
    public void ShowLevelUpPopup()
    {
        if (panelLevelUp == null) return;
        if (_levelUpCanvasGroup == null)
        {
            _levelUpCanvasGroup = panelLevelUp.GetComponent<CanvasGroup>();
            if (_levelUpCanvasGroup == null)
                _levelUpCanvasGroup = panelLevelUp.AddComponent<CanvasGroup>();
        }
        panelLevelUp.SetActive(true);
        _levelUpCanvasGroup.DOKill();
        _levelUpCanvasGroup.alpha = 0f;
        _levelUpCanvasGroup.interactable = true;
        _levelUpCanvasGroup.blocksRaycasts = true;
        LevelUpShown = false;
        AudioController.Instance.PlayLevelUpSound();
        AudioController.Instance.DefaultVibration();
        _levelUpCanvasGroup
            .DOFade(1f, levelUpFadeDuration)
            .SetUpdate(true)
            .OnComplete(() => { LevelUpShown = true; });
    }
    public void HideLevelUpPopup()
    {
        if (panelLevelUp == null || _levelUpCanvasGroup == null) return;
        _levelUpCanvasGroup.DOKill();
        _levelUpCanvasGroup.DOFade(0f, levelUpFadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _levelUpCanvasGroup.interactable = false;
                _levelUpCanvasGroup.blocksRaycasts = false;
                LevelUpShown = false;
                panelLevelUp.SetActive(false);
            });
    }
    #endregion
}
