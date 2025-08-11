using UnityEngine;

public class LoginGuard : MonoBehaviour
{
    public static LoginGuard I;
    const float Cooldown = 1.5f;         // 1.5 s entre peticiones del mismo tipo
    float nextAllowedLogin = 0f;

    void Awake() {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool CanLoginNow(string reason = "")
    {
        float now = Time.realtimeSinceStartup;
        if (now < nextAllowedLogin) {
            Debug.LogWarning($"[LoginGuard] Bloqueado ({reason}). Espera {nextAllowedLogin - now:0.00}s");
            return false;
        }
        nextAllowedLogin = now + Cooldown;
        return true;
    }

    public void ResetLoginGate() {
        nextAllowedLogin = 0f;
    }
}