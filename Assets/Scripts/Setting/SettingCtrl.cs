using Audio;
using Data;
using UnityEngine;
using UnityEngine.UI;
namespace Setting
{
    public class SettingCtrl : Singleton<SettingCtrl>
    {
        [SerializeField] private Image imgSound;
        [SerializeField] private Image imgMusic;
        [SerializeField] private Image imgVibration;
        [SerializeField] private ButtonType[] sprtSound;
        [SerializeField] private ButtonType[] sprtMusic;
        [SerializeField] private ButtonType[] sprtVibration;
        
        public bool SetSound()
        {
            DBController.Instance.SOUND = !DBController.Instance.SOUND;
            AudioController.Instance.SetVolumeSound(DBController.Instance.SOUND);
            UpdateSettingImage(imgSound, sprtSound, DBController.Instance.SOUND);
            return DBController.Instance.SOUND;
        }
        public bool SetVibration()
        {
            DBController.Instance.VIBRATE =  !DBController.Instance.VIBRATE;
            if (DBController.Instance.VIBRATE)
            {
                Handheld.Vibrate();
            }
            UpdateSettingImage(imgVibration, sprtVibration, DBController.Instance.VIBRATE);
            return  DBController.Instance.VIBRATE;
        }
        public bool SetMusic()
        {
            DBController.Instance.MUSIC = !DBController.Instance.MUSIC;
            AudioController.Instance.SetVolumeMusic( DBController.Instance.MUSIC);
            UpdateSettingImage(imgMusic, sprtMusic, DBController.Instance.MUSIC);
            return DBController.Instance.MUSIC;
        }
        public void UpdateSettingImage(Image imgTarget, ButtonType[] dataList, bool isOn)
        {
            var type = isOn ? typeSetting.isOn : typeSetting.isOff;
            Debug.Log($"[UpdateSetting] type: {type}");
            foreach (var data in dataList)
            {
                if (data.type == type)
                {
                    imgTarget.sprite = data.sprite;
                    break;
                }
            }
        }
        public void InitSetting()
        {
            UpdateSettingImage(imgSound, sprtSound, DBController.Instance.SOUND);
            // UpdateSettingImage(imgMusic, sprtMusic, DBController.Instance.MUSIC);
            UpdateSettingImage(imgVibration, sprtVibration, DBController.Instance.VIBRATE);
            // UpdateSettingImage(imgVibration, sprtVibration, DBController.Instance.VIBRATION);
        }
    }
}