using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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

    private List<GameObject> playerCards = new List<GameObject>();
    private List<GameObject> dealerCards = new List<GameObject>();
    private List<Sprite> deckInGame = new List<Sprite>();

    private bool isGameOver = false;
    private Sprite hiddenCardSprite;

    void Start()
    {
        // Ocultar UI inicial
        hitButton.gameObject.SetActive(false);
        standButton.gameObject.SetActive(false);
        doubleButton.gameObject.SetActive(false);
        panel1.SetActive(false);
        playerScoreText.gameObject.SetActive(false);
        dealerScoreText.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);


        playButton.onClick.AddListener(StartGame);
        hitButton.onClick.AddListener(PlayerHit);
        standButton.onClick.AddListener(PlayerStand);
        doubleButton.onClick.AddListener(PlayerDouble);

        UpdateScoreUI();
    }

    void StartGame()
    {
        StopAllCoroutines();
        isGameOver = false;

        // Reset UI
        if (resultPanel != null) resultPanel.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);
        resultText.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        playerScoreText.gameObject.SetActive(true);
        dealerScoreText.gameObject.SetActive(true);

        // Limpiar cartas viejas
        foreach (var c in playerCards) if (c != null) Destroy(c);
        foreach (var c in dealerCards) if (c != null) Destroy(c);
        playerCards.Clear();
        dealerCards.Clear();

        CreateDeck();
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
        // Desactivar botones durante la animación
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
        if (CalculateHandValue(playerCards) > 21) EndRound("¡Te pasaste! Dealer gana.");
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
            EndRound("¡Te pasaste al doblar! Dealer gana.");
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
            EndRound("¡Ganaste!");
        else if (dealerScore == playerScore)
            EndRound("Empate.");
        else
            EndRound("Dealer gana.");
    }

    void EndRound(string result)
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
        panel1.SetActive(false);
        UpdateScoreUI(false);
    }
}