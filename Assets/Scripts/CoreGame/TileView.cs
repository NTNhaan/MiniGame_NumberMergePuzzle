using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Text valueText; // Using legacy Text; replace with TMP_Text if using TextMeshPro

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
        name = $"Tile_{value}_{x}_{y}";
    }
}