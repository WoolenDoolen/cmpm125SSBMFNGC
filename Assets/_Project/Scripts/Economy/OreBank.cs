using UnityEngine;
using UnityEngine.Events;

namespace RockAndHardPlace
{
    public sealed class OreBank : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingOre;

        [Header("Events")]
        [Tooltip("Called whenever ore changes. Passes the new total.")]
        [SerializeField] private UnityEvent<int> onOreChanged = new UnityEvent<int>();

        public int Ore { get; private set; }
        public bool Upgraded { get; private set; }
        public UnityEvent<int> OnOreChanged => onOreChanged;

        private void Awake() => Ore = startingOre;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Ore += amount;
            onOreChanged.Invoke(Ore);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Ore < amount) return false;
            Ore -= amount;
            onOreChanged.Invoke(Ore);
            return true;
        }

        public bool TryBuyUpgrade(int cost)
        {
            if (Upgraded || !TrySpend(cost)) return false;
            Upgraded = true;
            return true;
        }
    }
}
