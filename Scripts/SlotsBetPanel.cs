using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SlotsBetPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField betInput;
    public TextMeshProUGUI payoutPreviewText;
    public Button confirmButton;

    [Header("Reglas")]
    public int minBet = 10;
    public int maxBet = 5000;
    public int multiplier = 10;

    public event Action<int> BetConfirmed;

    int wallet = 0;
    int bet = 0;

    void OnEnable()
    {
        if (betInput)
        {
            betInput.onValueChanged.RemoveListener(OnInputChanged);
            betInput.onValueChanged.AddListener(OnInputChanged);
        }
        UpdatePreview();
    }

    public void Open(int currentWallet)
    {
        wallet = currentWallet;
        // el máximo permitido también está limitado por el wallet
        maxBet = Mathf.Min(maxBet, Mathf.Max(0, wallet));

        bet = 0;
        if (betInput) betInput.text = "";
        UpdatePreview();

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(Confirm);
        }
        gameObject.SetActive(true);
    }

    void OnInputChanged(string raw)
    {
        // Deja solo dígitos
        string digits = "";
        foreach (char c in raw)
            if (char.IsDigit(c)) digits += c;

        // Quita ceros iniciales
        digits = digits.TrimStart('0');

        if (digits.Length == 0)
        {
            bet = 0;
            if (betInput && betInput.text != "") betInput.text = "";
            UpdatePreview();
            return;
        }

        // Parse y clamp
        if (!int.TryParse(digits, out bet)) bet = 0;
        bet = Mathf.Clamp(bet, 0, maxBet);

        // Refleja texto saneado
        if (betInput && betInput.text != digits) betInput.text = (bet > 0) ? bet.ToString() : "";

        UpdatePreview();
    }

    void UpdatePreview()
    {
        bool valid = bet >= minBet && bet <= maxBet;
        if (confirmButton) confirmButton.interactable = valid;

        int potential = valid ? bet * multiplier : 0;
        if (payoutPreviewText)
            payoutPreviewText.text = valid
                ? $"Posible ganancia: {potential}"
                : $"Ingresa de {minBet} a {maxBet}";
    }

    void Confirm()
    {
        if (bet >= minBet && bet <= maxBet)
            BetConfirmed?.Invoke(bet);
    }
}
