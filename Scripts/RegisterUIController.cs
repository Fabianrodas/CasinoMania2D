using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

// PlayFab
using PlayFab;
using PlayFab.ClientModels;

public class RegisterUIController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField usernameInput;   // arrastra: usernameInput
    public TMP_InputField emailInput;      // arrastra: emailInput
    public TMP_InputField passwordInput;   // arrastra: passwordInput
    public TMP_InputField passwordInput2;  // arrastra: passwordInput2
    public Toggle ageCheck;                // arrastra: ageCheck (Toggle)

    [Header("UI")]
    public Button registerButton;          // arrastra: registerButton (Button)
    public TextMeshProUGUI feedbackText;   // opcional, para mostrar mensajes

    void Awake()
    {
        // Asegura tipos de entrada
        if (passwordInput)  passwordInput.contentType  = TMP_InputField.ContentType.Password;
        if (passwordInput2) passwordInput2.contentType = TMP_InputField.ContentType.Password;

        // Suscribe validación
        usernameInput.onValueChanged.AddListener(_ => ValidateForm());
        emailInput.onValueChanged.AddListener(_ => ValidateForm());
        passwordInput.onValueChanged.AddListener(_ => ValidateForm());
        passwordInput2.onValueChanged.AddListener(_ => ValidateForm());
        ageCheck.onValueChanged.AddListener(_ => ValidateForm());
    }

    void OnDestroy()
    {
        // Limpia listeners (evita leaks si cambias de escena)
        usernameInput.onValueChanged.RemoveAllListeners();
        emailInput.onValueChanged.RemoveAllListeners();
        passwordInput.onValueChanged.RemoveAllListeners();
        passwordInput2.onValueChanged.RemoveAllListeners();
        ageCheck.onValueChanged.RemoveAllListeners();
    }

    void OnEnable() => ValidateForm();

    void ValidateForm()
    {
        bool ok =
            usernameInput && !string.IsNullOrWhiteSpace(usernameInput.text) &&
            emailInput && IsValidEmail(emailInput.text) &&
            passwordInput && passwordInput.text.Length >= 6 &&
            passwordInput2 && passwordInput.text == passwordInput2.text &&
            ageCheck && ageCheck.isOn;

        if (registerButton) registerButton.interactable = ok;

        if (!feedbackText) return;

        if (!IsValidEmail(emailInput.text)) feedbackText.text = "Email inválido";
        else if (passwordInput.text != passwordInput2.text) feedbackText.text = "Las contraseñas no coinciden";
        else if (passwordInput.text.Length < 6) feedbackText.text = "Mínimo 6 caracteres";
        else if (!ageCheck.isOn) feedbackText.text = "Debes confirmar la edad";
        else feedbackText.text = "";
    }

    bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // Engancha este método al botón REGISTER (OnClick)
    public void OnClickRegister()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            PlayFabSettings.staticSettings.TitleId = "12471A";

        ValidateForm();
        if (!registerButton || !registerButton.interactable) return;

        var username = usernameInput.text.Trim();
        var email    = emailInput.text.Trim();
        var pass     = passwordInput.text;

        if (feedbackText) feedbackText.text = "Creando usuario...";

        var req = new RegisterPlayFabUserRequest
        {
            Email = email,
            Username = username,           // puedes loguear con username + password
            Password = pass,               // mínimo 6 chars
            DisplayName = username,        // opcional: mismo que username
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(req, OnRegisterOk, OnRegisterErr);
    }

    void OnRegisterOk(RegisterPlayFabUserResult r)
    {
        if (feedbackText) feedbackText.text = "Usuario creado. ¡Ahora inicia sesión!";
        Debug.Log("[PlayFab] Register OK. PlayFabId: " + r.PlayFabId);
    }

    void OnRegisterErr(PlayFabError e)
    {
        var msg = e.GenerateErrorReport();
        if (feedbackText) feedbackText.text = "Error: " + msg;
        Debug.LogError("[PlayFab] Register Error: " + msg);
    }
}
