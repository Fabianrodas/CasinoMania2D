using UnityEngine;

public class UIButtonPulseCredits : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    public bool startEnabled = false; 
    Vector3 baseScale;
    bool isActive;

    void Awake()
    {
        baseScale = transform.localScale; 
        isActive = startEnabled;
    }

    void OnEnable()
    {

        transform.localScale = baseScale;
    }

    void Update()
    {
        if (!isActive)
            return;

        float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }

    public void SetPulse(bool enabled)
    {
        isActive = enabled;
        if (!enabled)
            transform.localScale = baseScale; 
    }

    public void SetBaseScale(Vector3 s)
    {
        baseScale = s;
        transform.localScale = s;
    }
}
