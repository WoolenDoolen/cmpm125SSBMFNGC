using UnityEngine;
using UnityEngine.Events;

namespace RockAndHardPlace
{
    [RequireComponent(typeof(Animator))]
    public sealed class Mountain : MonoBehaviour
    {
        [Header("Ore")]
        [Tooltip("Total ore in the mountain at the start.")]
        [SerializeField, Min(1)] private int startingOre = 80;
        [Tooltip("Ore given to the shooter per hit. The last hit gives whatever is left.")]
        [SerializeField, Min(1)] private int orePerHit = 20;

        [Header("Leaving")]
        [Tooltip("Plays the leave animation. Defaults to the Animator on this object.")]
        [SerializeField] private Animator animator;
        [Tooltip("Animator trigger parameter that starts the leave animation.")]
        [SerializeField] private string leaveTrigger = "Leave";

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
            if (animator == null) animator = GetComponent<Animator>();
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
            animator.SetTrigger(leaveTrigger);
        }

        // Called by the Animation Event on the last frame of the leave clip.
        public void OnLeaveFinished()
        {
            if (HasLeft) return;
            HasLeft = true;
            onLeft.Invoke();
            gameObject.SetActive(false);
        }
    }
}
