using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Data;

public interface ISceneController
{
    UniTask ChangeScene(SceneType _sceneType, UnityAction onCompleteFade = null);
}
public class SceneController : Singleton<SceneController>, ISceneController
{
    [SerializeField] private Image imgFadeUI;
    public UnityAction callBackLoadScreen;
    [SerializeField] private SceneType previousScene;
    [SerializeField] private SceneType currentScene;

    public SceneType PreviousScene => previousScene;
    public SceneType CurrentScene => currentScene;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void Start()
    {
        currentScene = previousScene = SceneType.MainScene;

        Debug.Log($"CheckLoading");
        imgFadeUI.gameObject.SetActive(true);
        imgFadeUI.DOFade(1, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            imgFadeUI.DOFade(0, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            {
                imgFadeUI.gameObject.SetActive(false);
            });
        });
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        callBackLoadScreen?.Invoke();
        callBackLoadScreen = FadeOutBlackScreen;
    }

    public async UniTask ChangeScene(SceneType _sceneType, UnityAction onCompleteFade = null)
    {
        previousScene = currentScene;
        currentScene = _sceneType;
        /*        imgFadeUI.gameObject.SetActive(true);
                imgFadeUI.DOFade(0, 0.3f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    onCompleteFade?.Invoke();
                    DOVirtual.DelayedCall(0.2f, () =>
                    {
                        SceneManager.LoadScene($"{currentScene}");
                    });
                });
        */
        await LoadingFade.Instance.ShowLoadingFade();
        await Task.Delay(1000);
        SceneManager.LoadScene($"{currentScene}");
        await Task.Delay(1000);
        await LoadingFade.Instance.HideLoadingFade();
    }

    public void FadeOutBlackScreen()
    {
        imgFadeUI.DOFade(0, 0.5f).SetEase(Ease.Linear).OnComplete(() => { imgFadeUI.gameObject.SetActive(false); });
    }


}
public enum SceneType
{
    LoadingScene = 0,
    MainScene = 1,
    GamePlayScene = 2
}