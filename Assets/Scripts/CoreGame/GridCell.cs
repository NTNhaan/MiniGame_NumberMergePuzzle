using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Đại diện 1 ô grid hiển thị (chỉ cell nền). Lưu toạ độ và chuyển click cho GridController.
/// </summary>
public class GridCell : MonoBehaviour, IPointerClickHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public string ColumnType { get; private set; } // Ví dụ: Column1, Column2...

    private GridController _controller;

    public void Init(int x, int y, GridController controller)
    {
        X = x;
        Y = y;
        _controller = controller;
        ColumnType = $"Column{X + 1}"; // human-friendly (1-based)
        gameObject.name = $"Cell_{X}_{Y}_{ColumnType}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _controller?.OnCellClicked(this);
    }
}
