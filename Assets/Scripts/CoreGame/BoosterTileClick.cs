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
        if (!BoosterController.Instance.IsActive) return;
        BoosterController.Instance.HandleTileClicked(_tile);
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }
    public void OnPointerUp(PointerEventData eventData)
    {
    }
}
