using UnityEngine;
using PlayFab;

public class PlayFabInit : MonoBehaviour
{
    [SerializeField] string titleId = "1EF38A";

    void Awake()
    {
        PlayFabSettings.staticSettings.TitleId = "1EF38A"; 
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            PlayFabSettings.staticSettings.TitleId = titleId;
    }
}
