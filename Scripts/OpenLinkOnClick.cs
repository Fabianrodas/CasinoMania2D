using UnityEngine;

public class OpenLinkOnClick : MonoBehaviour
{
    [SerializeField] private string url = "https://discord.com/discovery/applications/1300612940486934591";

    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}
