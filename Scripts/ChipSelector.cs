using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CasinoMania2D.Roulette
{
    public class ChipSelector : MonoBehaviour
    {
        public static ChipSelector Instance { get; private set; }

        [System.Serializable]
        public class ChipButtonRef
        {
            public int value;
            public Button button;
            public GameObject selectedGlow;
        }

        [Header("Botones de fichas")]
        [SerializeField] private List<ChipButtonRef> chips = new();

        public int CurrentChipValue { get; private set; } = 0;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var c in chips)
            {
                var local = c;
                if (local.button) local.button.onClick.AddListener(() => SelectChip(local.value));
                if (local.selectedGlow) local.selectedGlow.SetActive(false);
            }
        }

        void OnEnable()
        {
            // Suscríbete al lock del round controller
            if (RouletteRoundController.Instance != null)
                RouletteRoundController.Instance.LockStateChanged += OnLockChanged;

            // Aplica estado actual por si ya estaba bloqueado
            OnLockChanged(RouletteRoundController.Instance != null &&
                          RouletteRoundController.Instance.IsInteractionLocked);
        }

        void OnDisable()
        {
            if (RouletteRoundController.Instance != null)
                RouletteRoundController.Instance.LockStateChanged -= OnLockChanged;
        }

        private void OnLockChanged(bool locked)
        {
            // Habilita/Deshabilita los botones
            foreach (var c in chips)
                if (c.button) c.button.interactable = !locked;

            // Si se bloquea, quita selección visual y lógica
            if (locked) ClearSelection();
        }

        public void SelectChip(int value)
        {
            // Si está bloqueado, ignora
            if (RouletteRoundController.Instance != null &&
                RouletteRoundController.Instance.IsInteractionLocked) return;

            // Toggle: si es la misma, deselecciona
            if (CurrentChipValue == value) { ClearSelection(); return; }

            CurrentChipValue = value;
            foreach (var c in chips)
                if (c.selectedGlow) c.selectedGlow.SetActive(c.value == value);
        }

        public void ClearSelection()
        {
            CurrentChipValue = 0;
            foreach (var c in chips)
                if (c.selectedGlow) c.selectedGlow.SetActive(false);
        }
    }
}