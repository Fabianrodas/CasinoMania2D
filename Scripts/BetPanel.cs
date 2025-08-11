using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class BetPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI betText;
    [SerializeField] TextMeshProUGUI minmaxText;
    [SerializeField] Button confirmBetButton;   // <- PLAY del panel
    [SerializeField] Button undoButton;
    [SerializeField] Button clearButton;

    [Header("Límites")]
    [SerializeField] int minBet = 10;
    [SerializeField] int maxBet = 500;

    public int Wallet     { get; private set; }
    public int CurrentBet { get; private set; }

    public event Action<int> BetConfirmed;      // <- NUEVO evento

    readonly List<int> chips = new();

    void Awake()
    {
        if (confirmBetButton)
        {
            confirmBetButton.onClick.RemoveAllListeners();
            confirmBetButton.onClick.AddListener(OnConfirmBet);
        }
        else Debug.LogError("[BetPanel] Falta asignar confirmBetButton en el Inspector", this);

        if (undoButton)
        {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(Undo);
        }
        else Debug.LogError("[BetPanel] Falta asignar undoButton en el Inspector", this);

        if (clearButton)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(Clear);
        }
        else Debug.LogError("[BetPanel] Falta asignar clearButton en el Inspector", this);

        UpdateUI();
    }

    public void Open(int wallet)
    {
        Wallet = GlobalUI.Instance ? GlobalUI.Instance.CurrentWallet : 0;
        CurrentBet = 0;
        chips.Clear();
        UpdateUI();
        gameObject.SetActive(true);
        Debug.Log($"[BetPanel] Open wallet={Wallet}");
    }

    public void Init(int wallet) => Open(wallet);  // por compatibilidad

    public void AddChip(int value)
    {
        Debug.Log($"[BetPanel] AddChip {value} (wallet={Wallet}, current={CurrentBet})");
        if (value <= 0) return;
        if (CurrentBet + value > Wallet) return;
        if (CurrentBet + value > maxBet) return;

        chips.Add(value);
        CurrentBet += value;
        UpdateUI();
    }

    public void Undo()
    {
        if (chips.Count == 0) return;
        int last = chips[^1];
        chips.RemoveAt(chips.Count - 1);
        CurrentBet -= last;
        UpdateUI();
        Debug.Log($"[BetPanel] Undo -> current={CurrentBet}");
    }

    public void Clear()
    {
        chips.Clear();
        CurrentBet = 0;
        UpdateUI();
        Debug.Log("[BetPanel] Clear -> current=0");
    }

    void UpdateUI()
    {
        if (betText)    betText.text    = $"Apuesta actual: {CurrentBet}";
        if (minmaxText) minmaxText.text = $"Min: {minBet}   Max: {maxBet}   Saldo: {Wallet}";
        if (confirmBetButton)
            confirmBetButton.interactable = CurrentBet >= minBet && CurrentBet <= Mathf.Min(maxBet, Wallet);
    }

    void OnConfirmBet()
    {
        Debug.Log($"[BetPanel] Confirm pressed. current={CurrentBet}, wallet={Wallet}");
        if (!(CurrentBet >= minBet && CurrentBet <= Mathf.Min(maxBet, Wallet)))
        {
            Debug.Log("[BetPanel] Confirm ignorado por límites");
            return;
        }

        BetConfirmed?.Invoke(CurrentBet);

        gameObject.SetActive(false);
    }
}
