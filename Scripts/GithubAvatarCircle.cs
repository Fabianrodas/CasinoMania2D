using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class GithubAvatarCircle : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Usuario de GitHub")]
    public string githubUsername;

    [Tooltip("Si lo dejas vacío, se arma con el username")]
    public string githubUrl;

    [Tooltip("Tamaño del avatar a pedir (px)")]
    public int avatarSize = 256;

    [Header("Refs")]
    public RawImage avatarRawImage; 
    public Button button;            

    void Reset()
    {
        button = GetComponent<Button>();
        avatarRawImage = GetComponentInChildren<RawImage>(true);
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(githubUrl) && !string.IsNullOrEmpty(githubUsername))
            githubUrl = $"https://github.com/{githubUsername}";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenGithub);
        }
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(githubUsername))
            StartCoroutine(LoadGithubAvatar(githubUsername, avatarSize));
    }

    IEnumerator LoadGithubAvatar(string user, int size)
    {
        string url = $"https://avatars.githubusercontent.com/{user}?size={size}";
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            #if UNITY_2020_2_OR_NEWER
                        if (req.result != UnityWebRequest.Result.Success)
            #else
                        if (req.isNetworkError || req.isHttpError)
            #endif
            {
                Debug.LogWarning($"No se pudo cargar avatar de {user}: {req.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (avatarRawImage != null)
            {
                avatarRawImage.texture = tex;
                avatarRawImage.SetNativeSize(); 
                avatarRawImage.rectTransform.anchorMin = Vector2.zero;
                avatarRawImage.rectTransform.anchorMax = Vector2.one;
                avatarRawImage.rectTransform.offsetMin = Vector2.zero;
                avatarRawImage.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    void OpenGithub()
    {
        if (!string.IsNullOrEmpty(githubUrl))
            Application.OpenURL(githubUrl);
    }
}
