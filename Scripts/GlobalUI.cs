using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GlobalUI : MonoBehaviour
{
    public static GlobalUI Instance { get; private set; }

    [Header("Refs")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI walletText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => Refresh();

    public int guestStartBalance = 1000; 

    public void Refresh()
    {
        if (!Session.IsLoggedIn)
        {
            if (usernameText) usernameText.text = "Guest";
            if (walletText)   walletText.text   = "1000"; 
            return;
        }

        if (usernameText) usernameText.text = Session.Username;
        if (walletText)   walletText.text   = Session.Wallet.ToString();
    }

    public void OnClickLogout()
    {
        PlayFab.PlayFabClientAPI.ForgetAllCredentials();
        Session.Clear();
        Refresh();
        SceneManager.LoadScene("LoginMenu");
    }

    void OnEnable(){ SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable(){ SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene s, LoadSceneMode m){ Refresh(); }
}