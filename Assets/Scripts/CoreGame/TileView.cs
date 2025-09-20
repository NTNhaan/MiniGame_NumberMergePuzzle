using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Text valueText;
    [Header("Sprite Options")]
    [SerializeField] private bool useSpriteNativeSize = false;
    private Vector2 _initialSize;

    private void Awake()
    {
        if (background != null)
        {
            var rt = background.rectTransform;
            _initialSize = rt.sizeDelta;
        }
    }

    public int Value { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }

    public void Initialize(int value, Color color, int x, int y)
    {
        Value = value;
        X = x;
        Y = y;
        if (background != null) background.color = color;
        if (valueText != null) valueText.text = value.ToString();
        if (background != null && background.raycastTarget == false)
            background.raycastTarget = true;
        name = $"Tile_{value}_{x}_{y}";
    }

    public void SetSprite(Sprite s)
    {
        if (background == null || s == null) return;
        var rt = background.rectTransform;
        Vector2 prevSize = rt.sizeDelta;
        background.sprite = s;
        if (useSpriteNativeSize)
        {
            background.SetNativeSize();
        }
        else
        {
            if (_initialSize != Vector2.zero)
                rt.sizeDelta = prevSize == Vector2.zero ? _initialSize : prevSize;
        }
    }

    public void UpdateValue(int newValue)
    {
        Value = newValue;
        if (valueText != null) valueText.text = newValue.ToString();
        name = $"Tile_{newValue}_{X}_{Y}";
    }

    public void SetGridPosition(int newX, int newY)
    {
        X = newX;
        Y = newY;
        name = $"Tile_{Value}_{X}_{Y}";
    }
}