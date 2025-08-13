using UnityEngine;

namespace CasinoMania2D.Roulette
{
    public class PlacedChip : MonoBehaviour
    {
        private BetManager manager;
        public BetSpot Spot { get; private set; }
        public int Value { get; private set; }

        public void Init(BetManager m, BetSpot s, int value)
        {
            manager = m; Spot = s; Value = value;
        }

        private void OnMouseDown()
        {
            // Bloqueo global
            if (RouletteRoundController.Instance != null &&
                RouletteRoundController.Instance.IsInteractionLocked) return;

            int current = ChipSelector.Instance ? ChipSelector.Instance.CurrentChipValue : 0;

            if (current > 0)
                manager?.PlaceBet(Spot);                 // apilar si hay ficha activa
            else
                manager?.RemoveChipInstance(Spot, this); // borrar si no hay selección
        }
    }
}