using System.Collections.Generic;
using UnityEngine;

namespace RockAndHardPlace
{
    // Follows the same fixed steps and circle collider as the cannon shell.
    internal sealed class ShotTrajectory
    {
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        private readonly Collider2D playArea;

        public ShotTrajectory()
        {
            var area = GameObject.FindGameObjectWithTag("PlayArea");
            if (area != null) playArea = area.GetComponent<Collider2D>();
        }

        public RaycastHit2D Trace(Turret turret, List<Vector3> points = null)
        {
            points?.Clear();
            var shell = turret.Weapon?.ShellPrefab;
            if (shell == null || !shell.TryGetComponent<CircleCollider2D>(out var circle) ||
                !shell.TryGetComponent<Rigidbody2D>(out var body)) return default;

            Vector3 scale = shell.transform.localScale;
            Vector2 offset = circle.offset * (Vector2)scale;
            float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            Vector2 position = turret.MuzzlePosition;
            Vector2 velocity = turret.LaunchVelocity;
            Vector2 gravity = Physics2D.gravity * body.gravityScale;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(shell.gameObject.layer));

            points?.Add(position);
            float dt = Time.fixedDeltaTime;
            float damping = 1 / (1 + dt * body.linearDamping);
            for (float time = dt; time <= shell.MaxLifetime; time += dt)
            {
                velocity = (velocity + gravity * dt) * damping;
                Vector2 move = velocity * dt;
                float distance = move.magnitude;
                if (distance > 0)
                {
                    Physics2D.CircleCast(position + offset, radius, move / distance, filter, hits, distance);
                    foreach (var hit in hits)
                    {
                        if (hit.collider.transform.IsChildOf(turret.transform)) continue;
                        points?.Add(hit.centroid - offset);
                        return hit;
                    }
                }
                position += move;
                points?.Add(position);
                if (playArea != null)
                {
                    Vector2 center = position + offset;
                    if ((playArea.ClosestPoint(center) - center).sqrMagnitude > radius * radius) break;
                }
            }
            return default;
        }
    }
}
