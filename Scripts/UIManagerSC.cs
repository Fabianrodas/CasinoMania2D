using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManagerSC : MonoBehaviour
{
    public void CargarEscena()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
