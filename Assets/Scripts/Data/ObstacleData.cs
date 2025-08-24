using System;
using System.Collections;
using UnityEngine;
namespace Data
{
    [Serializable]
    public class ObstacleData
    {
        public ObstacleType obstacleType;
        public AnimatorOverrideController animatorOverride;
        public Sprite sprite;
        public int pointValue;
        public float speed;
        public float spawnRates;
    }
}
public enum ObstacleType
{
    Balloon_1,
    Balloon_2, 
    Balloon_3,
    Balloon_4,
    Balloon_5,
    Balloon_6,
    Balloon_7,
    Balloon_8,
    Balloon_9,
    None,
}