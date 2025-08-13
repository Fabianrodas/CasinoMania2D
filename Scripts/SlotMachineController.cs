using UnityEngine;
using TMPro;
using System.Collections;

public class SlotMachineController : MonoBehaviour
{
    [Header("Reels & UI")]
    public SlotReel[] reels;
    public TextMeshProUGUI resultadoText;
    public TextMeshProUGUI textoInferior;
    public GameObject paneles;       // panel oscuro + cartel de resultado
    public GameObject pobres;        // imagen “perdiste”
    public GameObject ricos;         // imagen “ganaste”
    public GameObject slotmachine;   // contenedor de la máquina
    public GameObject tablaapuesta;  // panel de apuesta (con SlotsBetPanel)

    [Header("Betting")]
    public SlotsBetPanel betPanel;   // script SlotsBetPanel en tu tabla
    public int minBet = 10;
    public int maxBet = 5000;
    public int multiplier = 10;      // paga 10x al ganar

    [Header("Timing")]
    public float revealDelay = 1.5f;     // espera para ver la combinación final
    public float resultShowSeconds = 3f; // cuánto tiempo mostrar el overlay

    // estado interno
    bool spinning = false;
    int stoppedCount = 0;
    Sprite[] results;
    bool isRich = false;
    Coroutine revealRoutine;
    Coroutine resultRoutine;
    int currentBet = 0;

    void OnEnable()
    {
        if (betPanel) betPanel.BetConfirmed += OnBetConfirmed;
    }
    void OnDisable()
    {
        if (betPanel) betPanel.BetConfirmed -= OnBetConfirmed;
    }

    void Awake()
    {
        results = new Sprite[reels.Length];

        for (int i = 0; i < reels.Length; i++)
        {
            int idx = i;
            reels[i].OnStopped += (r, sprite) =>
            {
                results[idx] = sprite;
                stoppedCount++;

                if (stoppedCount == reels.Length)
                {
                    spinning = false;

                    // mensaje intermedio + reveal con delay
                    ActualizarTextoInferior("Mostrando resultado...");
                    if (revealRoutine != null) StopCoroutine(revealRoutine);
                    revealRoutine = StartCoroutine(RevealAfterDelay());
                }
            };
        }
    }

    void Start()
    {
        SetResultadoUI(false);
        ActualizarTextoInferior("Ingresa tu apuesta");
        OpenBetPanel();
    }

    void Update()
    {
        if (spinning || currentBet <= 0) return;

        if (Input.GetKeyDown(KeyCode.Space))
            SpinAll();
    }

    // -----------------------------
    // Apuestas
    // -----------------------------
    void OpenBetPanel()
    {
        if (!betPanel) return;

        int wallet = GlobalUI.Instance ? GlobalUI.Instance.CurrentWallet : 0;
        betPanel.minBet = minBet;
        betPanel.maxBet = maxBet;
        betPanel.multiplier = multiplier;
        betPanel.Open(wallet);

        if (tablaapuesta) tablaapuesta.SetActive(true);
        if (slotmachine) slotmachine.SetActive(true);
        ActualizarTextoInferior(wallet > 0 ? "Ingresa tu apuesta" : "Sin saldo suficiente");
    }

    void OnBetConfirmed(int bet)
    {
        int wallet = GlobalUI.Instance ? GlobalUI.Instance.CurrentWallet : 0;
        bet = Mathf.Clamp(bet, minBet, Mathf.Min(maxBet, wallet));
        if (bet <= 0) return;

        GlobalUI.Instance.TrySpend(bet, success =>
        {
            if (!success)
            {
                if (betPanel) betPanel.Open(GlobalUI.Instance.CurrentWallet);
                return;
            }

            currentBet = bet;

            if (betPanel) betPanel.gameObject.SetActive(false);
            if (tablaapuesta) tablaapuesta.SetActive(false);

            SpinAll();
        });
    }

    // -----------------------------
    // Juego
    // -----------------------------
    public void SpinAll()
    {
        if (spinning) return;
        if (currentBet <= 0) { ActualizarTextoInferior("Primero apuesta"); return; }

        if (revealRoutine != null) { StopCoroutine(revealRoutine); revealRoutine = null; }
        if (resultRoutine != null) { StopCoroutine(resultRoutine); resultRoutine = null; }

        spinning = true;
        stoppedCount = 0;
        isRich = false;

        SetResultadoUI(false);
        ActualizarTextoInferior("Girando...");

        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].stopDelay = 2f + i * 0.5f;
            reels[i].Spin();
        }
    }

    IEnumerator RevealAfterDelay()
    {
        yield return new WaitForSeconds(revealDelay);

        EvaluarResultado(); // calcula win/lose y setea el mensaje

        // Mostrar overlay solo por X segundos y luego restaurar UI
        if (resultRoutine != null) StopCoroutine(resultRoutine);
        resultRoutine = StartCoroutine(ShowResultThenReset());

        revealRoutine = null;
    }

    void EvaluarResultado()
    {
        bool win = true;
        for (int i = 1; i < results.Length; i++)
        {
            if (results[i] == null || results[0] == null || results[i].name != results[0].name)
            {
                win = false; break;
            }
        }

        isRich = win;
        Mostrar(win ? "¡GANASTE!" : "Vuelve a intentarlo");

        // pago (apuesta x10 si gana)
        if (win && currentBet > 0)
        {
            int payout = currentBet * multiplier;
            GlobalUI.Instance.Grant(payout, _ => { /* feedback opcional */ });
        }

        // deja preparado para la siguiente
        currentBet = 0;
    }

    IEnumerator ShowResultThenReset()
    {
        // mostrar overlay
        SetResultadoUI(true);
        ActualizarTextoInferior(isRich ? "¡GANASTE!" : "Sigue intentando...");
        yield return new WaitForSeconds(resultShowSeconds);

        // ocultar overlay y volver a la vista normal
        SetResultadoUI(false);
        if (slotmachine) slotmachine.SetActive(true);
        OpenBetPanel(); // reabre la tabla de apuesta
        ActualizarTextoInferior("Ingresa tu apuesta");

        resultRoutine = null;
    }

    // -----------------------------
    // UI helpers
    // -----------------------------
    void Mostrar(string msg)
    {
        if (resultadoText != null) resultadoText.text = msg;
    }

    void SetResultadoUI(bool visible)
    {
        if (paneles) paneles.SetActive(visible);
        if (resultadoText) resultadoText.gameObject.SetActive(visible);

        if (pobres) pobres.SetActive(false);
        if (ricos)  ricos.SetActive(false);

        if (visible)
        {
            if (isRich) { if (ricos)  ricos.SetActive(true); }
            else        { if (pobres) pobres.SetActive(true); }

            // mientras está el overlay oculto la máquina/tabla (se restauran luego)
            if (slotmachine) slotmachine.SetActive(false);
            if (tablaapuesta) tablaapuesta.SetActive(false);
        }
    }

    void ActualizarTextoInferior(string mensaje)
    {
        if (textoInferior != null) textoInferior.text = mensaje;
    }
}