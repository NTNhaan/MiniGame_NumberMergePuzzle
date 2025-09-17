using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using Object = System.Object;
using Data;

public class UIManager : MonoBehaviour
{
    [Header("Score Menu")]
    [SerializeField] private float highScore;
    [SerializeField] private Text textMission;
    [Header("Text in GamePlay")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text bestScoreText;
    // [SerializeField] private Text HealthPlayer;
    [Header("Text in Popup")]
    [SerializeField] private Text PanelScoreText;
    [SerializeField] private Text PanelBestScoreText;


    [Header("Coin Shop IAP")]
    [SerializeField] private Text CoinText;

    [Header("Animation Best Score")]
    [SerializeField] private Animator bestScoreAnim;
    [SerializeField] private float bestScoreAnimDuration = 0.9f;
    private Coroutine _bestScoreAnimRoutine;
    private int _prevBestScore;
    void Start()
    {
        ScoreController.Instance.OnScoreChanged += UpdateScoreText;
        ScoreController.Instance.OnScoreChanged += UpdatePanelScoreText;
        ScoreController.Instance.OnScoreChanged += OnScoreMaybeUpdatedHighScore;
        EventManager.OnMissionChange += UpdateMissionText;

        EventManager.OnCoinChanged += UpdateCoinText;
        if (DBController.Instance != null) UpdateCoinText(DBController.Instance.COIN);
        UpdateBestScoreTexts();
        _prevBestScore = ScoreController.Instance != null ? ScoreController.Instance.GetHighScore() : 0;
        InitializeMissionUI();
        StartCoroutine(DelayedMissionSync());
    }
    void OnDestroy()
    {
        ScoreController.Instance.OnScoreChanged -= UpdateScoreText;
        ScoreController.Instance.OnScoreChanged -= UpdatePanelScoreText;
        ScoreController.Instance.OnScoreChanged -= OnScoreMaybeUpdatedHighScore;
        EventManager.OnMissionChange -= UpdateMissionText;
        EventManager.OnCoinChanged -= UpdateCoinText;
        // ScoreController.Instance.OnHealthChanged -= UpdateHealthPlayer;
    }

    private void UpdateCoinText(int coin)
    {
        if (CoinText != null) CoinText.text = coin.ToString();
    }
    // removed per-frame best score polling

    void UpdateScoreText(int score)
    {
        scoreText.text = ScoreController.Instance.Score.ToString("N0");
    }

    void UpdatePanelScoreText(int score)
    {
        PanelScoreText.text = ScoreController.Instance.Score.ToString("N0");
    }

    private void OnScoreMaybeUpdatedHighScore(int _)
    {
        int newHS = ScoreController.Instance != null ? ScoreController.Instance.GetHighScore() : 0;
        if (newHS > _prevBestScore)
        {
            PlayBestScoreAnim();
        }
        _prevBestScore = newHS;
        UpdateBestScoreTexts();
    }

    private void UpdateBestScoreTexts()
    {
        int hs = ScoreController.Instance != null ? ScoreController.Instance.GetHighScore() : 0;
        if (bestScoreText != null) bestScoreText.text = hs.ToString();
        if (PanelBestScoreText != null) PanelBestScoreText.text = hs.ToString();
    }

    private void PlayBestScoreAnim()
    {
        if (bestScoreAnim == null) return;
        if (_bestScoreAnimRoutine != null)
        {
            StopCoroutine(_bestScoreAnimRoutine);
            _bestScoreAnimRoutine = null;
        }
        _bestScoreAnimRoutine = StartCoroutine(BestScoreAnimRoutine());
    }

    private IEnumerator BestScoreAnimRoutine()
    {
        bestScoreAnim.SetBool("isJump", true);
        float t = 0f;
        while (t < bestScoreAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        bestScoreAnim.SetBool("isJump", false);
        _bestScoreAnimRoutine = null;
    }

    void UpdateMissionText(int mission)
    {
        if (textMission != null)
        {
            textMission.text = mission.ToString();
        }
    }
    private void InitializeMissionUI()
    {
        int target = 0;
        if (MissionController.Instance != null)
            target = MissionController.Instance.CurrentMission;
        if (target <= 0)
        {
            target = 256;
        }
        UpdateMissionText(target);
    }

    private System.Collections.IEnumerator DelayedMissionSync()
    {
        yield return null; yield return null;
        if (MissionController.Instance != null)
        {
            UpdateMissionText(MissionController.Instance.CurrentMission);
        }
    }
    // void UpdateHealthPlayer(int health)
    // {
    //     HealthPlayer.text = ScoreController.Instance.HealthPlayer.ToString();
    // }
}
