using UnityEngine;
using UnityEngine.Events;

namespace RockAndHardPlace
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        [Header("Events")]
        [Tooltip("Called after taking damage. Passes the remaining health.")]
        [SerializeField] private UnityEvent<int> onDamaged = new UnityEvent<int>();
        [Tooltip("Called whenever current or maximum health changes. Passes current health")]
        [SerializeField] private UnityEvent<int> onHealthChanged = new UnityEvent<int>();
        [Tooltip("Called once when health reaches 0.")]
        [SerializeField] private UnityEvent onDied = new UnityEvent();

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;
        public UnityEvent<int> OnDamaged => onDamaged;
        public UnityEvent OnDied => onDied;
        public UnityEvent<int> OnHealthChanged => onHealthChanged;

        private void Awake() => Current = maxHealth;

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            onDamaged.Invoke(Current);
            onHealthChanged.Invoke(Current);
            if (IsDead) onDied.Invoke();
        }
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            int previousHealth = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            if (Current != previousHealth)
                onHealthChanged.Invoke(Current);
        }
        public void IncreaseMaxHealth(int amount)
        {
            if (IsDead || amount <= 0) return;
            maxHealth += amount;
            Current += amount;
            onHealthChanged.Invoke(Current);
        }
    }
}
