using System.Collections;
using Audio;
using Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using LineManager;

public class SceneBase : MonoBehaviour
{
    [Header("=====Variables Override Scene Base=====")]
    [SerializeField] protected Animator[] anims;
    // [SerializeField] protected Text txtCoin;
    [SerializeField] protected GameObject[] gobjPanels;
    [SerializeField] protected Animator animTransition;
    [SerializeField] private CanvasGroup canvasGroup;
    public GameObject[] GobjPanels { get => gobjPanels; }
    protected DBController _db;

    private void Start()
    {
        canvasGroup.blocksRaycasts = false;
        _db = DBController.Instance;
    }

    public virtual void ShowScreen(UnityAction onComplete = null)
    {
        AudioController.Instance.PlayOpenClosePopup();
        if (gobjPanels != null)
        {
            foreach (var panel in gobjPanels)
            {
                if (panel != null)
                    panel.SetActive(true);
            }
        }
        if (anims != null)
        {
            foreach (var animator in anims)
            {
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.SetBool("isShow", true);
                }
            }
        }
        DOVirtual.DelayedCall(0.6f, () =>
        {
            onComplete?.Invoke();
        });

    }
    public virtual void HideScreen(UnityAction onComplete = null)
    {
        AudioController.Instance.PlayOpenClosePopup();
        if (anims != null)
        {
            foreach (var animator in anims)
            {
                if (animator != null)
                    animator.SetBool("isShow", false);
            }
        }
        HideTransition();
        DOVirtual.DelayedCall(1f, () =>
        {
            if (gobjPanels != null)
            {
                foreach (var panel in gobjPanels)
                {
                    if (panel != null)
                        panel.SetActive(false);
                }
            }
            onComplete?.Invoke();
        });
    }
    public void HideTransition()
    {
        Debug.Log("Hiding transition");
        if (animTransition != null)
        {
            canvasGroup.blocksRaycasts = false;
            animTransition.SetBool("isStransition", false);
        }
    }
    public void ShowTransition()
    {
        Debug.Log("Show transition");
        if (animTransition != null)
        {
            canvasGroup.blocksRaycasts = true;
            animTransition.SetBool("isStransition", true);
        }
    }

    public virtual void LoadUI()
    {
        // txtCoin.text = DBController.Instance.COIN.ToString("n0");
        //Debug.Log("============== Load UI");
    }
}
