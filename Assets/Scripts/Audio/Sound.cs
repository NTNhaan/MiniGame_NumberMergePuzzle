using System;
using UnityEngine;

namespace Audio
{
    [Serializable]
    public class Sound
    {
        public enum Name
        {
            Music_GamePlay,
            Sound_GameOver,
            Sound_Merge,
            Sound_LevelUp,
            Sound_Open_Close_Popup,
            Sound_PutDown_Shape,
            Sound_Btn_Click
        }

        public Name name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1;
        [HideInInspector]
        public AudioSource source;
        public bool loop = false;
    }
}