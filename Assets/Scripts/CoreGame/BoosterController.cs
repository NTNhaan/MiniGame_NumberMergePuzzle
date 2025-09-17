using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Data;

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
    [SerializeField] private Text textCover;

    [Header("Booster Guide Texts")]
    [SerializeField, TextArea] private string destroyGuide = "Choose a block to destroy";
    [SerializeField, TextArea] private string swapGuide = "Choose two block to swap";
    [SerializeField, TextArea] private string mergeGuide = "Choose one block to apply magnet";
    [SerializeField, TextArea] private string notMatchGuide = "Not match. Find same number";
    [SerializeField, TextArea] private string notEnoughCoins = "Not enough coins";


    private BoosterMode _mode = BoosterMode.None;
    private TileView _firstSelection;
    private Coroutine _notMatchRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (tileSystem == null) tileSystem = FindFirstObjectByType<TileSystem>();
        if (textCover != null) textCover.gameObject.SetActive(false);
    }

    private bool CanAffordBooster()
    {
        if (DBController.Instance == null) return true; // allow if DB not ready
        return DBController.Instance.COIN >= DataConfig.COIN_BOOSTER;
    }

    private bool TrySpendBooster()
    {
        if (DBController.Instance == null) return true;
        if (DBController.Instance.COIN < DataConfig.COIN_BOOSTER) return false;
        DBController.Instance.COIN -= DataConfig.COIN_BOOSTER; // triggers EventManager.CoinChanged via DBController
        return true;
    }

    public void SetPanel(GameObject panel) => boosterPanel = panel;

    public void ActivateDestroy()
    {
        if (tileSystem != null && tileSystem.Busy) return; // tránh dùng trong lúc merge
        if (!CanAffordBooster()) { OpenShopForCoins(); return; }
        _mode = BoosterMode.DestroySelect;
        _firstSelection = null;
        ShowPanel(true);
        UpdateCoverText();
    }
    public void ActivateSwap()
    {
        if (tileSystem != null && tileSystem.Busy) return;
        if (!CanAffordBooster()) { OpenShopForCoins(); return; }
        _mode = BoosterMode.SwapFirst;
        _firstSelection = null;
        ShowPanel(true);
        UpdateCoverText();
    }
    public void ActivateMerge()
    {
        if (tileSystem != null && tileSystem.Busy) return;
        if (!CanAffordBooster()) { OpenShopForCoins(); return; }
        _mode = BoosterMode.MergeFirst;
        _firstSelection = null;
        ShowPanel(true);
        UpdateCoverText();
    }
    public void Cancel()
    {
        _mode = BoosterMode.None;
        _firstSelection = null;
        if (_notMatchRoutine != null) { StopCoroutine(_notMatchRoutine); _notMatchRoutine = null; }
        ShowPanel(false);
        UpdateCoverText();
    }

    public bool IsActive => _mode != BoosterMode.None;
    public BoosterMode CurrentMode => _mode;

    private void ShowPanel(bool v)
    {
        if (boosterPanel != null) boosterPanel.SetActive(v);
        if (textCover != null) textCover.gameObject.SetActive(v && _mode != BoosterMode.None);
    }
    private void UpdateCoverText()
    {
        if (textCover == null) return;
        string msg = "";
        switch (_mode)
        {
            case BoosterMode.DestroySelect: msg = destroyGuide; break;
            case BoosterMode.SwapFirst:
            case BoosterMode.SwapSecond:
                msg = swapGuide; break;
            case BoosterMode.MergeFirst:
            case BoosterMode.MergeSecond:
                msg = mergeGuide; break;
            default: msg = ""; break;
        }
        textCover.text = msg;
        textCover.gameObject.SetActive(_mode != BoosterMode.None && (boosterPanel == null || boosterPanel.activeSelf));
    }

    private void ShowNotMatchHint()
    {
        if (textCover == null) return;
        if (_notMatchRoutine != null) { StopCoroutine(_notMatchRoutine); _notMatchRoutine = null; }
        _notMatchRoutine = StartCoroutine(NotMatchHintCoroutine());
    }

    private IEnumerator NotMatchHintCoroutine()
    {
        textCover.text = notMatchGuide;
        textCover.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        // Revert to the default guide if still in merge selection
        if (_mode == BoosterMode.MergeSecond || _mode == BoosterMode.MergeFirst)
        {
            textCover.text = mergeGuide;
            textCover.gameObject.SetActive(true);
        }
        _notMatchRoutine = null;
    }

    // New handling: open shop instead of temporary text when lacking coins
    private void OpenShopForCoins()
    {
        Cancel();
        if (ShopController.Instance != null)
        {
            ShopController.Instance?.ShowShop();
        }
        else
        {
            // Fallback old behavior
            if (textCover != null)
            {
                ShowPanel(true);
                textCover.text = notEnoughCoins;
                textCover.gameObject.SetActive(true);
            }
        }
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
                if (!CanAffordBooster()) { OpenShopForCoins(); break; }
                if (tileSystem.TryDestroyTile(tile))
                {
                    if (TrySpendBooster())
                        Cancel();
                }
                break;

            case BoosterMode.SwapFirst:
                _firstSelection = tile;
                _mode = BoosterMode.SwapSecond;
                UpdateCoverText();
                break;

            case BoosterMode.SwapSecond:
                if (_firstSelection == null) { _mode = BoosterMode.SwapFirst; UpdateCoverText(); break; }
                if (_firstSelection == tile) { Cancel(); break; }
                if (!CanAffordBooster()) { OpenShopForCoins(); break; }
                if (tileSystem.TrySwapTiles(_firstSelection, tile))
                {
                    if (TrySpendBooster())
                        Cancel();
                }
                break;

            case BoosterMode.MergeFirst:
                _firstSelection = tile;
                _mode = BoosterMode.MergeSecond;
                UpdateCoverText();
                break;

            case BoosterMode.MergeSecond:
                if (_firstSelection == null) { _mode = BoosterMode.MergeFirst; UpdateCoverText(); break; }
                if (_firstSelection == tile) { Cancel(); break; }
                if (_firstSelection.Value != tile.Value)
                {
                    // Chọn sai cặp: hiển thị hướng dẫn notMatch và tiếp tục chờ chọn block thứ 2
                    ShowNotMatchHint();
                    // giữ nguyên _firstSelection và _mode = MergeSecond
                    break;
                }
                if (!CanAffordBooster()) { OpenShopForCoins(); break; }
                if (tileSystem.TryMergePair(_firstSelection, tile))
                {
                    if (TrySpendBooster())
                        Cancel();
                }
                break;
        }
    }
}
