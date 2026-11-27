using UnityEngine;

namespace RockAndHardPlace
{
    [RequireComponent(typeof(Health), typeof(OreBank))]
    public sealed class Turret : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Blue";

        [Header("Parts")]
        [Tooltip("Rotates to aim the barrel.")]
        [SerializeField] private Transform barrelPivot;
        [Tooltip("Barrel tip. Shells spawn here and fly in the direction from the pivot to this point.")]
        [SerializeField] private Transform muzzle;

        [Header("Weapon")]
        [SerializeField] private WeaponData weapon;
        [SerializeField, Min(0)] private int damageBonus = 0;

        [Header("Aim limits")]
        [SerializeField, Range(0, 90)] private float minAngle = 5;
        [SerializeField, Range(0, 90)] private float maxAngle = 85;
        [SerializeField, Min(0)] private float minPower = 5;
        [SerializeField, Min(0)] private float maxPower = 20;

        [Header("Starting aim")]
        [SerializeField, Range(0, 90)] private float startAngle = 40;
        [SerializeField, Min(0)] private float startPower = 8;

        public string DisplayName => displayName;
        public WeaponData Weapon => weapon;
        public Health Health { get; private set; }
        public OreBank Bank { get; private set; }
        public float Angle { get; private set; }
        public float Power { get; private set; }

        public int Damage => weapon == null ? 0 : (Bank.Upgraded ? weapon.UpgradedDamage : weapon.Damage) + damageBonus;

        // Works for mirrored turrets (Scale X = -1) because it uses world positions.
        public Vector2 AimDirection => ((Vector2)(muzzle.position - barrelPivot.position)).normalized;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Bank = GetComponent<OreBank>();
            SetAim(startAngle, startPower);
        }

        public void SetAim(float angle, float power)
        {
            Angle = Mathf.Clamp(angle, minAngle, maxAngle);
            Power = Mathf.Clamp(power, minPower, maxPower);
            RotateBarrel(Angle);
        }

        public void AdjustAim(float angleDelta, float powerDelta) => SetAim(Angle + angleDelta, Power + powerDelta);
        public void IncreaseDamage(int amount)
        {
            if (amount <= 0) return;
            damageBonus += amount;
        }

        public Projectile Fire()
        {
            if (weapon == null || weapon.ShellPrefab == null || muzzle == null)
            {
                Debug.LogError(name + " needs a Weapon with a Shell Prefab, and a Muzzle.", this);
                return null;
            }
            var shell = Instantiate(weapon.ShellPrefab, muzzle.position, Quaternion.identity);
            shell.Launch(this, AimDirection * Power);
            return shell;
        }

        [ContextMenu("Test Fire")]
        private void TestFire()
        {
            if (Application.isPlaying) Fire();
            else Debug.LogWarning("Test Fire only works in Play mode.", this);
        }

        private void RotateBarrel(float angle)
        {
            if (barrelPivot != null) barrelPivot.localRotation = Quaternion.Euler(0, 0, angle);
        }

        // Keeps limits sensible and previews the starting angle in the Scene view.
        private void OnValidate()
        {
            maxAngle = Mathf.Max(maxAngle, minAngle);
            maxPower = Mathf.Max(maxPower, minPower);
            startAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
            startPower = Mathf.Clamp(startPower, minPower, maxPower);
            if (!Application.isPlaying) RotateBarrel(startAngle);
        }
    }
}
