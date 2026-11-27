using System;
using UnityEngine;

namespace RockAndHardPlace
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [Tooltip("Seconds in the air before the shot counts as a miss.")]
        [SerializeField, Min(0.1f)] private float maxLifetime = 6;
        [Tooltip("Tag of the trigger that marks the playable area. Leaving it counts as a miss.")]
        [SerializeField] private string playAreaTag = "PlayArea";

        // Raised once when the shot ends. The collider is what it hit, or null for a miss.
        public event Action<Projectile, Collider2D> Resolved;
        public Turret Shooter { get; private set; }
        public float MaxLifetime => maxLifetime;
        public Vector2 ImpactPoint { get; private set; }

        private Rigidbody2D body;
        private float lifetime;
        private bool finished;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public void Launch(Turret shooter, Vector2 velocity)
        {
            Shooter = shooter;
            var hitbox = GetComponent<Collider2D>();
            foreach (var part in shooter.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(hitbox, part);
            body.linearVelocity = velocity;
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            if (lifetime > maxLifetime) Finish(null);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ImpactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            Finish(collision.collider);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playAreaTag)) Finish(null);
        }

        private void Finish(Collider2D hit)
        {
            if (finished) return;
            finished = true;
            Resolved?.Invoke(this, hit);
            Destroy(gameObject);
        }
    }
}
