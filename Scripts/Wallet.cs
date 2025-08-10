using System;
using PlayFab;
using PlayFab.ClientModels;

public static class Wallet
{
    const string CUR = "CO";

    public static void Refresh(Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            r => {
                int bal = r.VirtualCurrency != null && r.VirtualCurrency.TryGetValue(CUR, out int v) ? v : 0;
                onOk?.Invoke(bal);
            },
            e => onErr?.Invoke(e.GenerateErrorReport()));
    }

    public static void Add(int amount, Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest {
            VirtualCurrency = CUR, Amount = amount
        },
        r => onOk?.Invoke(r.Balance),
        e => onErr?.Invoke(e.GenerateErrorReport()));
    }

    public static void Subtract(int amount, Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.SubtractUserVirtualCurrency(new SubtractUserVirtualCurrencyRequest {
            VirtualCurrency = CUR, Amount = amount
        },
        r => onOk?.Invoke(r.Balance),
        e => onErr?.Invoke(e.GenerateErrorReport()));
    }
}
