using UnityEngine;
using System.Collections.Generic;

namespace CasinoMania2D.Roulette
{
    public class BetManager : MonoBehaviour
    {
        public static BetManager Instance { get; private set; }

        // ---- EVENTO para habilitar/deshabilitar el PLAY ----
        public event System.Action BetsChanged;
        private void NotifyBetsChanged() => BetsChanged?.Invoke();
        public bool HasAnyBets() => spotAmounts.Count > 0;

        public int GetTotalStake()
        {
            int t = 0;
            foreach (var kv in spotAmounts) t += kv.Value;
            return t;
        }

        [System.Serializable]
        public class ChipVisual
        {
            public int value;     // 10,20,50,100,500
            public Sprite sprite; // sprite de esa denominación
        }

        [Header("Visual de fichas")]
        [SerializeField] private ChipVisual[] chipSprites;
        [SerializeField] private GameObject chipPrefab; // Prefab con SpriteRenderer
        [SerializeField] private float stackOffsetY = 0.06f;
        [SerializeField] private Transform chipsParent; // opcional

        private readonly Dictionary<BetSpot, int> spotAmounts = new();
        private readonly Dictionary<BetSpot, List<GameObject>> spotChips = new();
        private readonly Stack<PlacedChip> placementHistory = new();

        private static readonly int[] reds = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        private static readonly int[] blacks = {2,4,6,8,10,11,13,15,17,20,22,24,26,28,29,31,33,35};

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Estado inicial (sin apuestas) para que el PLAY arranque en off
            NotifyBetsChanged();
        }

        public void PlaceBet(BetSpot spot)
        {
            int chip = ChipSelector.Instance ? ChipSelector.Instance.CurrentChipValue : 0;
            if (chip <= 0) return;

            if (!spotAmounts.ContainsKey(spot)) spotAmounts[spot] = 0;
            spotAmounts[spot] += chip;

            var chipGO = Instantiate(chipPrefab, spot.GetChipWorldPosition(), Quaternion.identity, chipsParent);
            chipGO.transform.localScale = Vector3.one * 2.5f;

            var sr = chipGO.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = GetChipSprite(chip);

            var col = chipGO.GetComponent<Collider2D>();
            if (!col) col = chipGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            if (!spotChips.ContainsKey(spot)) spotChips[spot] = new List<GameObject>();
            var list = spotChips[spot];
            chipGO.transform.position += Vector3.up * (stackOffsetY * list.Count);
            list.Add(chipGO);

            var placed = chipGO.AddComponent<PlacedChip>();
            placed.Init(this, spot, chip);
            placementHistory.Push(placed);

            Debug.Log($"[BM] PlaceBet {spot.name} +{chip} → total spot={spotAmounts[spot]} all={GetTotalStake()}");
            NotifyBetsChanged();
        }

        public bool UndoLastChip()
        {
            // Saca de la pila hasta encontrar uno vivo
            while (placementHistory.Count > 0)
            {
                var pc = placementHistory.Pop();
                if (pc != null && pc.gameObject != null)
                {
                    RemoveChipInstance(pc.Spot, pc); // ya hace NotifyBetsChanged()
                    return true;
                }
            }
            return false;
        }

        private Sprite GetChipSprite(int value)
        {
            foreach (var c in chipSprites) if (c.value == value) return c.sprite;
            return null;
        }

        internal void RemoveChipInstance(BetSpot spot, PlacedChip chipComp)
        {
            if (!spotChips.TryGetValue(spot, out var list)) return;
            var go = chipComp.gameObject;
            if (list.Remove(go))
            {
                spotAmounts[spot] -= chipComp.Value;
                if (spotAmounts[spot] <= 0) { spotAmounts.Remove(spot); spotChips.Remove(spot); }
                Destroy(go);
                Debug.Log($"[BM] RemoveChip {spot.name} -{chipComp.Value} → all={GetTotalStake()}");
                NotifyBetsChanged();
            }
        }

        public void RemoveTopChip(BetSpot spot)
        {
            if (!spotChips.TryGetValue(spot, out var list) || list.Count == 0) return;
            var go = list[^1];
            var pc = go.GetComponent<PlacedChip>();
            if (pc != null) RemoveChipInstance(spot, pc);
            else
            {
                list.RemoveAt(list.Count - 1);
                Destroy(go);
                RecomputeSpotAmount(spot);
                NotifyBetsChanged();
            }
        }

        private void RecomputeSpotAmount(BetSpot spot)
        {
            if (!spotChips.TryGetValue(spot, out var list)) { spotAmounts.Remove(spot); return; }
            int sum = 0;
            foreach (var go in list) { var pc = go ? go.GetComponent<PlacedChip>() : null; if (pc) sum += pc.Value; }
            if (sum > 0) spotAmounts[spot] = sum; else { spotAmounts.Remove(spot); spotChips.Remove(spot); }
        }

        public void ClearAllBets()
        {
            foreach (var kv in spotChips)
                foreach (var go in kv.Value) if (go) Destroy(go);
            spotChips.Clear();
            spotAmounts.Clear();
            placementHistory.Clear();     
            Debug.Log("[BM] ClearAll");
            NotifyBetsChanged();
        }

        public int GetAmountOnSpot(BetSpot spot) => spotAmounts.TryGetValue(spot, out var a) ? a : 0;

        public IReadOnlyCollection<int> GetCoveredNumbers(BetSpot spot)
        {
            if (spot.group == BetGroup.Straight && spot.coveredNumbers.Count > 0)
                return spot.coveredNumbers;

            switch (spot.group)
            {
                case BetGroup.Even:   return BuildRange(n => n >= 1 && n <= 36 && n % 2 == 0);
                case BetGroup.Odd:    return BuildRange(n => n >= 1 && n <= 36 && n % 2 != 0);
                case BetGroup.Red:    return reds;
                case BetGroup.Black:  return blacks;
                case BetGroup.Low:    return BuildRange(n => n >= 1 && n <= 18);
                case BetGroup.High:   return BuildRange(n => n >= 19 && n <= 36);
                case BetGroup.Dozen1: return BuildRange(n => n >= 1 && n <= 12);
                case BetGroup.Dozen2: return BuildRange(n => n >= 13 && n <= 24);
                case BetGroup.Dozen3: return BuildRange(n => n >= 25 && n <= 36);
                case BetGroup.Column1: return BuildRange(n => n >= 1 && n <= 36 && (n % 3 == 1));
                case BetGroup.Column2: return BuildRange(n => n >= 1 && n <= 36 && (n % 3 == 2));
                case BetGroup.Column3: return BuildRange(n => n >= 1 && n <= 36 && (n % 3 == 0));
                default: return spot.coveredNumbers;
            }
        }

        private static int[] BuildRange(System.Func<int, bool> pred)
        {
            List<int> list = new();
            for (int n = 0; n <= 36; n++)
                if (pred(n)) list.Add(n);
            return list.ToArray();
        }

        // Exponer estado para el controlador
        public Dictionary<BetSpot, int> GetAllBets() => new(spotAmounts);
    }
}
