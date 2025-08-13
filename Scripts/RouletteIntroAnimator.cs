using UnityEngine;

public class RouletteIntroAnimator : MonoBehaviour
{
    [Header("Asignaciones (arrástralas desde la jerarquía)")]
    [Tooltip("Sprite de la rueda con números: RotatorRoulette/Art/wheel_numbers-02-02_0")]
    [SerializeField] private Transform numbersWheel;

    [Tooltip("Pivote vacío que se ubica en el centro: RotatorRoulette/RotatorBall")]
    [SerializeField] private Transform ballPivot;

    [Tooltip("Sprite de la bola: RotatorRoulette/RotatorBall/sphereBall")]
    [SerializeField] private Transform ballSprite;

    [Header("Velocidades (grados por segundo)")]
    [Tooltip("Velocidad del giro de la rueda de números (signo = sentido).")]
    [SerializeField] private float numbersWheelSpeed = -120f;

    [Tooltip("Velocidad de la órbita de la bola (signo = sentido).")]
    [SerializeField] private float ballOrbitSpeed = 420f;

    [Header("Órbita de la bola")]
    [Tooltip("Radio de la órbita en unidades de mundo. Si es 0, usa la posición local actual del sprite como radio.")]
    [SerializeField] private float orbitRadius = 0f;

    [Tooltip("Si está activo, el script recoloca automáticamente el ballPivot al centro del numbersWheel al iniciar.")]
    [SerializeField] private bool autoCenterPivot = true;

    [Header("Arranque suave (opcional)")]
    [SerializeField] private bool smoothStart = true;
    [SerializeField, Range(0.05f, 2f)] private float accelTime = 0.6f;

    private float t; // tiempo transcurrido para el arranque suave
    private float initialBallAngleDeg;
    private Vector3 ballLocalStart;

    private void Reset()
    {
        // Intento de autollenado cuando se añade el componente
        if (numbersWheel == null)
        {
            var art = transform.Find("RotatorRoulette/Art/wheel_numbers-02-02_0");
            if (art) numbersWheel = art;
        }
        if (ballPivot == null)
        {
            var pivot = transform.Find("RotatorRoulette/RotatorBall");
            if (pivot) ballPivot = pivot;
        }
        if (ballSprite == null && ballPivot != null)
        {
            var ball = ballPivot.Find("sphereBall");
            if (ball) ballSprite = ball;
        }
    }

    private void OnValidate()
    {
        // Evita valores raros si alguien edita mientras corre
        accelTime = Mathf.Max(0.05f, accelTime);
    }

    private void Start()
    {
        if (numbersWheel == null || ballPivot == null || ballSprite == null)
        {
            Debug.LogError("[RouletteIntroAnimator] Faltan referencias. Asigna numbersWheel, ballPivot y ballSprite.");
            enabled = false;
            return;
        }

        if (autoCenterPivot)
        {
            // Asegura que el pivote esté centrado con la rueda de números
            ballPivot.position = numbersWheel.position;
        }

        // Si orbitRadius es 0, tomamos el radio desde la posición local actual del sprite
        if (Mathf.Approximately(orbitRadius, 0f))
        {
            ballLocalStart = ballSprite.localPosition;
            orbitRadius = new Vector2(ballLocalStart.x, ballLocalStart.y).magnitude;
        }
        else
        {
            // Colocamos la bola a 'orbitRadius' a la derecha del pivote (ángulo 0)
            ballSprite.localPosition = new Vector3(orbitRadius, 0f, ballSprite.localPosition.z);
            ballLocalStart = ballSprite.localPosition;
        }

        // Calculamos el ángulo inicial a partir de la posición local
        initialBallAngleDeg = Mathf.Atan2(ballLocalStart.y, ballLocalStart.x) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        // Factor de arranque suave (0→1)
        float k = 1f;
        if (smoothStart && t < accelTime)
        {
            t += Time.deltaTime;
            // Ease-out (cubic)
            float u = Mathf.Clamp01(t / accelTime);
            k = 1f - Mathf.Pow(1f - u, 3f);
        }

        // 1) Giro de la rueda de números (Z porque es 2D)
        numbersWheel.Rotate(0f, 0f, numbersWheelSpeed * k * Time.deltaTime, Space.Self);

        // 2) Órbita de la bola:
        // Rotamos el pivote; la bola mantiene su offset local y por lo tanto orbita
        ballPivot.Rotate(0f, 0f, ballOrbitSpeed * k * Time.deltaTime, Space.Self);

        // (Opcional) Si quieres que la bola también ruede sobre sí misma, descomenta:
        // ballSprite.Rotate(0f, 0f, -ballOrbitSpeed * k * Time.deltaTime, Space.Self);
    }

    // Métodos públicos pensados para cuando pongas el botón "Play" más adelante
    public void SetSpeeds(float wheelDegPerSec, float ballDegPerSec)
    {
        numbersWheelSpeed = wheelDegPerSec;
        ballOrbitSpeed = ballDegPerSec;
    }

    public void StopIntro()
    {
        numbersWheelSpeed = 0f;
        ballOrbitSpeed = 0f;
    }
}
