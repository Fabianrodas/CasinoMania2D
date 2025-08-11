using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoginUIController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField userOrEmailInput;
    public TMP_InputField passwordInput;

    [Header("UI")]
    public TextMeshProUGUI feedbackText;
    public GlobalUI globalUIPrefab;          // HUD global (opcional)
    public UnityEngine.UI.Button loginButton; // arrástralo si quieres desactivar el botón mientras se loguea

    const string CURRENCY = "CO";

    // estado interno
    bool busy = false;
    LoginResult cachedLoginResult; // para FinishLogin después del backoff

    void Start() {
        if (PlayFabClientAPI.IsClientLoggedIn()) {
            SceneManager.LoadScene("GameSelection");
            return;
        }
    }

    public void OnClickLogin()
    {
        if (LoginGuard.I != null && !LoginGuard.I.CanLoginNow("login")) return;
        if (busy) return;
        busy = true;
        if (loginButton) loginButton.interactable = false;

        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            PlayFabSettings.staticSettings.TitleId = "1EF38A";

        if (string.IsNullOrWhiteSpace(userOrEmailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            SetFeedback("Completa usuario/email y contraseña");
            busy = false;
            if (loginButton) loginButton.interactable = true;
            return;
        }

        SetFeedback("Iniciando sesión...");

        var infoParams = new GetPlayerCombinedInfoRequestParams
        {
            GetUserInventory   = true,
            GetUserAccountInfo = true
        };

        if (userOrEmailInput.text.Contains("@"))
        {
            PlayFabClientAPI.LoginWithEmailAddress(new LoginWithEmailAddressRequest
            {
                Email = userOrEmailInput.text.Trim(),
                Password = passwordInput.text,
                InfoRequestParameters = infoParams
            }, OnLoginOK, OnLoginErr);
        }
        else
        {
            PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest
            {
                Username = userOrEmailInput.text.Trim(),
                Password = passwordInput.text,
                InfoRequestParameters = infoParams
            }, OnLoginOK, OnLoginErr);
        }
    }

    void OnLoginOK(LoginResult r)
    {
        cachedLoginResult = r;

        // 1) si vino el saldo en el LoginResult, úsalo y listo
        int wallet;
        if (r.InfoResultPayload?.UserVirtualCurrency != null &&
            r.InfoResultPayload.UserVirtualCurrency.TryGetValue(CURRENCY, out wallet))
        {
            FinishLogin(r, wallet);
            return;
        }

        // 2) si no vino, pedimos el inventario con backoff anti‑throttle
        GetInventoryWithBackoff();
    }

    // --- BACKOFF ANTI-THROTTLE ---
    void GetInventoryWithBackoff(int attempt = 0)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), inv =>
        {
            int bal = 0;
            inv.VirtualCurrency?.TryGetValue(CURRENCY, out bal);
            FinishLogin(cachedLoginResult, bal);
        },
        err =>
        {
            // 429 = throttling; reintenta hasta 3 veces: 0.5s, 1s, 2s
            if (err.HttpCode == 429 && attempt < 3)
            {
                float delay = Mathf.Min(0.5f * Mathf.Pow(2, attempt), 2f);
                StartCoroutine(RetryInv(delay, attempt + 1));
            }
            else
            {
                Debug.LogError("GetUserInventory: " + err.GenerateErrorReport());
                FinishLogin(cachedLoginResult, 0); // último recurso
            }
        });
    }

    IEnumerator RetryInv(float delay, int nextAttempt)
    {
        yield return new WaitForSeconds(delay);
        GetInventoryWithBackoff(nextAttempt);
    }
    // --- FIN BACKOFF ---

    void FinishLogin(LoginResult r, int wallet)
    {
        // Rellena sesión
        Session.Ticket    = r.SessionTicket;
        Session.PlayFabId = r.PlayFabId;
        Session.Username  = r.InfoResultPayload?.AccountInfo?.Username ?? userOrEmailInput.text.Trim();
        Session.Email     = r.InfoResultPayload?.AccountInfo?.PrivateInfo?.Email ?? "";
        Session.Wallet    = wallet;

        SetFeedback($"¡Bienvenido {Session.Username}!");

        // Instancia HUD global si no existe y refresca (NO debe llamar APIs)
        if (GlobalUI.Instance == null && globalUIPrefab != null) Instantiate(globalUIPrefab);
        GlobalUI.Instance?.Refresh();

        // liberar compuerta
        busy = false;
        if (loginButton) loginButton.interactable = true;
        LoginGuard.I?.ResetLoginGate();  
        SceneManager.LoadScene("GameSelection");
    }

    void OnLoginErr(PlayFabError e)
    {
        busy = false;
        if (loginButton) loginButton.interactable = true;
        LoginGuard.I?.ResetLoginGate();

        string nice;
        switch (e.Error)
        {
            case PlayFabErrorCode.AccountNotFound:
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                nice = "Usuario o contraseña incorrectos."; break;
            case PlayFabErrorCode.InvalidEmailOrPassword:
                nice = "Email o contraseña incorrectos."; break;
            case PlayFabErrorCode.InvalidEmailAddress:
                nice = "Email inválido."; break;
            default:
                nice = e.ErrorMessage ?? "Error de inicio de sesión."; break;
        }

        SetFeedback(nice);
        Debug.LogError("[Login] " + e.GenerateErrorReport());
    }

    void SetFeedback(string msg)
    {
        if (!feedbackText) return;
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = msg;
        Canvas.ForceUpdateCanvases();
    }
}