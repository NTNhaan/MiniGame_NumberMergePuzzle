using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
namespace Popup
{
    public class PopUpBase : MonoBehaviour
    {
        [SerializeField] protected Transform tfmPopup;
        [SerializeField] protected Image imgCover;

        public Transform TfmPopup { get => tfmPopup; }

        public virtual void ShowPopUp(float posY = 0, float duration = 0, UnityAction onComplete = null)
        {

            // soundController.PlaySound(SoundName.Show);  // POPUP SHOW
            TfmPopup.gameObject.SetActive(true);
            TfmPopup.DOLocalMoveY(posY, duration).SetEase(Ease.OutBack).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }
        public virtual void HidePopUp(float posY, float duration, UnityAction onComplete = null)
        {
            // soundController.PlaySound(SoundName.Hide); // POPUP HIDE

            TfmPopup.DOLocalMoveY(posY, duration).SetEase(Ease.InBack).OnComplete(() =>
            {
                TfmPopup.gameObject.SetActive(true);

                onComplete?.Invoke();
            });
        }
        public virtual void ShowCover(float duration = 0.5f, UnityAction onComplete = null)
        {
            imgCover.gameObject.SetActive(true);
            imgCover.DOFade(0.9f, duration).SetEase(Ease.Linear).OnComplete(() =>
            {
                onComplete?.Invoke();
            });

        }
        public virtual void HideCover(UnityAction onComplete = null)
        {
            imgCover.DOFade(0f, 1f).SetEase(Ease.Linear).OnComplete(() =>
            {
                imgCover.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}