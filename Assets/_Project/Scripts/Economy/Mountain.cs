using System;
using System.Collections.Generic;
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
        public bool HasLeft => travel == Travel.Away || travel == Travel.Returning;

        private enum Travel { Resting, Leaving, Away, Returning }
        private readonly List<Collider2D> disabledColliders = new List<Collider2D>();
        private Travel travel;
        private Action onReturned;

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
            travel = Travel.Leaving;
            foreach (var part in GetComponentsInChildren<Collider2D>())
            {
                if (!part.enabled) continue;
                part.enabled = false;
                disabledColliders.Add(part);
            }
            onDepleted.Invoke();
            escape.Begin();
        }

        // The escape controller calls this after the mountain disappears into the valley.
        public void OnLeaveFinished()
        {
            if (travel != Travel.Leaving) return;
            travel = Travel.Away;
            gameObject.SetActive(false);
            onLeft.Invoke();
        }

        internal bool TryReturn(Action completed)
        {
            if (travel != Travel.Away) return false;
            travel = Travel.Returning;
            onReturned = completed;
            gameObject.SetActive(true);
            escape.BeginReturn();
            return true;
        }

        internal void OnReturnFinished()
        {
            if (travel != Travel.Returning) return;
            travel = Travel.Resting;
            foreach (var part in disabledColliders)
                if (part != null) part.enabled = true;
            disabledColliders.Clear();
            Physics2D.SyncTransforms();

            var completed = onReturned;
            onReturned = null;
            completed?.Invoke();
        }
    }
}
