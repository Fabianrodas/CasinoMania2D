using UnityEngine;
using UnityEngine.UI;

public class UIButtonPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;     // velocidad de pulso
    public float pulseAmount = 0.1f;  // cuánto crece
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * scale;
    }
}
