using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    public TMP_InputField passwordField;
    public GameObject eyeOpenIcon;
    public GameObject eyeClosedIcon;

    private bool isPasswordVisible = false;

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        passwordField.contentType = isPasswordVisible ?
            TMP_InputField.ContentType.Standard :
            TMP_InputField.ContentType.Password;

        passwordField.ForceLabelUpdate();

        eyeOpenIcon.SetActive(isPasswordVisible);
        eyeClosedIcon.SetActive(!isPasswordVisible);
    }
}
