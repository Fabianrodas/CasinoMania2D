using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

public class WalletUI : MonoBehaviour
{
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI walletText;
    const string C = "CO";

    void OnEnable()
    {
        if (usernameText) usernameText.text = string.IsNullOrEmpty(Session.Username) ? "Invitado" : Session.Username;

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), r =>
        {
            int bal = r.VirtualCurrency != null && r.VirtualCurrency.TryGetValue(C, out int v) ? v : 0;
            Session.Wallet = bal;
            if (walletText) walletText.text = bal.ToString();
        }, e => Debug.LogError(e.GenerateErrorReport()));
    }

    public void OnClickAdd100()  => Change(+100);
    public void OnClickBet50()   => Change(-50);

    void Change(int delta)
    {
        if (delta >= 0)
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest {
                FunctionName = "grantCoins", FunctionParameter = new { amount = delta }
            }, res => ApplyBalanceFromCS(res), err => Debug.LogError(err.GenerateErrorReport()));
        else
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest {
                FunctionName = "spendCoins", FunctionParameter = new { amount = -delta }
            }, res => ApplyBalanceFromCS(res), err => Debug.LogError(err.GenerateErrorReport()));
    }

    void ApplyBalanceFromCS(ExecuteCloudScriptResult res)
    {
        var dict = res.FunctionResult as System.Collections.IDictionary;
        if (dict != null && dict.Contains("balance"))
        {
            Session.Wallet = (int)(long)dict["balance"];
            if (walletText) walletText.text = Session.Wallet.ToString();
        }
    }
}
