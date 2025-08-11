using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine.SceneManagement;

public class GlobalUI : MonoBehaviour
{
    public static GlobalUI Instance { get; private set; }

    [Header("Refs")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI walletText;

    [Header("Guest")]
    public int guestStartBalance = 1000;
    const string GUEST_KEY = "guest_wallet";

    void Awake()
    {
        if (!Session.IsLoggedIn) ResetGuestWallet();

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Siempre que arranca la app, si NO está logueado, resetea a 1000
        if (!Session.IsLoggedIn)
        {
            PlayerPrefs.SetInt(GUEST_KEY, guestStartBalance);
            PlayerPrefs.Save();
        }
    }

    void Start() => Refresh();

    public int CurrentWallet
    {
        get
        {
            if (Session.IsLoggedIn) return Session.Wallet;
            return PlayerPrefs.GetInt(GUEST_KEY, guestStartBalance);
        }
    }

    public void Refresh()
    {
        if (!Session.IsLoggedIn)
        {
            if (usernameText) usernameText.text = "Guest";
            if (walletText)   walletText.text   = CurrentWallet.ToString();
            return;
        }

        if (usernameText) usernameText.text = Session.Username;
        if (walletText)   walletText.text   = Session.Wallet.ToString();
    }

    // ---------- Cambios de saldo ----------

    public void TrySpend(int amount, System.Action<bool> done)
    {
        if (amount <= 0) { done?.Invoke(true); return; }

        if (!Session.IsLoggedIn)
        {
            int w = CurrentWallet;
            if (w < amount) { done?.Invoke(false); return; }
            PlayerPrefs.SetInt(GUEST_KEY, w - amount);
            Refresh();
            done?.Invoke(true);
            return;
        }

        var req = new ExecuteCloudScriptRequest {
            FunctionName = "spendCoins",
            FunctionParameter = new { amount = amount },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(req, _ =>
        {
            // Refrescamos saldo con GetUserInventory (sin JSON)
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), inv =>
            {
                int bal = 0;
                inv.VirtualCurrency?.TryGetValue("CO", out bal);
                Session.Wallet = bal;
                Refresh();
                done?.Invoke(true);
            },
            e => {
                UnityEngine.Debug.LogError("[Wallet] GetUserInventory error: " + e.GenerateErrorReport());
                done?.Invoke(false);
            });
        },
        e => {
            UnityEngine.Debug.LogError("[Wallet] spendCoins error: " + e.GenerateErrorReport());
            done?.Invoke(false);
        });
    }

    public void Grant(int amount, System.Action<bool> done = null)
    {
        if (amount <= 0) { done?.Invoke(true); return; }

        if (!Session.IsLoggedIn)
        {
            int w = CurrentWallet + amount;
            PlayerPrefs.SetInt(GUEST_KEY, Mathf.Max(0, w));
            Refresh();
            done?.Invoke(true);
            return;
        }

        var req = new ExecuteCloudScriptRequest {
            FunctionName = "grantCoins",
            FunctionParameter = new { amount = amount },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(req, _ =>
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), inv =>
            {
                int bal = 0;
                inv.VirtualCurrency?.TryGetValue("CO", out bal);
                Session.Wallet = bal;
                Refresh();
                done?.Invoke(true);
            },
            e => {
                UnityEngine.Debug.LogError("[Wallet] GetUserInventory error: " + e.GenerateErrorReport());
                done?.Invoke(false);
            });
        },
        e => {
            UnityEngine.Debug.LogError("[Wallet] grantCoins error: " + e.GenerateErrorReport());
            done?.Invoke(false);
        });
    }

    void ResetGuestWallet()
    {
        PlayerPrefs.SetInt(GUEST_KEY, guestStartBalance);
        PlayerPrefs.Save();
    }

    public void OnClickLogout()
    {
        ResetGuestWallet();

        PlayFab.PlayFabClientAPI.ForgetAllCredentials();
        Session.Clear();

        // Guest vuelve a 1000 al salir de una cuenta
        PlayerPrefs.SetInt(GUEST_KEY, guestStartBalance);
        PlayerPrefs.Save();

        Refresh();
        SceneManager.LoadScene("LoginMenu");
    }

}
