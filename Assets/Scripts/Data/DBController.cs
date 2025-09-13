using UnityEngine;
using UnityEngine.Events;
namespace Data
{
    public class DBController : Singleton<DBController>
    {
        #region VARIABLE
        private bool _music;
        public bool MUSIC
        {
            get => _music;
            set
            {
                _music = value;
                Save(DBKey.MUSIC, _music);
            }
        }

        private bool _sound;
        public bool SOUND
        {
            get => _sound;
            set
            {
                _sound = value;
                Save(DBKey.SOUND, _sound);
            }
        }

        private bool _vibrate;
        public bool VIBRATE
        {
            get => _vibrate;
            set
            {
                _vibrate = value;
                Save(DBKey.VIBRATE, _vibrate);
            }
        }
        private bool _tutorialCompleted;
        public bool TUTORIAL_COMPLETED
        {
            get => _tutorialCompleted;
            set
            {
                _tutorialCompleted = value;
                Save(DBKey.TUTORIAL_COMPLETED, _tutorialCompleted);
            }
        }
        private int _bestScore;

        public int BEST_SCORE
        {
            get => _bestScore;
            set
            {
                _bestScore = value;
                Save(DBKey.BEST_SCORE, _bestScore);
            }
        }
        private int _highestMissionBlock;
        public int HIGHEST_MISSION_BLOCK
        {
            get => _highestMissionBlock;
            set
            {
                _highestMissionBlock = value;
                Save(DBKey.HIGHEST_MISSION_BLOCK, _highestMissionBlock);
            }
        }
        private int _coin;
        public int COIN
        {
            get => _coin;
            set
            {
                _coin = value;
                Save(DBKey.COIN, _coin);
                EventManager.CoinChanged(_coin);
            }
        }
        #endregion

        protected override void CustomAwake()
        {
            Initializing();
        }
        void Initializing()
        {
            CheckDependency(DBKey.MUSIC, key => MUSIC = true);
            CheckDependency(DBKey.SOUND, key => SOUND = true);
            CheckDependency(DBKey.VIBRATE, key => VIBRATE = true);
            CheckDependency(DBKey.TUTORIAL_COMPLETED, key => TUTORIAL_COMPLETED = false);
            CheckDependency(DBKey.BEST_SCORE, key => BEST_SCORE = 0);
            CheckDependency(DBKey.HIGHEST_MISSION_BLOCK, key => HIGHEST_MISSION_BLOCK = 0);
            CheckDependency(DBKey.COIN, key => COIN = 150);

            Load();
        }

        #region MainFucntions
        void CheckDependency(string key, UnityAction<string> onComplete)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                onComplete?.Invoke(key);
            }
        }
        public void Save<T>(string key, T values)
        {
            if (typeof(T) == typeof(int))
                PlayerPrefs.SetInt(key, (int)(object)values);
            else if (typeof(T) == typeof(bool))
                PlayerPrefs.SetInt(key, (bool)(object)values ? 1 : 0);
            else if (typeof(T) == typeof(string))
                PlayerPrefs.SetString(key, values as string);
            else if (typeof(T) == typeof(float))
                PlayerPrefs.SetFloat(key, (float)(object)values);
            else
            {
                try
                {
                    string json = JsonUtility.ToJson(values);
                    PlayerPrefs.SetString(key, json);
                }
                catch (UnityException e)
                {
                    throw new UnityException(e.Message);
                }
            }

            PlayerPrefs.Save();
        }
        void Load()
        {
            _sound = LoadDataByKey<bool>(DBKey.SOUND);
            _music = LoadDataByKey<bool>(DBKey.MUSIC);
            _vibrate = LoadDataByKey<bool>(DBKey.VIBRATE);
            _tutorialCompleted = LoadDataByKey<bool>(DBKey.TUTORIAL_COMPLETED);
            _bestScore = LoadDataByKey<int>(DBKey.BEST_SCORE);
            _highestMissionBlock = LoadDataByKey<int>(DBKey.HIGHEST_MISSION_BLOCK);
            _coin = LoadDataByKey<int>(DBKey.COIN);
            EventManager.CoinChanged(_coin);
        }

        public T LoadDataByKey<T>(string key)
        {
            if (typeof(T) == typeof(int))
                return (T)(object)PlayerPrefs.GetInt(key);
            else if (typeof(T) == typeof(bool))
                return (T)(object)(PlayerPrefs.GetInt(key) == 1);
            else if (typeof(T) == typeof(string))
                return (T)(object)PlayerPrefs.GetString(key);
            else if (typeof(T) == typeof(float))
                return (T)(object)PlayerPrefs.GetFloat(key);
            else
            {
                string json = PlayerPrefs.GetString(key);
                return JsonUtility.FromJson<T>(json);
            }
        }
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        #endregion
    }
}

public class DBKey
{
    public readonly static string SOUND = "SOUND";
    public readonly static string MUSIC = "MUSIC";
    public readonly static string VIBRATE = "VIBRATE";
    public static readonly string BEST_SCORE = "BEST_SCORE";
    public readonly static string TUTORIAL_COMPLETED = "TUTORIAL_COMPLETED";
    public static readonly string HIGHEST_MISSION_BLOCK = "HIGHEST_MISSION_BLOCK";
    public static readonly string COIN = "COIN";
}