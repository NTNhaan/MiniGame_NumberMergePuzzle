using System.Collections;
using UnityEngine;

public class BoosterController : MonoBehaviour
{
    public static BoosterController Instance { get; private set; }

    public enum BoosterMode
    {
        None,
        DestroySelect,
        SwapFirst,
        SwapSecond,
        MergeFirst,
        MergeSecond
    }

    [SerializeField] private TileSystem tileSystem;
    [SerializeField] private GameObject boosterPanel;

    private BoosterMode _mode = BoosterMode.None;
    private TileView _firstSelection;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (tileSystem == null) tileSystem = FindFirstObjectByType<TileSystem>();
    }

    public void SetPanel(GameObject panel) => boosterPanel = panel;

    public void ActivateDestroy()
    {
        if (tileSystem != null && tileSystem.Busy) return; // tránh dùng trong lúc merge
        _mode = BoosterMode.DestroySelect;
        _firstSelection = null;
        ShowPanel(true);
    }
    public void ActivateSwap()
    {
        if (tileSystem != null && tileSystem.Busy) return;
        _mode = BoosterMode.SwapFirst;
        _firstSelection = null;
        ShowPanel(true);
    }
    public void ActivateMerge()
    {
        if (tileSystem != null && tileSystem.Busy) return;
        _mode = BoosterMode.MergeFirst;
        _firstSelection = null;
        ShowPanel(true);
    }
    public void Cancel()
    {
        _mode = BoosterMode.None;
        _firstSelection = null;
        ShowPanel(false);
    }

    public bool IsActive => _mode != BoosterMode.None;
    public BoosterMode CurrentMode => _mode;

    private void ShowPanel(bool v)
    {
        if (boosterPanel != null) boosterPanel.SetActive(v);
    }

    public void HandleTileClicked(TileView tile)
    {
        if (tile == null) return;
        if (tileSystem == null) tileSystem = FindFirstObjectByType<TileSystem>();
        if (_mode == BoosterMode.None) return;
        if (tileSystem != null && tileSystem.Busy) return; // chờ merge/spawn xong

        switch (_mode)
        {
            case BoosterMode.DestroySelect:
                if (tileSystem.TryDestroyTile(tile))
                {
                    Cancel();
                }
                break;
            case BoosterMode.SwapFirst:
                _firstSelection = tile;
                _mode = BoosterMode.SwapSecond;
                break;
            case BoosterMode.SwapSecond:
                if (_firstSelection == null)
                {
                    _mode = BoosterMode.SwapFirst; break;
                }
                if (_firstSelection == tile) { Cancel(); break; }
                if (tileSystem.TrySwapTiles(_firstSelection, tile))
                {
                    Cancel();
                }
                break;
            case BoosterMode.MergeFirst:
                _firstSelection = tile;
                _mode = BoosterMode.MergeSecond;
                break;
            case BoosterMode.MergeSecond:
                if (_firstSelection == null)
                {
                    _mode = BoosterMode.MergeFirst; break;
                }
                if (_firstSelection == tile) { Cancel(); break; }
                if (_firstSelection.Value != tile.Value)
                {
                    // giá trị khác nhau -> bỏ chọn cũ, coi tile này là chọn mới
                    _firstSelection = tile; _mode = BoosterMode.MergeSecond; break;
                }
                if (tileSystem.TryMergePair(_firstSelection, tile))
                {
                    Cancel();
                }
                break;
        }
    }
}
