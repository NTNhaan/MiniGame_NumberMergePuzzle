using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using DefaultNamespace;
using UnityEngine;
using  UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using Object = System.Object;

public class UIManager : MonoBehaviour
{
    [Header("Score Menu")]
    [SerializeField] private float highScore;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text HealthPlayer;
    public Text PanelScoreText;
    public Text HighScoreText;
    
    void Start()
    {
        ScoreController.Instance.OnScoreChanged += UpdateScoreText;
        ScoreController.Instance.OnScoreChanged += UpdatePanelScoreText;
        ScoreController.Instance.OnHealthChanged += UpdateHealthPlayer;
    }
    void OnDestroy()
    {
        ScoreController.Instance.OnScoreChanged -= UpdateScoreText;
        ScoreController.Instance.OnScoreChanged -= UpdatePanelScoreText;
        ScoreController.Instance.OnHealthChanged -= UpdateHealthPlayer;
    }
    private void Update()
    {
        HighScoreText.text = ScoreController.Instance.GetHighScore().ToString();
        // HealthPlayer.text = scoreManager.GetHealthPlayer().ToString();
    }
    
    void UpdateScoreText(int score)
    {
        scoreText.text = ScoreController.Instance.Score.ToString();
    }

    void UpdatePanelScoreText(int score)
    {
        PanelScoreText.text = "Score: " +  ScoreController.Instance.Score;
    }
    void UpdateHealthPlayer(int health)
    {
        HealthPlayer.text = ScoreController.Instance.HealthPlayer.ToString();
    }
}
