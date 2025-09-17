using UnityEngine;
using Data;
using UnityEngine.Events;
namespace DefaultNamespace
{
    public class ScoreController : Singleton<ScoreController>
    {
        // private const string HIGH_SCORE_KEY = "HighScore";
        public event UnityAction<int> OnScoreChanged;
        public event UnityAction<int> OnHealthChanged;
        public event UnityAction<int> OnCoinsChanged; // local coin event (optional legacy)
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int HealthPlayer { get; private set; }
        [SerializeField] private GameObject iconCheckOverHighScore;
        private void Start()
        {
            Score = 0;
            HealthPlayer = 5;

            if (iconCheckOverHighScore != null)
                iconCheckOverHighScore.SetActive(false);

            // HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
            HighScore = DBController.Instance.BEST_SCORE;
            OnScoreChanged?.Invoke(Score);
            OnHealthChanged?.Invoke(HealthPlayer);
        }
        public void AddPoints(int points)
        {
            Score += points;

            if (Score > HighScore)
            {
                HighScore = Score;
                // PlayerPrefs.SetInt(HIGH_SCORE_KEY, HighScore);
                // PlayerPrefs.Save();
                DBController.Instance.BEST_SCORE = Score;
                if (iconCheckOverHighScore != null)
                    iconCheckOverHighScore.SetActive(true);

                Debug.Log($"[CheckHighScore]: {HighScore}");
            }

            OnScoreChanged?.Invoke(Score);
        }

        public void TakeDamage(int damage)
        {
            HealthPlayer -= damage;
            OnHealthChanged?.Invoke(HealthPlayer);
        }
        public void UpdateCoin()
        {
            if (DBController.Instance == null) return;
            int coins = DBController.Instance.COIN;
            Debug.Log($"[ScoreController] UpdateCoin -> {coins}");
            // Fire new unified event path
            EventManager.CoinsChanged(coins);
            // Keep optional local notify if some legacy subscriber relies on OnCoinsChanged directly on ScoreController
            OnCoinsChanged?.Invoke(coins);
        }
        public int GetHighScore() => HighScore;
        public int GetHealthPlayer() => HealthPlayer;
    }
}