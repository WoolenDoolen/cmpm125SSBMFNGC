using UnityEngine;

namespace RockAndHardPlace
{
    public sealed class ArtilleryShell : MonoBehaviour
    {
        private PrototypeGame game;
        private ArtilleryTank shooter;
        private bool finished;
        private float lifetime;

        public void Launch(PrototypeGame match, ArtilleryTank tank)
        {
            game = match;
            shooter = tank;
            transform.position = tank.Muzzle;
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var hitbox = gameObject.AddComponent<CircleCollider2D>();
            hitbox.radius = 0.13f;
            Physics2D.IgnoreCollision(hitbox, tank.GetComponent<Collider2D>());
            body.linearVelocity = tank.Direction * tank.power;
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            if (lifetime > 6 || transform.position.y < -4 || Mathf.Abs(transform.position.x) > 25)
                Finish(null);
        }

        private void OnCollisionEnter2D(Collision2D hit) => Finish(hit.collider);

        private void Finish(Collider2D hit)
        {
            if (finished) return;
            finished = true;
            game.ShotFinished(shooter, hit);
            Destroy(gameObject);
        }
    }
}
