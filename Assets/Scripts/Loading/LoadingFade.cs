using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Data;

public class LoadingFade : Singleton<LoadingFade>
{
    [SerializeField] private float timeClose = 0.8f;
    [SerializeField] private float timeOpen = 0.5f;
    [SerializeField] private Ease easeOpen = Ease.OutQuad;
    [SerializeField] private Ease easeClose = Ease.OutQuad;
    [SerializeField] private Image imgBackground;
    [SerializeField] private GameObject topBanner;
    [SerializeField] private Text bottomBanner;
    public async UniTask ShowLoadingFade()
    {
        imgBackground.gameObject.SetActive(true);
        bottomBanner.gameObject.SetActive(true);
        topBanner.gameObject.SetActive(true);

        await imgBackground
            .DOFade(1f, timeOpen)
            .From(0f)
            .SetEase(easeOpen)
            .AsyncWaitForCompletion();

        var rt = (RectTransform)topBanner.transform;
        rt.DOKill();
        rt.localScale = Vector3.zero;

        await bottomBanner.DOFade(1, .5f).From(0).SetEase(Ease.OutQuad);
        await rt.DOScale(1f, timeOpen)
                .SetEase(easeOpen)
                .AsyncWaitForCompletion();

    }
    public async UniTask HideLoadingFade()
    {
        if (topBanner.activeSelf)
        {
            var rt = (RectTransform)topBanner.transform;
            rt.DOKill();
            await rt.DOScale(0f, timeClose)
                    .SetEase(easeClose)
                    .AsyncWaitForCompletion();
            topBanner.SetActive(false);
        }

        await bottomBanner.DOFade(0, 0.5f).From(1).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
        await UniTask.Delay(500);
        await imgBackground.DOFade(0, 1f).From(1).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
        await UniTask.Delay(1000);

        imgBackground.gameObject.SetActive(false);
        bottomBanner.gameObject.SetActive(false);
    }
}