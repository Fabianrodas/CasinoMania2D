using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CasinoMania2D.Roulette
{
    public class BetToolbar : MonoBehaviour
    {
        [SerializeField] private Button undoButton;
        [SerializeField] private Button trashButton;
        [SerializeField] private RouletteRoundController roundController; // se puede dejar vacío; lo buscamos

        void Awake()
        {
            if (undoButton)
            {
                undoButton.onClick.RemoveAllListeners();
                undoButton.onClick.AddListener(OnUndo);
            }
            if (trashButton)
            {
                trashButton.onClick.RemoveAllListeners();
                trashButton.onClick.AddListener(OnTrash);
            }
        }

        void OnEnable()
        {
            if (!roundController) roundController = FindObjectOfType<RouletteRoundController>();

            // Suscribirse al estado de apuestas
            if (BetManager.Instance != null)
                BetManager.Instance.BetsChanged += RefreshButtons;
            else
                StartCoroutine(WaitAndSubBM());

            // Suscribirse al lock de la ronda
            if (roundController != null)
                roundController.LockStateChanged += OnLockChanged;

            RefreshButtons();
        }

        void OnDisable()
        {
            if (BetManager.Instance != null)
                BetManager.Instance.BetsChanged -= RefreshButtons;
            if (roundController != null)
                roundController.LockStateChanged -= OnLockChanged;
        }

        private IEnumerator WaitAndSubBM()
        {
            for (int i = 0; i < 120; i++)
            {
                if (BetManager.Instance != null)
                {
                    BetManager.Instance.BetsChanged -= RefreshButtons;
                    BetManager.Instance.BetsChanged += RefreshButtons;
                    RefreshButtons();
                    yield break;
                }
                yield return null;
            }
        }

        private void OnLockChanged(bool _)
        {
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool anyBets = BetManager.Instance != null && BetManager.Instance.HasAnyBets();
            bool locked  = roundController != null && roundController.IsInteractionLocked;

            bool interactable = anyBets && !locked;

            if (undoButton)  undoButton.interactable  = interactable;
            if (trashButton) trashButton.interactable = interactable;
        }

        private void OnUndo()
        {
            // Doble seguridad: no actuar si está bloqueado
            if (roundController != null && roundController.IsInteractionLocked) return;

            if (BetManager.Instance != null)
                BetManager.Instance.UndoLastChip();
        }

        private void OnTrash()
        {
            if (roundController != null && roundController.IsInteractionLocked) return;

            if (BetManager.Instance != null) BetManager.Instance.ClearAllBets();
            if (ChipSelector.Instance != null) ChipSelector.Instance.ClearSelection();
        }
    }
}