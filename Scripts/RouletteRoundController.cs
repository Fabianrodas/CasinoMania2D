using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace CasinoMania2D.Roulette
{
    public class RouletteRoundController : MonoBehaviour
    {
        [Header("Referencias en RotatorRoulette")]
        [SerializeField] private Transform numbersWheel;          // Art/wheel_numbers-02-02_0
        [SerializeField] private Transform ballPivot;            // RotatorBall (centro)
        [SerializeField] private Transform ballSprite;           // RotatorBall/sphereBall
        [SerializeField] private Transform controllerBall;       // ControllerBall (handler_0..36)
        [SerializeField] private WinnerBadge winnerBadge;        // NumberWinnerAnchor (con WinnerBadge)
        [SerializeField] private ResultMessageUI resultMessage;  // Canvas_UI/ResultText
        [SerializeField] private RouletteIntroAnimator introAnimator; // animación idle (opcional)
        [SerializeField] private Button playButton;

        [Header("Tiempos")]
        [SerializeField] private float accelTime = 0.7f;
        [SerializeField] private float cruiseTime = 1.2f;
        [SerializeField] private float decelTime = 2.8f;

        [Header("Velocidades máximas (deg/s)")]
        [SerializeField] private float ballMaxSpeed = 1080f;     // órbita rápida
        [SerializeField] private float wheelMaxSpeed = -540f;    // giro contrario

        [Header("Comportamiento")]
        [Tooltip("Reparenta ControllerBall bajo la rueda para que gire exactamente junto con los números.")]
        [SerializeField] private bool linkHandlersToWheel = true;
        [Tooltip("Vueltas extra SOLO en la fase de frenado (suaviza la caída).")]
        [SerializeField] private int extraRevolutionsOnDecel = 2;
        [Tooltip("Congela la rueda (y los handlers) durante el frenado para garantizar la coincidencia visual.")]
        [SerializeField] private bool freezeWheelOnDecel = true;
        [Tooltip("Calibración: si notas un sesgo angular constante, ajusta (+ hacia antihorario, - hacia horario).")]
        [SerializeField] private float handlerAngleOffsetDeg = 0f;
        [Tooltip("Segundos de espera antes de reanudar la animación idle.")]
        [SerializeField] private float introResumeDelay = 5f;

        [Header("Estado")]
        [SerializeField] private bool isSpinning = false;
        [SerializeField] private int lastWinningNumber = -1;

        private readonly Dictionary<int, Transform> handlers = new();
        private float localBallAngleDeg;
        private bool lockUntilResume = false;
        public event Action<bool> LockStateChanged;           // notifica cuando cambia el lock
        public bool IsInteractionLocked => isSpinning || lockUntilResume;
        private void NotifyLock() => LockStateChanged?.Invoke(IsInteractionLocked);

        public static RouletteRoundController Instance { get; private set; }

        private static readonly int[] reds = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        private static readonly int[] blacks = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };

        #region Subcripción/PLAY robusto

        private Coroutine waitBmCo;

        private void Awake()
        {
            // Autorreferencias de jerarquía
            if (!numbersWheel) numbersWheel = transform.Find("Art/wheel_numbers-02-02_0");
            if (!ballPivot)    ballPivot    = transform.Find("RotatorBall");
            if (!ballSprite && ballPivot) ballSprite = ballPivot.Find("sphereBall");
            if (!controllerBall) controllerBall = transform.Find("ControllerBall");

            // Autodescubrimiento de botón PLAY si quedó sin asignar
            if (!playButton)
            {
                var go = GameObject.Find("play"); // mismo nombre que tienes en Canvas
                if (go) playButton = go.GetComponent<Button>();
            }

            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            CacheHandlers();
            UpdatePlayButtonState(); // estado inicial (por si el BetManager aún no existe)
        }

        private void OnEnable()
        {
            TrySubscribeToBetManager();
            UpdatePlayButtonState();
        }

        private void Start()
        {
            // Asegura centros alineados
            if (!numbersWheel || !ballPivot || !ballSprite || !controllerBall)
            {
                Debug.LogError("[RouletteRoundController] Faltan referencias.");
                enabled = false; return;
            }

            if ((numbersWheel.position - ballPivot.position).sqrMagnitude > 1e-6f)
                ballPivot.position = numbersWheel.position;

            localBallAngleDeg = Mathf.Atan2(ballSprite.localPosition.y, ballSprite.localPosition.x) * Mathf.Rad2Deg;

            // Vincula handlers a la rueda para que roten 1:1
            if (linkHandlersToWheel && controllerBall.parent != numbersWheel)
            {
                controllerBall.SetParent(numbersWheel, true); // conserva world pos
                controllerBall.rotation = numbersWheel.rotation;
            }

            if (winnerBadge) winnerBadge.Hide();

            UpdatePlayButtonState(); // por si el BetManager notificó en Start
        }

        private void OnDisable()
        {
            if (BetManager.Instance != null) BetManager.Instance.BetsChanged -= UpdatePlayButtonState;
            if (waitBmCo != null) { StopCoroutine(waitBmCo); waitBmCo = null; }
        }

        private void TrySubscribeToBetManager()
        {
            if (BetManager.Instance != null)
            {
                BetManager.Instance.BetsChanged -= UpdatePlayButtonState; // evita duplicados
                BetManager.Instance.BetsChanged += UpdatePlayButtonState;
            }
            else
            {
                // Espera a que aparezca el BetManager y suscríbete
                if (waitBmCo == null) waitBmCo = StartCoroutine(WaitAndSubscribeBM());
            }
        }

        private IEnumerator WaitAndSubscribeBM()
        {
            // Reintenta unas cuantas frames hasta que exista
            for (int i = 0; i < 120; i++) // ~2 segundos a 60fps
            {
                if (BetManager.Instance != null)
                {
                    BetManager.Instance.BetsChanged -= UpdatePlayButtonState;
                    BetManager.Instance.BetsChanged += UpdatePlayButtonState;
                    UpdatePlayButtonState();
                    waitBmCo = null;
                    yield break;
                }
                yield return null;
            }
            waitBmCo = null;
        }

        private void UpdatePlayButtonState()
        {
            bool hasBets = BetManager.Instance != null && BetManager.Instance.HasAnyBets();
            bool interactable = hasBets && !isSpinning && !lockUntilResume;
            if (playButton) playButton.interactable = interactable;
            // Debug opcional:
            // Debug.Log($"[RC] PLAY={interactable} | hasBets={hasBets} spin={isSpinning} lock={lockUntilResume}");
        }

        #endregion

        void CacheHandlers()
        {
            handlers.Clear();
            if (!controllerBall) return;
            for (int i = 0; i <= 36; i++)
            {
                var h = controllerBall.Find($"handler_{i}");
                if (h) handlers[i] = h;
            }
        }

        // Conecta este método al botón PLAY
        public void OnPlayPressed()
        {
            if (isSpinning || lockUntilResume) return;
            if (BetManager.Instance == null || !BetManager.Instance.HasAnyBets()) return;

            int target = UnityEngine.Random.Range(0, 37);
            StartRound(target);
        }

        public void StartRound(int winningNumber)
        {
            if (isSpinning) return;
            if (!handlers.ContainsKey(winningNumber))
            {
                Debug.LogError($"No está handler_{winningNumber} en ControllerBall.");
                return;
            }

            isSpinning = true;
            lockUntilResume = true;
            UpdatePlayButtonState();
            NotifyLock();  

            if (introAnimator) introAnimator.enabled = false;
            if (winnerBadge) winnerBadge.Hide();

            StartCoroutine(RoundRoutine(winningNumber));
        }

        IEnumerator RoundRoutine(int targetNumber)
        {
            // 1) Acelerar
            float t = 0f;
            while (t < accelTime)
            {
                float dt = Time.deltaTime; t += dt;
                float u = Mathf.Clamp01(t / accelTime);
                float ballSpeed  = Mathf.Lerp(0f, ballMaxSpeed, EaseOutCubic(u));
                float wheelSpeed = Mathf.Lerp(0f, wheelMaxSpeed, EaseOutCubic(u));

                ballPivot.Rotate(0, 0, ballSpeed * dt, Space.Self);
                numbersWheel.Rotate(0, 0, wheelSpeed * dt, Space.Self);
                yield return null;
            }

            // 2) Crucero
            t = 0f;
            while (t < cruiseTime)
            {
                float dt = Time.deltaTime; t += dt;
                ballPivot.Rotate(0, 0, ballMaxSpeed * dt, Space.Self);
                numbersWheel.Rotate(0, 0, wheelMaxSpeed * dt, Space.Self);
                yield return null;
            }

            // 3) Frenado: congela rueda y lleva la bola EXACTA al handler
            float wheelZFixed = numbersWheel.eulerAngles.z;
            if (freezeWheelOnDecel)
                numbersWheel.rotation = Quaternion.Euler(0, 0, wheelZFixed);

            Vector3 c = ballPivot.position;
            float currentAngle = Mathf.Atan2(ballSprite.position.y - c.y, ballSprite.position.x - c.x) * Mathf.Rad2Deg;

            Transform h = handlers[targetNumber];
            float thetaT = Mathf.Atan2(h.position.y - c.y, h.position.x - c.x) * Mathf.Rad2Deg;
            thetaT += handlerAngleOffsetDeg;

            float remainedDelta = DeltaAnglePositive(currentAngle, thetaT) + 360f * Mathf.Max(0, extraRevolutionsOnDecel);

            t = 0f;
            while (t < decelTime)
            {
                float dt = Time.deltaTime; t += dt;

                if (freezeWheelOnDecel)
                    numbersWheel.rotation = Quaternion.Euler(0, 0, wheelZFixed);
                else
                {
                    float u = Mathf.Clamp01(t / decelTime);
                    numbersWheel.Rotate(0, 0, Mathf.Lerp(wheelMaxSpeed, 0f, EaseInCubic(u)) * dt, Space.Self);
                }

                float uDec = Mathf.Clamp01(t / decelTime);
                float instSpeed = 2f * remainedDelta / decelTime * (1f - uDec);
                ballPivot.Rotate(0, 0, instSpeed * dt, Space.Self);

                yield return null;
            }

            float pivotFinal = thetaT - localBallAngleDeg;
            ballPivot.rotation = Quaternion.Euler(0, 0, pivotFinal);

            // Badge al centro
            if (winnerBadge && numbersWheel)
                winnerBadge.transform.position = numbersWheel.position;

            bool isGreen = (targetNumber == 0);
            bool isRed = !isGreen && System.Array.IndexOf(reds, targetNumber) >= 0;
            if (winnerBadge) winnerBadge.Show(targetNumber, isRed, isGreen);

            var res = ResolveBets(targetNumber);
            if (resultMessage) resultMessage.ShowResult(targetNumber, res.totalStake, res.totalProfit, res.net, isRed, isGreen);

            isSpinning = false;

            if (introAnimator) StartCoroutine(ResumeIntroAfterDelay());
        }

        private IEnumerator ResumeIntroAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, introResumeDelay));
            if (introAnimator) introAnimator.enabled = true;

            lockUntilResume = false;
            UpdatePlayButtonState();
            NotifyLock();  
        }

        // ------ Pagos y resultado ------
        struct RoundResult { public int totalStake, totalProfit, net; }
        RoundResult ResolveBets(int winningNumber)
        {
            var bm = BetManager.Instance;
            RoundResult rr = new RoundResult();
            if (bm == null) return rr;

            int stakeWinners = 0;
            int profit = 0;
            int totalStake = 0;

            foreach (var kv in bm.GetAllBets())
            {
                BetSpot spot = kv.Key; int amount = kv.Value;
                totalStake += amount;

                bool covers = false;
                foreach (var n in bm.GetCoveredNumbers(spot))
                    if (n == winningNumber) { covers = true; break; }

                if (covers)
                {
                    int mult = PayoutFor(spot.group);
                    profit += amount * mult;
                    stakeWinners += amount;
                }
            }

            int lostStake = totalStake - stakeWinners;
            int net = profit - lostStake;

            Debug.Log($"► Resultado: N° {winningNumber}  | Apostado: {totalStake} | Ganancia: {profit} | Neto: {net}");

            rr.totalStake = totalStake;
            rr.totalProfit = profit;
            rr.net = net;
            return rr;
        }

        int PayoutFor(BetGroup g)
        {
            switch (g)
            {
                case BetGroup.Straight: return 35;
                case BetGroup.Dozen1:
                case BetGroup.Dozen2:
                case BetGroup.Dozen3:
                case BetGroup.Column1:
                case BetGroup.Column2:
                case BetGroup.Column3: return 2;
                case BetGroup.Even:
                case BetGroup.Odd:
                case BetGroup.Red:
                case BetGroup.Black:
                case BetGroup.Low:
                case BetGroup.High: return 1;
                default: return 0;
            }
        }

        // ---------- Utils ----------
        static float DeltaAnglePositive(float fromDeg, float toDeg)
        {
            float d = Mathf.DeltaAngle(fromDeg, toDeg);
            if (d < 0f) d += 360f;
            return d;
        }
        static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
        static float EaseInCubic(float x) => x * x * x;
    }
}