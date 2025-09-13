using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Events;
using Unity.Services.Core;
using System.Threading.Tasks;
using Data;

[System.Serializable]
public class ItemIap
{
    public string key;
    public ProductType productType;
}

public class IAPController : Singleton<IAPController>, IStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;
    [SerializeField] private List<ItemIap> lstKeyCode;
    [SerializeField] private bool isInitialized = false;

    public UnityAction<string> OnPurchaseSuccess;
    public UnityAction<string, PurchaseFailureReason> OnPurchaseFailedAction;

    [System.Serializable]
    public class CoinReward
    {
        public string productId;
        public int coins;
    }
    [Header("Coin Rewards")]
    [SerializeField] private List<CoinReward> coinRewards = new List<CoinReward>();

    private void Awake()
    {
        InitializeUnityGamingServices();
        InitializePurchasing();
    }

    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        for (int i = 0; i < lstKeyCode.Count; i++)
        {
            builder.AddProduct(lstKeyCode[i].key, lstKeyCode[i].productType);
            Debug.Log($"AddProduct: {lstKeyCode[i].key} - {lstKeyCode[i].productType}");
        }

        UnityPurchasing.Initialize(this, builder);
    }
    private async Task InitializeUnityGamingServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Gaming Services initialized!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Gaming Services: {e}");
        }
    }
    public bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    public bool BuyProduct(string productId, out string resultMessage)
    {
        resultMessage = "";
        if (!IsInitialized())
        {
            resultMessage = "IAP not initialized.";
            return false;
        }

        Product product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase)
        {
            storeController.InitiatePurchase(product);
            Debug.Log("BuyProduct: " + product.definition.id);
            return true;
        }
        else
        {
            resultMessage = "Product not found or not available for purchase.";
            Debug.LogError("BuyProduct FAIL: " + resultMessage);
            return false;
        }
    }

    public void RestorePurchases()
    {
        if (!IsInitialized()) return;

        var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
        // apple.RestoreTransactions((success) =>
        // {
        //     Debug.Log("RestorePurchases: " + success);
        // });
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("Unity IAP Initialization Failed: " + error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log("Purchase successful: " + args.purchasedProduct.definition.id);
        if (args.purchasedProduct.hasReceipt)
        {
            Debug.Log("Receipt: " + args.purchasedProduct.receipt);
            var pid = args.purchasedProduct.definition.id;
            var reward = coinRewards.Find(r => r.productId == pid);
            if (reward != null && reward.coins > 0 && DBController.Instance != null)
            {
                DBController.Instance.COIN += reward.coins;
                Debug.Log($"Added {reward.coins} coins for {pid}");
            }
            OnPurchaseSuccess?.Invoke(args.purchasedProduct.definition.id);
            storeController.ConfirmPendingPurchase(args.purchasedProduct);
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError("Purchase failed: " + product.definition.id + " Reason: " + failureReason);
        OnPurchaseFailedAction?.Invoke(product.definition.id, failureReason);
    }

    public Product GetProductByKey(string key)
    {
        if (!IsInitialized()) return null;

        return storeController.products.WithID(key);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError("Unity IAP Initialization Failed: " + error + " Message: " + message);
        IsInitialized();
        // throw new NotImplementedException();
    }
}
