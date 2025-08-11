using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class RegisterUIController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField passwordInput2;
    public Toggle ageCheck;

    [Header("UI")]
    public Button registerButton;
    public TextMeshProUGUI feedbackText;

    [Header("Prefabs (opcional)")]
    public GlobalUI globalUIPrefab; // arrástralo si quieres auto‑instanciar el HUD global

    const string CURRENCY = "CO";

    void Awake()
    {
        if (passwordInput)  passwordInput.contentType  = TMP_InputField.ContentType.Password;
        if (passwordInput2) passwordInput2.contentType = TMP_InputField.ContentType.Password;

        usernameInput.onValueChanged.AddListener(_ => ValidateForm());
        emailInput.onValueChanged.AddListener(_ => ValidateForm());
        passwordInput.onValueChanged.AddListener(_ => ValidateForm());
        passwordInput2.onValueChanged.AddListener(_ => ValidateForm());
        ageCheck.onValueChanged.AddListener(_ => ValidateForm());
    }

    void OnEnable() => ValidateForm();

    void ValidateForm()
    {
        bool ok =
            !string.IsNullOrWhiteSpace(usernameInput.text) &&
            IsValidEmail(emailInput.text) &&
            passwordInput.text.Length >= 6 &&
            passwordInput.text == passwordInput2.text &&
            ageCheck != null && ageCheck.isOn;

        if (registerButton) registerButton.interactable = ok;

        if (!feedbackText) return;
        if (!IsValidEmail(emailInput.text)) feedbackText.text = "Email inválido";
        else if (passwordInput.text != passwordInput2.text) feedbackText.text = "Las contraseñas no coinciden";
        else if (passwordInput.text.Length < 6) feedbackText.text = "Mínimo 6 caracteres";
        else if (!ageCheck.isOn) feedbackText.text = "Debes confirmar la edad";
        else feedbackText.text = "";
    }

    bool IsValidEmail(string email)
        => !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    // Enlaza este método al botón REGISTER
    public void OnClickRegister()
    {
        ValidateForm();
        if (!registerButton || !registerButton.interactable) return;

        // cinturón de seguridad
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            PlayFabSettings.staticSettings.TitleId = "1EF38A";

        var req = new RegisterPlayFabUserRequest
        {
            Username = usernameInput.text.Trim(),
            Email = emailInput.text.Trim(),
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = true
        };

        if (feedbackText) feedbackText.text = "Creando cuenta...";
        PlayFabClientAPI.RegisterPlayFabUser(req, OnRegisterOk, OnRegisterErr);
    }

    void OnRegisterOk(RegisterPlayFabUserResult r)
    {
        // (Opcional) fija el DisplayName al username
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest{
            DisplayName = usernameInput.text.Trim()
        }, _ => {}, _ => {});

        // (Opcional) dispara tu evento custom para la Rule que da 1000 en el registro
        PlayFabClientAPI.WritePlayerEvent(new WriteClientPlayerEventRequest{
            EventName = "player_registered"
        }, _ => {}, _ => {});

        if (feedbackText) feedbackText.text = "Cuenta creada. Iniciando sesión...";

        // AUTO-LOGIN inmediatamente con lo que el usuario acaba de escribir
        var info = new GetPlayerCombinedInfoRequestParams { GetUserInventory = true, GetUserAccountInfo = true };
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest{
            Username = usernameInput.text.Trim(),
            Password = passwordInput.text,
            InfoRequestParameters = info
        }, OnLoginAfterRegisterOk, OnRegisterErr);
    }

    void OnLoginAfterRegisterOk(LoginResult r)
    {
        // lee saldo del login; si no viene, GetUserInventory
        int wallet = 0;
        bool ok = r.InfoResultPayload?.UserVirtualCurrency != null &&
                  r.InfoResultPayload.UserVirtualCurrency.TryGetValue(CURRENCY, out wallet);

        if (ok)
        {
            FinishAndGo(r, wallet);
        }
        else
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), inv =>
            {
                int w = 0; inv.VirtualCurrency?.TryGetValue(CURRENCY, out w);
                FinishAndGo(r, w);
            }, err =>
            {
                Debug.LogError(err.GenerateErrorReport());
                FinishAndGo(r, 0);
            });
        }
    }

    void FinishAndGo(LoginResult r, int wallet)
    {
        // RELLENA Session ANTES de cargar escena
        Session.Ticket    = r.SessionTicket;
        Session.PlayFabId = r.PlayFabId;
        Session.Username  = usernameInput.text.Trim(); // o r.InfoResultPayload?.AccountInfo?.Username
        Session.Email     = emailInput.text.Trim();
        Session.Wallet    = wallet;

        // Asegura HUD y refresca
        if (GlobalUI.Instance == null && globalUIPrefab != null) Instantiate(globalUIPrefab);
        GlobalUI.Instance?.Refresh();

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameSelection");
    }

    void OnRegisterErr(PlayFabError e)
    {
        string nice;

        // name del enum en tu SDK
        if (e.Error == PlayFabErrorCode.EmailAddressNotAvailable)
            nice = "El email ya está registrado.";
        else if (e.Error == PlayFabErrorCode.UsernameNotAvailable)
            nice = "El usuario ya existe.";
        else if (e.Error == PlayFabErrorCode.InvalidEmailAddress)
            nice = "Email inválido.";
        else if (e.Error == PlayFabErrorCode.InvalidPassword)
            nice = "Contraseña inválida (mín. 6).";
        else if ((e.ErrorMessage ?? "").ToLower().Contains("display name")
            ||  e.Error.ToString().ToLower().Contains("displayname"))
            nice = "El nombre público (Display Name) no está disponible.";
        else
            nice = e.ErrorMessage ?? "Error al registrar.";

        if (feedbackText) feedbackText.text = "Error: " + nice;
        Debug.LogError($"[Register] {e.Error}: {e.GenerateErrorReport()}");
    }
}