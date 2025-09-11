using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Brick Set", fileName = "BrickSet")]
public class BrickSet : ScriptableObject
{
    [System.Serializable]
    public class BrickInfo
    {
        public int number = 2;
        public Sprite sprite;
    }

    public List<BrickInfo> bricks = new();

    public int GetRandomNumber()
    {
        if (bricks == null || bricks.Count == 0) return 2;
        int idx = Random.Range(0, bricks.Count);
        return bricks[idx].number;
    }

    public Sprite GetSprite(int number)
    {
        if (bricks == null) return null;
        for (int i = 0; i < bricks.Count; i++)
        {
            if (bricks[i].number == number) return bricks[i].sprite;
        }
        return null;
    }

    public bool Contains(int number)
    {
        if (bricks == null) return false;
        for (int i = 0; i < bricks.Count; i++)
        {
            if (bricks[i].number == number) return true;
        }
        return false;
    }
}
