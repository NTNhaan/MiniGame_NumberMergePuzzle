using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach this to a root Canvas (or any always-active GameObject in the scene that has access to an EventSystem & GraphicRaycaster)
// Purpose: While a booster mode is active, any click/tap that does NOT hit a TileView will cancel the booster.
// This matches the requested UX: click on a valid tile applies booster logic (handled elsewhere), click outside cancels.
public class BoosterOutsideClickCanceller : MonoBehaviour
{
    [SerializeField] private BoosterController boosterController; // optional explicit reference
    [SerializeField] private GraphicRaycaster graphicRaycaster;   // raycaster on the UI canvas containing tiles
    [SerializeField] private EventSystem eventSystem;             // usually auto-filled

    // If true, clicking any non-tile UI (score, buttons) also cancels booster (default desired behavior: outside board cancels)
    [SerializeField] private bool cancelOnAnyNonTileUI = true;

    // Reuse list to avoid GC
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

        // Mouse / Editor
        if (Input.GetMouseButtonDown(0))
        {
            if (!ClickedOnTile(Input.mousePosition))
                boosterController.Cancel();
        }

        // Touch (support multiple touches; only need to react to first began inside frame)
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
            return false; // treat as outside (will cancel) if we can't verify

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
            // Check if this hit is part of a TileView (or its children)
            if (go.GetComponentInParent<TileView>() != null)
                return true; // clicked a tile; booster should continue and tile click script will handle

            // If we only want to cancel when clicking empty space (not any UI), then encountering other UI should count as not cancelling
            if (!cancelOnAnyNonTileUI)
                return true; // treat other UI as safe (not cancelling)
        }

        // No tile found in hits -> consider as outside board
        return false;
    }
}
