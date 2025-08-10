using UnityEngine;
using PlayFab;

public class PlayFabInit : MonoBehaviour
{
    [SerializeField] string titleId = "12471A";

    void Awake()
    {
        PlayFabSettings.staticSettings.TitleId = "12471A"; 
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            PlayFabSettings.staticSettings.TitleId = titleId;
    }
}
