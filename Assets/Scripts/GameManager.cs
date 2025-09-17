using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DefaultNamespace;
using UnityEngine.SceneManagement;
using Data;
public class GameManager : Singleton<GameManager>
{
    public static bool isGameOver = false;
    public static bool isTutorial = true;
    public static bool isCounting = false;
    [SerializeField] private Text textCountDown;
    [SerializeField] private Text textTutorial;
    [SerializeField] private GameObject panelGameTutorial;
    [SerializeField] private ScoreController scoreController;
    void Start()
    {
        EventManager.OnAddPoints += scoreController.AddPoints;
        EventManager.OnHPchanged += HandleHealthChanged;

        AudioController.Instance.PlayBackroundMusicGameplay();
    }

    public void HandleHealthChanged(int damage)
    {
        scoreController.TakeDamage(damage);

        if (scoreController.GetHealthPlayer() <= 0)
        {
            CheckGameOver();
        }
    }

    public void CheckGameOver()
    {
        isGameOver = true;
        isTutorial = true;
        AudioController.Instance.PlayGameOverSound();
        PopupController.Instance.ShowGameOverPopUp();
    }
    public void HideTutorial()
    {
        textTutorial.gameObject.SetActive(false);
        StartCoroutine(StartCountDown());
    }
    IEnumerator StartCountDown()
    {
        isCounting = true;
        Debug.Log($"StartCountDown isCounting: {isCounting}");
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
        textCountDown.gameObject.SetActive(false);
        isTutorial = false;
        panelGameTutorial.SetActive(false);
    }
}
