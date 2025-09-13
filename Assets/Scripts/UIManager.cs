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
    void Start()
    {
        ScoreController.Instance.OnScoreChanged += UpdateScoreText;
        ScoreController.Instance.OnScoreChanged += UpdatePanelScoreText;
        EventManager.OnMissionChange += UpdateMissionText;
        // Coin UI
        EventManager.OnCoinChanged += UpdateCoinText;
        if (DBController.Instance != null) UpdateCoinText(DBController.Instance.COIN);
        // Set initial mission text nếu MissionController đã khởi tạo trước UIManager
        InitializeMissionUI();
        // Đảm bảo nếu MissionController khởi tạo trễ thì vẫn sync được mission thực tế (ví dụ đã > startingMission)
        StartCoroutine(DelayedMissionSync());
        // ScoreController.Instance.OnHealthChanged += UpdateHealthPlayer;
    }
    void OnDestroy()
    {
        ScoreController.Instance.OnScoreChanged -= UpdateScoreText;
        ScoreController.Instance.OnScoreChanged -= UpdatePanelScoreText;
        EventManager.OnMissionChange -= UpdateMissionText;
        EventManager.OnCoinChanged -= UpdateCoinText;
        // ScoreController.Instance.OnHealthChanged -= UpdateHealthPlayer;
    }

    private void UpdateCoinText(int coin)
    {
        if (CoinText != null) CoinText.text = coin.ToString();
    }
    private void Update()
    {
        PanelBestScoreText.text = ScoreController.Instance.GetHighScore().ToString();
        // HealthPlayer.text = scoreManager.GetHealthPlayer().ToString();
    }

    void UpdateScoreText(int score)
    {
        scoreText.text = ScoreController.Instance.Score.ToString();
    }

    void UpdatePanelScoreText(int score)
    {
        PanelScoreText.text = "Score: " + ScoreController.Instance.Score;
    }

    void UpdateMissionText(int mission)
    {
        if (textMission != null)
        {
            // Chỉ hiển thị target (mission) không cần prefix nếu không muốn
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
            // fallback starting mission (256)
            target = 256;
        }
        UpdateMissionText(target);
    }

    private System.Collections.IEnumerator DelayedMissionSync()
    {
        // Chờ 2 frame để chắc chắn các Awake khác chạy xong
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
