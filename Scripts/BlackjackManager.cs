using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[DisallowMultipleComponent]
public class BlackjackManager : MonoBehaviour
{
    [Header("Cartas")]
    public List<Sprite> deckSprites;
    public Sprite backCardSprite;
    public GameObject cardPrefab;

    [Header("Posiciones de cartas")]
    public Transform playerCardsPos;
    public Transform dealerCardsPos;

    [Header("Botones")]
    public Button hitButton;
    public Button standButton;
    public Button doubleButton;
    public Button playButton;

    [Header("UI Resultado")]
    public GameObject resultPanel;
    public GameObject panel3;
    public TextMeshProUGUI resultText;

    [Header("UI Puntaje")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;

    [Header("Paneles")]
    public GameObject panel1;

    [Header("Apuesta")]
    public BetPanel betPanel;                    
    public UnityEngine.UI.Button confirmBetButton; 
    public UnityEngine.UI.Button undoButton;     
    public UnityEngine.UI.Button clearButton;     

    [Tooltip("Apuesta actual (ronda)")]
    public int currentBet = 0;

    [Tooltip("Apuesta mínima opcional")]
    public int minBet = 10; 
    [Tooltip("Apuesta máxima opcional (además del límite por wallet)")]
    public int maxBet = 500;  

    private List<GameObject> playerCards = new List<GameObject>();
    private List<GameObject> dealerCards = new List<GameObject>();
    private List<Sprite> deckInGame = new List<Sprite>();

    private bool isGameOver = false;
    private Sprite hiddenCardSprite;
    private bool isPlaying = false;
    private static BlackjackManager _instance;

    enum RoundOutcome { Win, Push, Lose }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[BJ] Duplicado de BlackjackManager destruido", this);
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // tu init normal (no te suscribas al evento aquí)
    }

    void OnEnable()
    {
        if (betPanel) betPanel.BetConfirmed += OnBetConfirmed;
    }

    void OnDisable()
    {
        if (betPanel) betPanel.BetConfirmed -= OnBetConfirmed;
    }

