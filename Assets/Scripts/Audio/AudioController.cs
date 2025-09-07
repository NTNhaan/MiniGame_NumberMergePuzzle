using System;
using UnityEngine;
using CandyCoded.HapticFeedback;
using Data;
namespace Audio
{
    public class AudioController : Singleton<AudioController>
    {
        public Sound arrBackgroundMusic;
        public Sound[] arrSoundEffect;

        #region InitDataSound
        private void Start()
        {
            CreateAudioSource(arrSoundEffect);
            CheckSound(DBController.Instance.MUSIC, DBController.Instance.SOUND);
            Debug.Log($"[AudioController] Start {DBController.Instance.MUSIC} {DBController.Instance.SOUND}");
            Debug.Log($"CheckScene audio loading");
        }
        private void CreateAudioSourceBackround()
        {
            arrBackgroundMusic.source = gameObject.AddComponent<AudioSource>();
            arrBackgroundMusic.source.clip = arrBackgroundMusic.clip;
            arrBackgroundMusic.source.loop = arrBackgroundMusic.loop;
        }
        private void CreateAudioSource(Sound[] sounds)
        {
            foreach (Sound sound in sounds)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.loop = sound.loop;
            }
            CreateAudioSourceBackround();
        }
        #endregion

        public void PlayBackroundMusicGameplay()
        {
            SetVolumeMusic(true);
            arrBackgroundMusic.source.Play();
        }
        public void PlayGameOverSound()
        {
            PlayEffect(Sound.Name.Sound_GameOver);
        }
        public void PlayMergeSound()
        {
            PlayEffect(Sound.Name.Sound_Merge);
        }
        public void PlayLevelUpSound()
        {
            PlayEffect(Sound.Name.Sound_LevelUp);
        }
        public void PlayOpenClosePopup()
        {
            PlayEffect(Sound.Name.Sound_Open_Close_Popup);
        }
        public void PlayPutDownShape()
        {
            PlayEffect(Sound.Name.Sound_PutDown_Shape);
        }
        public void PlaySoundButtonClick()
        {
            PlayEffect(Sound.Name.Sound_Btn_Click);
        }
        #region FunctionPlaySound
        public void PlayEffect(Sound.Name name)
        {
            Debug.Log("[SoundManager] PlayEffect: " + name);
            Sound effect = Array.Find(arrSoundEffect, effect => effect.name == name);
            if (effect == null)
            {
                Debug.LogError("Unable to play effect " + name);
                return;
            }
            effect.source.Play();
            Debug.Log("[SoundManager] PlayEffectDone: " + name);

        }

        public void StopEffect(Sound.Name name)
        {
            Sound effect = Array.Find(arrSoundEffect, effect => effect.name == name);
            if (effect == null)
            {
                Debug.LogError("Unable to play effect " + name);
                return;
            }
            effect.source.Stop();
        }

        public void SetVolumeMusic(bool status)
        {
            Debug.Log($"[UpdateSetting] Musictype: {status}");
            arrBackgroundMusic.volume = status ? 1 : 0;
            arrBackgroundMusic.source.volume = arrBackgroundMusic.volume;
        }

        public void SetVolumeSound(bool status)
        {
            Debug.Log($"[UpdateSetting] Soundtype: {status}");
            foreach (Sound sound in arrSoundEffect)
            {
                sound.volume = status ? 1 : 0;
                sound.source.volume = sound.volume;
            }
        }
        // public void Set
        public void CheckSound(bool music, bool Effect)
        {
            SetVolumeMusic(music);
            SetVolumeSound(Effect);
        }
        public void DefaultVibration()
        {
            if (DBController.Instance.VIBRATE)
                HapticFeedback.LightFeedback();
        }
        private void MediumVibration()
        {
            if (DBController.Instance.VIBRATE)
                HapticFeedback.MediumFeedback();
        }
        private void HeavyVibration()
        {
            if (DBController.Instance.VIBRATE)
                HapticFeedback.HeavyFeedback();
        }
        public void RunVibration(float intensity = 1f, int milliseconds = 50)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            Debug.Log($"[Vibration] RunVibration: intensity={intensity}, ms={milliseconds}");
        }
        #endregion
    }
}