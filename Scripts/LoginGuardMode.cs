using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginGuardMode : MonoBehaviour
{
    void Awake()
    {
        if (Session.IsLoggedIn)
            SceneManager.LoadScene("GameSelection");
    }
}