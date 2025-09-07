using UnityEngine;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour, IPointerClickHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public string ColumnType { get; private set; }

    private GridController _controller;

    public void Init(int x, int y, GridController controller)
    {
        X = x;
        Y = y;
        _controller = controller;
        ColumnType = $"Column{X + 1}";
        gameObject.name = $"Cell_{X}_{Y}_{ColumnType}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _controller?.OnCellClicked(this);
    }
}
