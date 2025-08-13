using UnityEngine;

public class CreditsUI : MonoBehaviour
{
    public GameObject creditsPanel; // arrastra aquí tu panel

    public void ShowCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(false);
    }

    public void ToggleCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(!creditsPanel.activeSelf);
    }
}