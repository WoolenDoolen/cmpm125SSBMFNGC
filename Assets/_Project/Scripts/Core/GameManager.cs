using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RockAndHardPlace
{
    public enum GamePhase { Mining, MountainLeaving, Combat, GameOver }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("Scene references")]
        [Tooltip("Turn order. The first turret goes first.")]
        [SerializeField] private Turret[] turrets;
        [SerializeField] private Mountain mountain;
        [SerializeField] private LootManager lootManager;

        [Header("Aiming")]
        [Tooltip("Degrees per second while holding the aim input.")]
        [SerializeField, Min(0)] private float angleSpeed = 30;
        [Tooltip("Power per second while holding the power input.")]
        [SerializeField, Min(0)] private float powerSpeed = 5;

        [Header("Events")]
        [SerializeField] private UnityEvent<GamePhase> onPhaseChanged = new UnityEvent<GamePhase>();
        [Tooltip("Passes the turret whose turn just started.")]
        [SerializeField] private UnityEvent<Turret> onTurnStarted = new UnityEvent<Turret>();
        [Tooltip("Passes the turret that fired.")]
        [SerializeField] private UnityEvent<Turret> onShotFired = new UnityEvent<Turret>();
        [Tooltip("Passes the winning turret.")]
        [SerializeField] private UnityEvent<Turret> onGameOver = new UnityEvent<Turret>();

        public GamePhase Phase { get; private set; }
        public int CurrentIndex { get; private set; }
        public Turret CurrentTurret => turrets[CurrentIndex];
        public IReadOnlyList<Turret> Turrets => turrets;
        public Mountain Mountain => mountain;
        public Turret Winner { get; private set; }
        public bool ShotActive => activeShell != null;
        public string LastEvent { get; private set; } = "Shoot the mountain to collect ore.";

        private Projectile activeShell;

        private bool CanAct => !ShotActive && (Phase == GamePhase.Mining || Phase == GamePhase.Combat);

        private void Awake()
        {
            if (turrets == null || turrets.Length < 2 || mountain == null)
            {
                Debug.LogError("GameManager needs at least two Turrets and the Mountain assigned.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            SetPhase(GamePhase.Mining);
            onTurnStarted.Invoke(CurrentTurret);
        }

        // Inputs are -1 to 1; InputReader calls this every frame.
        public void Aim(float angleInput, float powerInput)
        {
            if (!enabled || !CanAct) return;
            CurrentTurret.AdjustAim(angleInput * angleSpeed * Time.deltaTime, powerInput * powerSpeed * Time.deltaTime);
        }

        public bool TryFire()
        {
            if (!enabled || !CanAct) return false;
            var shell = CurrentTurret.Fire();
            if (shell == null) return false;
            activeShell = shell;
            shell.Resolved += OnShotResolved;
            onShotFired.Invoke(CurrentTurret);
            return true;
        }

        public bool TryBuyUpgrade()
        {
            if (!enabled || !CanAct) return false;
            var turret = CurrentTurret;
            if (turret.Weapon == null || !turret.Bank.TryBuyUpgrade(turret.Weapon.UpgradeCost)) return false;
            LastEvent = turret.DisplayName + " bought the upgrade: damage is now " + turret.Damage + ".";
            return true;
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Hook this to Mountain > On Left in the Inspector.
        public void OnMountainLeft()
        {
            if (Phase != GamePhase.MountainLeaving) return;
            SetPhase(GamePhase.Combat);
            LastEvent = "Mountain cleared. Direct hits now damage tanks.";
            NextTurn();
        }

        private void OnShotResolved(Projectile shell, Collider2D hit)
        {
            shell.Resolved -= OnShotResolved;
            activeShell = null;
            var shooter = shell.Shooter;
            LastEvent = shooter.DisplayName + " missed.";

            if (Phase == GamePhase.Mining && mountain.Owns(hit))
            {
                int mined = mountain.Mine(shooter.Bank);

                string lootMessage = "";
                if (mined > 0 && lootManager != null)
                    lootMessage = lootManager.AwardLoot(shooter);

                LastEvent = shooter.DisplayName + " mined " + mined + " ore.";

                if (!string.IsNullOrEmpty(lootMessage))
                    LastEvent += " " + lootMessage;

                if (mountain.IsDepleted)
                {
                    SetPhase(GamePhase.MountainLeaving);
                    LastEvent = "The mountain is empty and leaving... " + lootMessage;
                    return;
                }
            }
            else if (Phase == GamePhase.Combat && hit != null)
            {
                var target = hit.GetComponentInParent<Turret>();
                if (target != null && target != shooter)
                {
                    int damage = shooter.Damage;
                    target.Health.TakeDamage(damage);
                    LastEvent = shooter.DisplayName + " hit " + target.DisplayName + " for " + damage + ".";
                    if (target.Health.IsDead)
                    {
                        EndGame(shooter);
                        return;
                    }
                }
            }
            NextTurn();
        }

        private void NextTurn()
        {
            CurrentIndex = (CurrentIndex + 1) % turrets.Length;
            onTurnStarted.Invoke(CurrentTurret);
        }

        private void EndGame(Turret winner)
        {
            Winner = winner;
            SetPhase(GamePhase.GameOver);
            LastEvent = winner.DisplayName + " wins! Press Restart to play again.";
            onGameOver.Invoke(winner);
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            onPhaseChanged.Invoke(phase);
        }
    }
}
