using Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ItemCoin : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private int coinReceive;
    [SerializeField] private Text txtPrice;
    [SerializeField] private Text txtCoin;
    UnityAction<string> actionOnClick;

    public string Key => key;
    public int CoinReceive => coinReceive;

    public void Init(string txtPrice, UnityAction<string> actionOnClick)
    {
        this.txtPrice.text = txtPrice;
        txtCoin.text = coinReceive.ToString();
        Debug.Log($"CheckInitCoin {coinReceive}");
        this.actionOnClick = actionOnClick;
    }
    private void Start()
    {
        // Kiểm tra DatabaseController khi item được tạo
        if (DBController.Instance == null)
        {
            Debug.LogError("DatabaseController is not in the scene! Please add it to the scene.");
        }
    }

    public void OnClickItem()
    {
        actionOnClick?.Invoke(key);
    }
    public void OnSuccess()
    {
        // Reward is applied in IAPController.ProcessPurchase. Use this hook for UI effects only.
    }
}
