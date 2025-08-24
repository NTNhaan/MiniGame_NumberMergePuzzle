using System;
using UnityEngine;

namespace Audio
{
    [Serializable]
    public class Sound
    {
        public enum Name
        {
            Music_MainScene,
            Music_GamePlay,
            Sound_Click,
            Sound_GameOver,
            Sound_PopupClose,
            Sound_PopupOpen,
            Sound_Smash,  // Hit
            Sound_ReachSand,
            Sound_OnDrag,
            Sound_Dask,
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