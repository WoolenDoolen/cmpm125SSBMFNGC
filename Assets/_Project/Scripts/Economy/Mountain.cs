using UnityEngine;
using UnityEngine.Events;

namespace RockAndHardPlace
{
    [RequireComponent(typeof(MountainEscape))]
    public sealed class Mountain : MonoBehaviour
    {
        [Header("Ore")]
        [Tooltip("Total ore in the mountain at the start.")]
        [SerializeField, Min(1)] private int startingOre = 80;
        [Tooltip("Ore given to the shooter per hit. The last hit gives whatever is left.")]
        [SerializeField, Min(1)] private int orePerHit = 20;

        [Header("Leaving")]
        [Tooltip("Wakes up and runs away relative to the mountain's placed position.")]
        [SerializeField] private MountainEscape escape;

        [Header("Events")]
        [Tooltip("Called after each successful hit. Passes the ore remaining.")]
        [SerializeField] private UnityEvent<int> onMined = new UnityEvent<int>();
        [Tooltip("Called once when the ore runs out, as the leave animation starts.")]
        [SerializeField] private UnityEvent onDepleted = new UnityEvent();
        [Tooltip("Called once when the leave animation finishes.")]
        [SerializeField] private UnityEvent onLeft = new UnityEvent();

        public int StartingOre => startingOre;
        public int OrePerHit => orePerHit;
        public int OreRemaining { get; private set; }
        public bool IsDepleted => OreRemaining <= 0;
        public bool HasLeft { get; private set; }

        private void Awake()
        {
            OreRemaining = startingOre;
            if (escape == null) escape = GetComponent<MountainEscape>();
        }

        // True if the collider belongs to this mountain or one of its children.
        public bool Owns(Collider2D hit) => hit != null && hit.transform.IsChildOf(transform);

        // Gives ore to the bank and returns how much was mined.
        public int Mine(OreBank bank)
        {
            if (IsDepleted || bank == null) return 0;
            int mined = Mathf.Min(orePerHit, OreRemaining);
            OreRemaining -= mined;
            bank.Add(mined);
            onMined.Invoke(OreRemaining);
            if (IsDepleted) StartLeaving();
            return mined;
        }

        private void StartLeaving()
        {
            foreach (var part in GetComponentsInChildren<Collider2D>())
                part.enabled = false;
            onDepleted.Invoke();
            escape.Begin();
        }

        // The escape controller calls this after the mountain disappears into the valley.
        public void OnLeaveFinished()
        {
            if (HasLeft || !IsDepleted) return;
            HasLeft = true;
            onLeft.Invoke();
            gameObject.SetActive(false);
        }
    }
}
