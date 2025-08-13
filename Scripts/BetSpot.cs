using UnityEngine;
using System.Collections.Generic;

namespace CasinoMania2D.Roulette
{
    public enum BetGroup
    {
        Straight, Even, Odd, Red, Black, Low, High,
        Dozen1, Dozen2, Dozen3, Column1, Column2, Column3
    }

    [RequireComponent(typeof(Collider2D))]
    public class BetSpot : MonoBehaviour
    {
        [Header("Tipo de apuesta")]
        public BetGroup group = BetGroup.Straight;

        [Tooltip("Para Straight: el número único (0-36). Para grupos, dejar vacío.")]
        public List<int> coveredNumbers = new();

        [Header("Punto donde spawnear fichas (opcional)")]
        public Transform chipAnchor;

        private void OnMouseDown()
        {
            if (RouletteRoundController.Instance != null &&
                RouletteRoundController.Instance.IsInteractionLocked) return;
            int chip = ChipSelector.Instance ? ChipSelector.Instance.CurrentChipValue : 0;

            if (chip > 0)
                BetManager.Instance?.PlaceBet(this);
            else
                BetManager.Instance?.RemoveTopChip(this); // ← borrar el último chip del spot
        }

        public Vector3 GetChipWorldPosition() => chipAnchor ? chipAnchor.position : transform.position;
    }
}
