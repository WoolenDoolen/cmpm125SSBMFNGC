using UnityEngine;

namespace RockAndHardPlace
{
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Rock and Hard Place/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Tooltip("Prefab fired by this weapon. It must have a Projectile component.")]
        [SerializeField] private Projectile shellPrefab;
        [Tooltip("Direct-hit damage before the upgrade.")]
        [SerializeField, Min(0)] private int damage = 20;
        [Tooltip("Direct-hit damage after the upgrade.")]
        [SerializeField, Min(0)] private int upgradedDamage = 30;
        [Tooltip("Ore cost of the damage upgrade.")]
        [SerializeField, Min(0)] private int upgradeCost = 20;

        public Projectile ShellPrefab => shellPrefab;
        public int Damage => damage;
        public int UpgradedDamage => upgradedDamage;
        public int UpgradeCost => upgradeCost;
    }
}
