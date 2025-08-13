using UnityEngine;

public class PocketHandler : MonoBehaviour
{
    public int number;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            FindObjectOfType<RouletteManager>().OnBallLanded(number);
        }
    }
}
