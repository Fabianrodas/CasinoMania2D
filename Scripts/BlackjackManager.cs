using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BlackjackManager : MonoBehaviour
{
    [Header("Cartas")]
    public List<Sprite> deckSprites;      // Sprites de cartas
    public Sprite backCardSprite;         // Dorso para carta oculta
    public GameObject cardPrefab;         // Prefab de carta

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
    public TextMeshProUGUI resultText;

    [Header("UI Puntaje")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;

    private List<GameObject> playerCards = new List<GameObject>();
    private List<GameObject> dealerCards = new List<GameObject>();

    private List<Sprite> deckInGame = new List<Sprite>();
    private bool isGameOver = false;

    private Sprite hiddenCardSprite; // para revelar luego

    [Header("Paneles")]
    public GameObject panel1;
    public GameObject panel2;

    void Start()
    {
        // Ocultar botones y panel al inicio
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);
        playerScoreText.gameObject.SetActive(false);
        dealerScoreText.gameObject.SetActive(false);

        
        if (resultPanel != null) resultPanel.SetActive(false);

        playButton.onClick.AddListener(StartGame);
        hitButton.onClick.AddListener(PlayerHit);
        standButton.onClick.AddListener(PlayerStand);
        doubleButton.onClick.AddListener(PlayerDouble);

        UpdateScoreUI();
    }

    void StartGame()
    {
        isGameOver = false;
        if (resultPanel != null) resultPanel.SetActive(false);

        playButton.gameObject.SetActive(false);

        hitButton.gameObject.SetActive(true);
        standButton.gameObject.SetActive(true);
        doubleButton.gameObject.SetActive(true);
        panel1.SetActive(true);
        resultText.gameObject.SetActive(false);
        playerScoreText.gameObject.SetActive(true);
        dealerScoreText.gameObject.SetActive(true);

        // Limpiar mesa
        foreach (GameObject c in playerCards) Destroy(c);
        foreach (GameObject c in dealerCards) Destroy(c);
        playerCards.Clear();
        dealerCards.Clear();

        CreateDeck();

        // 2 cartas jugador
        GiveCard(playerCardsPos, playerCards, true);
        GiveCard(playerCardsPos, playerCards, true);

        // 1 carta visible dealer
        GiveCard(dealerCardsPos, dealerCards, true);

        // 1 carta oculta dealer
        GiveHiddenDealerCard();

        UpdateScoreUI();
    }

    void CreateDeck()
    {
        deckInGame = new List<Sprite>(deckSprites);
    }

    void ArrangeCards(List<GameObject> hand, bool isPlayer)
    {
        float spacing = 1.2f; // separación horizontal
        float startX = -(hand.Count - 1) * spacing / 2f; // centrar cartas

        for (int i = 0; i < hand.Count; i++)
        {
            hand[i].transform.localPosition = new Vector3(startX + i * spacing, isPlayer ? -i * 0.05f : i * 0.05f, 0);
        }
    }

    // ---------------------
    //  Dar carta visible
    // ---------------------
    void GiveCard(Transform targetPos, List<GameObject> hand, bool visible)
    {
        int rand = Random.Range(0, deckInGame.Count);
        Sprite chosenSprite = deckInGame[rand];

        GameObject card = Instantiate(cardPrefab, targetPos.position, Quaternion.identity, targetPos);
        card.GetComponent<SpriteRenderer>().sprite = visible ? chosenSprite : backCardSprite;
        hand.Add(card);

        // Alinear cartas
        ArrangeCards(hand, hand == playerCards);

        // Si es visible, actualizar puntajes
        UpdateScoreUI();

        // Remover del mazo
        deckInGame.RemoveAt(rand);
    }

    // ---------------------
    //  Dar carta oculta dealer
    // ---------------------
    void GiveHiddenDealerCard()
    {
        int rand = Random.Range(0, deckInGame.Count);
        hiddenCardSprite = deckInGame[rand];

        GameObject card = Instantiate(cardPrefab, dealerCardsPos.position, Quaternion.identity, dealerCardsPos);
        card.GetComponent<SpriteRenderer>().sprite = backCardSprite;
        dealerCards.Add(card);

        // Remover del mazo
        deckInGame.RemoveAt(rand);

        // Alinear
        float spacing = 1.0f;
        for (int i = 0; i < dealerCards.Count; i++)
        {
            dealerCards[i].transform.localPosition = new Vector3(i * spacing, -i * 0.05f, 0);
        }
    }

    int GetCardValue(Sprite cardSprite)
    {
        string name = cardSprite.name;

        // As
        if (name.StartsWith("A"))
            return 11;

        // Figuras
        if (name.StartsWith("K") || name.StartsWith("Q") || name.StartsWith("J"))
            return 10;

        // 10 explícito
        if (name.StartsWith("10"))
            return 10;

        // Cartas 2-9
        int value;
        if (int.TryParse(name.Substring(0, 1), out value))
            return value;

        Debug.LogWarning($"No se pudo parsear la carta: {name}");
        return 0;
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

            // Ocultar segunda carta del dealer si aún está oculta
            if (hideDealer && hand == dealerCards && i == 1)
                continue;

            int value = GetCardValue(cardSprite);

            if (value == 11) aces++;
            total += value;
        }

        // Ajustar Ases de 11 a 1 si se pasa de 21
        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    // ---------------------
    //  Actualizar puntaje UI
    // ---------------------
    void UpdateScoreUI(bool hideDealer = true)
    {
        int playerScore = CalculateHandValue(playerCards);
        int dealerScore = CalculateHandValue(dealerCards, hideDealer); // oculta la segunda carta

        if (playerScoreText != null) playerScoreText.text = playerScore.ToString();
        if (dealerScoreText != null) dealerScoreText.text = dealerScore.ToString();
    }

    // ---------------------
    //  Turnos del jugador
    // ---------------------
    void PlayerHit()
    {
        if (isGameOver) return;

        GiveCard(playerCardsPos, playerCards, true);

        if (CalculateHandValue(playerCards) > 21)
        {
            EndRound("¡Te pasaste! Dealer gana.");
        }
    }

    void PlayerStand()
    {
        if (isGameOver) return;
        DealerTurn();
    }

    void PlayerDouble()
    {
        if (isGameOver) return;

        GiveCard(playerCardsPos, playerCards, true);

        if (CalculateHandValue(playerCards) > 21)
        {
            EndRound("¡Te pasaste al doblar! Dealer gana.");
            return;
        }

        DealerTurn();
    }

    // ---------------------
    //  Turno del dealer
    // ---------------------
    void DealerTurn()
    {
        // Ocultar botones
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);

        // Revelar carta oculta
        dealerCards[1].GetComponent<SpriteRenderer>().sprite = hiddenCardSprite;

        // Dealer roba hasta 17
        while (CalculateHandValue(dealerCards) < 17)
        {
            GiveCard(dealerCardsPos, dealerCards, true);
        }

        int playerScore = CalculateHandValue(playerCards);
        int dealerScore = CalculateHandValue(dealerCards);

        if (dealerScore > 21 || dealerScore < playerScore)
            EndRound("¡Ganaste!");
        else if (dealerScore == playerScore)
            EndRound("Empate.");
        else
            EndRound("Dealer gana.");
    }

    void EndRound(string result)
    {
        isGameOver = true;
        Debug.Log(result);

        if (resultPanel != null)
        {
            resultText.text = result;
            resultPanel.SetActive(true);
            resultText.gameObject.SetActive(true);
        }

        playButton.gameObject.SetActive(true);
        playButton.transform.SetAsLastSibling();

        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);


        UpdateScoreUI(false); // Actualiza puntaje final con carta revelada
    }
}