using System;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;


public static class WalletServer
{
    const string CURRENCY = "CO";

    [Serializable] class BalanceResp { public int balance; public string error; }

    public static void Refresh(Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            r => {
                int bal = (r.VirtualCurrency != null && r.VirtualCurrency.TryGetValue(CURRENCY, out int v)) ? v : 0;
                onOk?.Invoke(bal);
            },
            e => onErr?.Invoke(e.GenerateErrorReport()));
    }

    public static void Grant(int amount, Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest{
            FunctionName = "grantCoins",
            FunctionParameter = new { amount = amount },
            GeneratePlayStreamEvent = false
        },
        r => {
            var data = PlayFabSimpleJson.DeserializeObject<BalanceResp>(r.FunctionResult?.ToString() ?? "{}");
            if (!string.IsNullOrEmpty(data?.error)) onErr?.Invoke(data.error);
            else onOk?.Invoke(data != null ? data.balance : 0);
        },
        e => onErr?.Invoke(e.GenerateErrorReport()));
    }

    public static void Spend(int amount, Action<int> onOk, Action<string> onErr)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest{
            FunctionName = "spendCoins",
            FunctionParameter = new { amount = amount },
            GeneratePlayStreamEvent = false
        },
        r => {
            var data = PlayFabSimpleJson.DeserializeObject<BalanceResp>(r.FunctionResult?.ToString() ?? "{}");
            if (!string.IsNullOrEmpty(data?.error)) onErr?.Invoke(data.error);
            else onOk?.Invoke(data != null ? data.balance : 0);
        },
        e => onErr?.Invoke(e.GenerateErrorReport()));
    }
}