    void Start()
    {
        Debug.Log($"[BJ] Start() manager id={GetInstanceID()}");
        // Asegura estado “idle”
        isPlaying = false;

        // Oculta todo lo de juego
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);
        playerScoreText.gameObject.SetActive(false);
        dealerScoreText.gameObject.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);
        if (panel3) panel3.SetActive(false);

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(ShowBetPanel);

        hitButton.onClick.RemoveAllListeners();
        hitButton.onClick.AddListener(PlayerHit);

        standButton.onClick.RemoveAllListeners();
        standButton.onClick.AddListener(PlayerStand);

        doubleButton.onClick.RemoveAllListeners();
        doubleButton.onClick.AddListener(PlayerDouble);

        playButton.gameObject.SetActive(true);

        UpdateScoreUI();
    }

    void StartRoundFromBet()
    {
        Debug.Log("[BJ] StartRoundFromBet()");
        if (playerScoreText) playerScoreText.gameObject.SetActive(true);
        if (dealerScoreText) dealerScoreText.gameObject.SetActive(true);
        StartGame();
    }

    void StartGame()
    {
        Debug.Log("[BJ] StartGame()");
        StopAllCoroutines();
        isGameOver = false;

        if (resultPanel) resultPanel.SetActive(false);
        if (panel3) panel3.SetActive(false);
        if (resultText) resultText.gameObject.SetActive(false);
        if (playButton) playButton.gameObject.SetActive(false);
        if (playerScoreText) playerScoreText.gameObject.SetActive(true);
        if (dealerScoreText) dealerScoreText.gameObject.SetActive(true);

        foreach (var c in playerCards) if (c) Destroy(c);
        foreach (var c in dealerCards) if (c) Destroy(c);
        playerCards.Clear();
        dealerCards.Clear();

        CreateDeck();
        Debug.Log($"[BJ] Deck count={deckInGame?.Count}");
        StartCoroutine(DealInitialCards());
    }

    void CreateDeck()
    {
        deckInGame = new List<Sprite>(deckSprites);
    }

    // ---------------------
    //  Reparto inicial con animación local
    // ---------------------
    IEnumerator DealInitialCards()
    {
        Debug.Log("[BJ] DealInitialCards()");

        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);

        // 1a carta jugador
        yield return DealCardAnimated(playerCardsPos, playerCards, true);
        yield return new WaitForSeconds(0.2f);

        // 1a carta dealer
        yield return DealCardAnimated(dealerCardsPos, dealerCards, true);
        yield return new WaitForSeconds(0.2f);

        // 2a carta jugador
        yield return DealCardAnimated(playerCardsPos, playerCards, true);
        yield return new WaitForSeconds(0.2f);

        // 2a carta dealer (oculta)
        yield return DealCardAnimated(dealerCardsPos, dealerCards, false);

        // Activar botones
        hitButton.gameObject.SetActive(true);
        standButton.gameObject.SetActive(true);
        doubleButton.gameObject.SetActive(true);
        panel1.SetActive(true);
    }

    IEnumerator DealCardAnimated(Transform handBase, List<GameObject> hand, bool visible)
    {
        int rand = Random.Range(0, deckInGame.Count);
        Sprite chosenSprite = deckInGame[rand];

        // Crear carta como hija
        GameObject card = Instantiate(cardPrefab, handBase);
        card.transform.localPosition = new Vector3(5f, 3f, 0); // inicia fuera
        card.GetComponent<SpriteRenderer>().sprite = visible ? chosenSprite : backCardSprite;
        hand.Add(card);
        deckInGame.RemoveAt(rand);

        // Si es oculta del dealer, guardar sprite
        if (!visible && hand == dealerCards) hiddenCardSprite = chosenSprite;

        // Posición final local en la mano
        float spacing = 1.2f;
        int cardIndex = hand.Count - 1;
        float startX = -(hand.Count - 1) * spacing / 2f;
        Vector3 finalLocalPos = new Vector3(
            startX + cardIndex * spacing,
            hand == playerCards ? -0.05f * cardIndex : 0.05f * cardIndex,
            0
        );

        // Animación hacia su posición final
        float speed = 15f;
        while (card != null && Vector3.Distance(card.transform.localPosition, finalLocalPos) > 0.01f)
        {
            card.transform.localPosition = Vector3.MoveTowards(card.transform.localPosition, finalLocalPos, speed * Time.deltaTime);
            yield return null;
        }

        if (card != null) card.transform.localPosition = finalLocalPos;

        if (visible) UpdateScoreUI();
    }

    // ---------------------
    //  Calcular puntaje de mano
    // ---------------------
    int CalculateHandValue(List<GameObject> hand, bool hideDealer = false)
    {
        int total = 0;
        int aces = 0;

        for (int i = 0; i < hand.Count; i++)
        {
            Sprite cardSprite = hand[i].GetComponent<SpriteRenderer>().sprite;

            if (hideDealer && hand == dealerCards && i == 1) continue;

            int value = GetCardValue(cardSprite);
            if (value == 11) aces++;
            total += value;
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }
        return total;
    }

    int GetCardValue(Sprite cardSprite)
    {
        string name = cardSprite.name;
        if (name.StartsWith("A")) return 11;
        if (name.StartsWith("K") || name.StartsWith("Q") || name.StartsWith("J")) return 10;
        if (name.StartsWith("10")) return 10;
        int value;
        return int.TryParse(name.Substring(0, 1), out value) ? value : 0;
    }

    void UpdateScoreUI(bool hideDealer = true)
    {
        int playerScore = CalculateHandValue(playerCards);
        int dealerScore = CalculateHandValue(dealerCards, hideDealer);
        if (playerScoreText != null) playerScoreText.text = playerScore.ToString();
        if (dealerScoreText != null) dealerScoreText.text = dealerScore.ToString();
    }

    // ---------------------
    //  Turnos del jugador
    // ---------------------
    void PlayerHit()
    {
        if (isGameOver) return;
        StartCoroutine(DealCardAnimated(playerCardsPos, playerCards, true));
        if (CalculateHandValue(playerCards) > 21) EndRound(RoundOutcome.Lose, "¡Te pasaste! Dealer gana.");
    }

    void PlayerStand()
    {
        if (isGameOver) return;
        StartCoroutine(DealerTurn());
    }

    void PlayerDouble()
    {
        if (isGameOver) return;
        StartCoroutine(DealCardAnimated(playerCardsPos, playerCards, true));
        if (CalculateHandValue(playerCards) > 21)
        {
            EndRound(RoundOutcome.Lose, "¡Te pasaste al doblar! Dealer gana.");
            return;
        }
        StartCoroutine(DealerTurn());
    }

    // ---------------------
    //  Turno del dealer animado
    // ---------------------
    IEnumerator DealerTurn()
    {
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);

        // Revelar carta oculta
        dealerCards[1].GetComponent<SpriteRenderer>().sprite = hiddenCardSprite;
        UpdateScoreUI(false);
        yield return new WaitForSeconds(0.2f);

        // Dealer roba hasta 17
        while (CalculateHandValue(dealerCards) < 17)
        {
            yield return DealCardAnimated(dealerCardsPos, dealerCards, true);
            yield return new WaitForSeconds(0.2f);
        }

        int playerScore = CalculateHandValue(playerCards);
        int dealerScore = CalculateHandValue(dealerCards);

        if (dealerScore > 21 || dealerScore < playerScore)
            EndRound(RoundOutcome.Win, "¡Ganaste!");
        else if (dealerScore == playerScore)
            EndRound(RoundOutcome.Push, "Empate.");
        else
            EndRound(RoundOutcome.Lose, "Dealer gana.");
    }

    void EndRound(RoundOutcome outcome, string result)
    {
        isGameOver = true;
        resultText.text = result;
        if (resultPanel != null) resultPanel.SetActive(true);
        if (panel3 != null) panel3.SetActive(true);
        resultText.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);
        playButton.transform.SetAsLastSibling();
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        isPlaying = false;
        panel1.SetActive(false);
        UpdateScoreUI(false);

        // Pago según resultado (ya descontamos la apuesta al inicio)
        int payout = 0;
        switch (outcome)
        {
            case RoundOutcome.Win:  payout = currentBet * 2; break; // devuelve apuesta + ganancia
            case RoundOutcome.Push: payout = currentBet;     break; // devuelve apuesta
            case RoundOutcome.Lose: payout = 0;              break;
        }

        if (payout > 0)
            GlobalUI.Instance.Grant(payout, _ => { /* opcional: feedback */ });

        currentBet = 0; // limpia para la siguiente ronda
    }

    void ClearTableVisual()
    {
        foreach (var c in playerCards) if (c) Destroy(c);
        foreach (var c in dealerCards) if (c) Destroy(c);
        playerCards.Clear();
        dealerCards.Clear();
        hiddenCardSprite = null;
        UpdateScoreUI(true);
    }

    public void ShowBetPanel()
    {
        Debug.Log("ShowBetPanel()");
        StopAllCoroutines();
        isPlaying = false;

        ClearTableVisual();

        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);
        playerScoreText.gameObject.SetActive(false);
        dealerScoreText.gameObject.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);
        if (panel3) panel3.SetActive(false);

        playButton.gameObject.SetActive(false);

        if (betPanel)
        {
            int wallet = GlobalUI.Instance ? GlobalUI.Instance.CurrentWallet : 0; // <-- AQUI
            betPanel.gameObject.SetActive(true);
            betPanel.Open(wallet); // o Init(wallet) si prefieres
        }
    }

    public void OnBetConfirmed(int bet)
    {
        Debug.Log($"[BJ] OnBetConfirmed bet={bet}");

        int wallet = GlobalUI.Instance ? GlobalUI.Instance.CurrentWallet : 0;
        bet = Mathf.Clamp(bet, minBet, Mathf.Min(maxBet, wallet));
        if (bet <= 0) return;

        // intenta descontar
        GlobalUI.Instance.TrySpend(bet, success =>
        {
            if (!success)
            {
                Debug.LogWarning("[BJ] Saldo insuficiente al confirmar. Volvemos a abrir BetPanel.");
                if (betPanel) betPanel.Open(GlobalUI.Instance.CurrentWallet);
                return;
            }

            currentBet = bet;
            if (betPanel) betPanel.gameObject.SetActive(false);

            // Arranca la ronda
            isPlaying = false;
            StartCoroutine(StartRoundNextFrame());
        });
    }

    IEnumerator StartRoundNextFrame()
    {
        yield return null; // 1 frame
        Debug.Log("[BJ] StartRoundFromBet()");
        isPlaying = true;
        StartRoundFromBet();
    }

}