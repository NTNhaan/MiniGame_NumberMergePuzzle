using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoosterOutsideClickCanceller : MonoBehaviour
{
    [SerializeField] private BoosterController boosterController; 
    [SerializeField] private GraphicRaycaster graphicRaycaster; 
    [SerializeField] private EventSystem eventSystem;         

    [SerializeField] private bool cancelOnAnyNonTileUI = true;

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(16);

    private void Awake()
    {
        if (boosterController == null) boosterController = BoosterController.Instance;
        if (graphicRaycaster == null) graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (eventSystem == null) eventSystem = EventSystem.current;
    }

    private void Update()
    {
        if (boosterController == null || !boosterController.IsActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!ClickedOnTile(Input.mousePosition))
                boosterController.Cancel();
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    if (!ClickedOnTile(t.position))
                        boosterController.Cancel();
                }
            }
        }
    }

    private bool ClickedOnTile(Vector2 screenPos)
    {
        if (graphicRaycaster == null || eventSystem == null)
            return false;

        var ped = new PointerEventData(eventSystem)
        {
            position = screenPos
        };
        _raycastResults.Clear();
        graphicRaycaster.Raycast(ped, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var go = _raycastResults[i].gameObject;
            if (go == null) continue;
            if (go.GetComponentInParent<TileView>() != null)
                return true; 

            if (!cancelOnAnyNonTileUI)
                return true; 
        }

        return false;
    }
}
