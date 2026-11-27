using System.Collections.Generic;

namespace RockAndHardPlace
{
    public enum UpgradeId { ForgedShells, HeavyShot, ReinforcedWalls, InnerKeep }
    public enum UpgradeState { Available, Locked, NeedOre, Purchased }

    public readonly struct CastleUpgrade
    {
        public UpgradeId Id { get; }
        public string Name { get; }
        public UpgradeId? Requires { get; }
        public int HealthBonus { get; }

        internal CastleUpgrade(UpgradeId id, string name, UpgradeId? requires = null, int healthBonus = 0)
        {
            Id = id;
            Name = name;
            Requires = requires;
            HealthBonus = healthBonus;
        }

        public int Cost(WeaponData weapon) => Id == UpgradeId.ForgedShells ? weapon.UpgradeCost : 20;

        public string Effect(WeaponData weapon)
        {
            if (HealthBonus > 0) return "+" + HealthBonus + " max HP";
            int damage = Id == UpgradeId.ForgedShells ? weapon.UpgradedDamage - weapon.Damage : CastleUpgrades.HeavyShotDamage;
            return "+" + damage + " damage";
        }
    }

    public static class CastleUpgrades
    {
        public const int HeavyShotDamage = 10;

        // Each branch has one entry and one follow-up. Teams can invest in either or both.
        public static IReadOnlyList<CastleUpgrade> Nodes { get; } = new[]
        {
            new CastleUpgrade(UpgradeId.ForgedShells, "Forged Shells"),
            new CastleUpgrade(UpgradeId.HeavyShot, "Heavy Shot", UpgradeId.ForgedShells),
            new CastleUpgrade(UpgradeId.ReinforcedWalls, "Reinforced Walls", healthBonus: 25),
            new CastleUpgrade(UpgradeId.InnerKeep, "Inner Keep", UpgradeId.ReinforcedWalls, healthBonus: 20)
        };

        public static CastleUpgrade Get(UpgradeId id) => Nodes[(int)id];

        public static UpgradeState StateFor(CastleUpgrade upgrade, Turret turret)
        {
            if (turret.Bank.HasUpgrade(upgrade.Id)) return UpgradeState.Purchased;
            if (upgrade.Requires is UpgradeId parent && !turret.Bank.HasUpgrade(parent)) return UpgradeState.Locked;
            return turret.Bank.Ore >= upgrade.Cost(turret.Weapon) ? UpgradeState.Available : UpgradeState.NeedOre;
        }
    }
}
