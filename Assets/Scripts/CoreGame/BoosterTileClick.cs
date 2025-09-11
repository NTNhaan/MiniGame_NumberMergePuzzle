using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TileView))]
public class BoosterTileClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private TileView _tile;
    private void Awake()
    {
        _tile = GetComponent<TileView>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BoosterController.Instance == null) return;
        if (!BoosterController.Instance.IsActive) return; // không phải chế độ booster -> để Grid xử lý
        BoosterController.Instance.HandleTileClicked(_tile);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Debug hỗ trợ: nếu down mà không có click -> có thể bị chặn ở Up
        // Debug.Log($"BoosterTile Down {_tile?.Value}");
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        // Debug.Log($"BoosterTile Up {_tile?.Value}");
    }
}
