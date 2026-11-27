using System.Collections.Generic;
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
        private readonly HashSet<UpgradeId> upgrades = new HashSet<UpgradeId>();

        public bool Upgraded => HasUpgrade(UpgradeId.ForgedShells);
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

        public bool HasUpgrade(UpgradeId upgrade) => upgrades.Contains(upgrade);

        public bool TryBuyUpgrade(int cost) => TryUnlock(UpgradeId.ForgedShells, cost);

        internal bool TryUnlock(UpgradeId upgrade, int cost)
        {
            if (cost < 0 || Ore < cost || !upgrades.Add(upgrade)) return false;
            Ore -= cost;
            onOreChanged.Invoke(Ore);
            return true;
        }
    }
}
