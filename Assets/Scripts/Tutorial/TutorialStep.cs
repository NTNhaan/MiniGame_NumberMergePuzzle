using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Data;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
public class TutorialStep : Singleton<TutorialStep>
{
    public static bool isTutorialStep = false;
    public static bool isShootTutorial = false;
    public static bool isCountingTutorial = false;
    [SerializeField] private Text textCountDown;
    [SerializeField] private Text textTutorial;
    [SerializeField] private Text textAfterTutorial;
    [SerializeField] private GameObject panelGameTutorial;
    [SerializeField] private GameObject animationHand;
    [SerializeField] private Animator animatorGameTutorial;
    private void Update()
    {
        if (Input.GetMouseButton(0) && !isCountingTutorial)
        {
            HideTutorial();
        }
    }

    public void HideTutorial()
    {
        textTutorial.gameObject.SetActive(false);
        StartCoroutine(StartCountDown());
    }
    public async UniTaskVoid CompleteTutorial()
    {
        await UniTask.WaitUntil(() => isTutorialStep);
        Debug.Log("CheckIsTutorialStep: After " + isTutorialStep);
        
        textAfterTutorial.gameObject.SetActive(false);
        animatorGameTutorial.SetBool("isShow", false);
        animationHand.SetActive(false);
        await UniTask.WaitUntil(() => isShootTutorial);
        isTutorialStep = true;
        PopupController.Instance.ShowTutorialPopup();
    }
    IEnumerator StartCountDown()
    {
        isCountingTutorial = true;
        textCountDown.gameObject.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            textCountDown.text = count.ToString();
            yield return new WaitForSeconds(1f);
            count--;
        }

        textCountDown.text = "GO!";
        yield return new WaitForSeconds(1f);
        
        textAfterTutorial.gameObject.SetActive(true);
        textCountDown.gameObject.SetActive(false);
        panelGameTutorial.SetActive(false);
        animatorGameTutorial.SetBool("isShow", true);
        CompleteTutorial();
    }
}
