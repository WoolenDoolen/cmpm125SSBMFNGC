using UnityEngine;

namespace RockAndHardPlace
{
    // The solo opponent scouts shots in the same 2D battlefield that the player uses.
    internal sealed class CastleOpponent
    {
        private static readonly float[] Angles = { 55, 48, 60, 63, 44 };
        private const float ThinkTime = 0.55f;
        private const float AimTime = 0.42f;
        private readonly GameManager game;
        private readonly ShotTrajectory trajectory = new ShotTrajectory();
        private float elapsed;
        private int shots;
        private bool hasPlan;
        private Vector2 initialAim;
        private Vector2 plannedAim;

        public CastleOpponent(GameManager game)
        {
            this.game = game;
        }

        public void ResetTurn()
        {
            elapsed = 0;
            hasPlan = false;
        }

        public void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed < ThinkTime) return;

            var turret = game.CurrentTurret;
            if (!hasPlan)
            {
                initialAim = new Vector2(turret.Angle, turret.Power);
                plannedAim = FindShot(turret);
                turret.SetAim(initialAim.x, initialAim.y);
                BuyUpgrade();
                hasPlan = true;
            }

            float progress = Mathf.Clamp01((elapsed - ThinkTime) / AimTime);
            Vector2 aim = Vector2.Lerp(initialAim, plannedAim, Mathf.SmoothStep(0, 1, progress));
            turret.SetAim(aim.x, aim.y);
            if (progress >= 1 && game.TryFireCpu()) shots++;
        }

        private Vector2 FindShot(Turret turret)
        {
            bool mining = game.Phase == GamePhase.Mining;
            // Start near the usual range, then vary power on either side. Bound the search
            // so turns remain responsive even if a later map makes the target unreachable.
            float rangePower = mining ? 6 : 12;
            for (int i = 0; i < Angles.Length; i++)
            {
                float angle = Angles[(shots + i) % Angles.Length];
                for (int step = 0; step < 21; step++)
                {
                    float distance = ((step + 1) / 2) * 0.5f;
                    float power = rangePower + (step % 2 == 1 ? distance : -distance);
                    turret.SetAim(angle, power);
                    var hit = trajectory.Trace(turret);
                    if (hit.collider == null || hit.normal.y < 0.5f) continue;
                    bool foundTarget = mining ? game.Mountain.Owns(hit.collider) :
                        hit.collider.GetComponentInParent<Turret>() == game.Turrets[0];
                    if (foundTarget) return new Vector2(turret.Angle, turret.Power);
                }
            }
            return new Vector2(55, rangePower);
        }

        private void BuyUpgrade()
        {
            // One purchase per turn, using the same ore and prerequisites as a human.
            foreach (var upgrade in CastleUpgrades.Nodes)
                if (game.TryBuyCpuUpgrade(upgrade.Id)) return;
        }
    }
}
