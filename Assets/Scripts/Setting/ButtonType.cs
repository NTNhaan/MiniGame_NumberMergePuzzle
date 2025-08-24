using System;
using UnityEngine;

namespace Setting
{
    [Serializable]
    public class ButtonType
    {
        public typeSetting type;
        public Sprite sprite;

    }
    public enum typeSetting
    {
        isOn,
        isOff
    }
}