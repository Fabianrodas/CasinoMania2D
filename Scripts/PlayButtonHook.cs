using UnityEngine;
using UnityEngine.UI;
using CasinoMania2D.Roulette;

public class PlayButtonHook : MonoBehaviour
{
    [SerializeField] private Button playBtn;

    private void Awake()
    {
        playBtn.onClick.AddListener(OnPlay);
    }

    private void OnPlay()
    {
        // Aquí luego haremos: girar fuerte, frenar y caer en handler_X ganador
        var allBets = BetManager.Instance.GetAllBets();
        foreach (var kv in allBets)
        {
            var spot = kv.Key;
            int amount = kv.Value;
            var covered = BetManager.Instance.GetCoveredNumbers(spot);
            Debug.Log($"PLAY -> Spot {spot.name} ({spot.group}) apostado {amount} cubre [{string.Join(",", covered)}]");
        }
    }
}