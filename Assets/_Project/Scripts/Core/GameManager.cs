using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RockAndHardPlace
{
    public enum GamePhase { Mining, MountainLeaving, Combat, GameOver, Title }
    public enum MatchMode { TwoPlayer, SinglePlayer }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("Scene references")]
        [Tooltip("Turn order. The first turret goes first.")]
        [SerializeField] private Turret[] turrets;
        [SerializeField] private Mountain mountain;
        [SerializeField] private LootManager lootManager;
        [SerializeField] private bool startFromMenu;

        [Header("Aiming")]
        [Tooltip("Degrees per second while holding the aim input.")]
        [SerializeField, Min(0)] private float angleSpeed = 30;
        [Tooltip("Power per second while holding the power input.")]
        [SerializeField, Min(0)] private float powerSpeed = 5;

        [Header("Single Player")]
        [Tooltip("Multiplier applied to Red CPU damage after upgrades and loot.")]
        [SerializeField, Range(0.1f, 1f)] private float cpuDamageMultiplier = 0.6f;

        [Header("Events")]
        [SerializeField] private UnityEvent<GamePhase> onPhaseChanged = new UnityEvent<GamePhase>();
        [Tooltip("Passes the turret whose turn just started.")]
        [SerializeField] private UnityEvent<Turret> onTurnStarted = new UnityEvent<Turret>();
        [Tooltip("Passes the turret that fired.")]
        [SerializeField] private UnityEvent<Turret> onShotFired = new UnityEvent<Turret>();
        [Tooltip("Passes the winning turret.")]
        [SerializeField] private UnityEvent<Turret> onGameOver = new UnityEvent<Turret>();

        public GamePhase Phase { get; private set; }
        public MatchMode Mode { get; private set; }
        public int CurrentIndex { get; private set; }
        public Turret CurrentTurret => turrets[CurrentIndex];
        public IReadOnlyList<Turret> Turrets => turrets;
        public Mountain Mountain => mountain;
        public Turret Winner { get; private set; }
        public bool ShotActive => activeShell != null;
        public bool IsPaused { get; private set; }
        public bool EasyMode { get; private set; }
        public string LastEvent { get; private set; } = "Shoot the mountain to collect ore.";
        internal Turret LastEventSource { get; private set; }
        public bool HasTurn => Phase == GamePhase.Mining || Phase == GamePhase.Combat;
        public bool CanAct => isActiveAndEnabled && HasTurn && !IsPaused && !ShotActive;
        public bool IsCpuTurn => HasTurn && IsCpu(CurrentTurret);
        public bool CanHumanAct => CanAct && !IsCpuTurn;
        public event Action<Vector2> OreImpact;

        private const string EasyModePreference = "RockAndHardPlace.EasyMode";
        private static MatchMode? modeOnReload;
        private Projectile activeShell;
        private CastleOpponent opponent;
        private int ignoreInputFrame = -1;
        public bool AcceptsKeyboard => Time.frameCount != ignoreInputFrame;

        private void Awake()
        {
            if (turrets == null || turrets.Length < 2 || mountain == null)
            {
                Debug.LogError("GameManager needs at least two Turrets and the Mountain assigned.", this);
                enabled = false;
            }
            Phase = startFromMenu && !modeOnReload.HasValue ? GamePhase.Title : GamePhase.Mining;
            Mode = modeOnReload ?? MatchMode.TwoPlayer;
            modeOnReload = null;
            EasyMode = PlayerPrefs.GetInt(EasyModePreference, 0) != 0;
        }

        private void Start()
        {
            if (Phase == GamePhase.Title) return;
            StartMatch(Mode);
        }

        public void BeginMatch(MatchMode mode = MatchMode.TwoPlayer)
        {
            if (Phase != GamePhase.Title) return;
            IgnoreKeyboardThisFrame();
            StartMatch(mode);
        }

        private void StartMatch(MatchMode mode)
        {
            Mode = mode;
            if (Mode == MatchMode.SinglePlayer) opponent = new CastleOpponent(this);
            SetPhase(GamePhase.Mining);
            onTurnStarted.Invoke(CurrentTurret);
        }

        private void Update()
        {
            if (CanAct && IsCpuTurn) opponent.Tick(Time.deltaTime);
        }

        internal bool IsCpu(Turret turret) => Mode == MatchMode.SinglePlayer && turret == turrets[1];

        internal int HitDamage(Turret turret)
        {
            int damage = turret.Damage;
            return IsCpu(turret) && damage > 0 ? Mathf.Max(1, Mathf.RoundToInt(damage * cpuDamageMultiplier)) : damage;
        }

        public void SetEasyMode(bool enabled)
        {
            EasyMode = enabled;
            PlayerPrefs.SetInt(EasyModePreference, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            IgnoreKeyboardThisFrame();
        }

        public void IgnoreKeyboardThisFrame() => ignoreInputFrame = Time.frameCount;

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1;
        }

        // Inputs are -1 to 1; InputReader calls this every frame.
        public void Aim(float angleInput, float powerInput)
        {
            if (!CanHumanAct) return;
            CurrentTurret.AdjustAim(angleInput * angleSpeed * Time.deltaTime, powerInput * powerSpeed * Time.deltaTime);
        }

        public bool TryFire() => CanHumanAct && FireCurrentTurret();

        internal bool TryFireCpu() => CanAct && IsCpuTurn && FireCurrentTurret();

        private bool FireCurrentTurret()
        {
            var shell = CurrentTurret.Fire();
            if (shell == null) return false;
            activeShell = shell;
            shell.Resolved += OnShotResolved;
            onShotFired.Invoke(CurrentTurret);
            GameAudio.Play(SoundCue.Cannon);
            return true;
        }

        public bool TryBuyUpgrade() => TryBuyUpgrade(UpgradeId.ForgedShells);

        public bool TryBuyUpgrade(UpgradeId id) => CanHumanAct && BuyForCurrentTurret(id);

        internal bool TryBuyCpuUpgrade(UpgradeId id) => CanAct && IsCpuTurn && BuyForCurrentTurret(id);

        private bool BuyForCurrentTurret(UpgradeId id)
        {
            if ((uint)id >= CastleUpgrades.Nodes.Count) return false;
            var turret = CurrentTurret;
            var upgrade = CastleUpgrades.Get(id);
            if (turret.Weapon == null || CastleUpgrades.StateFor(upgrade, turret) != UpgradeState.Available ||
                !turret.Bank.TryUnlock(id, upgrade.Cost(turret.Weapon))) return false;
            if (upgrade.HealthBonus > 0) turret.Health.IncreaseMaxHealth(upgrade.HealthBonus);
            SetLastEvent(turret.DisplayName + " unlocked " + upgrade.Name + " (" + upgrade.Effect(turret.Weapon) + ").", turret);
            GameAudio.Play(SoundCue.Upgrade);
            return true;
        }

        public void Restart() => Reload(Mode);
        public void ReturnToTitle() => Reload(null);

        private void Reload(MatchMode? mode)
        {
            SetPaused(false);
            modeOnReload = mode;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Hook this to Mountain > On Left in the Inspector.
        public void OnMountainLeft()
        {
            if (Phase != GamePhase.MountainLeaving) return;
            SetPhase(GamePhase.Combat);
            SetLastEvent("Mountain cleared. Direct hits now damage kingdoms.");
            NextTurn();
        }

        private void OnShotResolved(Projectile shell, Collider2D hit)
        {
            shell.Resolved -= OnShotResolved;
            activeShell = null;
            var shooter = shell.Shooter;
            SetLastEvent(shooter.DisplayName + " missed.", shooter);

            if (Phase == GamePhase.Mining && mountain.Owns(hit))
            {
                int mined = mountain.Mine(shooter.Bank);
                if (mined > 0)
                {
                    GameAudio.Play(SoundCue.StoneHit);
                    OreImpact?.Invoke(shell.ImpactPoint);
                }

                string lootMessage = "";
                if (mined > 0 && lootManager != null)
                    lootMessage = lootManager.AwardLoot(shooter);

                string message = shooter.DisplayName + " mined " + mined + " ore.";
                if (!string.IsNullOrEmpty(lootMessage))
                    message += " " + lootMessage;
                SetLastEvent(message, shooter);

                if (mountain.IsDepleted)
                {
                    SetPhase(GamePhase.MountainLeaving);
                    SetLastEvent("The mountain is getting up... " + lootMessage);
                    return;
                }
            }
            else if (Phase == GamePhase.Combat && hit != null)
            {
                var target = hit.GetComponentInParent<Turret>();
                if (target != null && target != shooter)
                {
                    int damage = HitDamage(shooter);
                    target.Health.TakeDamage(damage);
                    GameAudio.Play(SoundCue.KingdomHit);
                    SetLastEvent(shooter.DisplayName + " hit " + target.DisplayName + " for " + damage + ".", shooter);
                    if (target.Health.IsDead)
                    {
                        EndGame(shooter);
                        return;
                    }
                }
                else GameAudio.Play(SoundCue.StoneHit, 0.45f);
            }
            else if (hit != null) GameAudio.Play(SoundCue.StoneHit, 0.45f);
            NextTurn();
        }

        private void NextTurn()
        {
            CurrentIndex = (CurrentIndex + 1) % turrets.Length;
            opponent?.ResetTurn();
            onTurnStarted.Invoke(CurrentTurret);
        }

        private void EndGame(Turret winner)
        {
            Winner = winner;
            SetPhase(GamePhase.GameOver);
            SetLastEvent(winner.DisplayName + " wins! Press Restart to play again.", winner);
            onGameOver.Invoke(winner);
            GameAudio.Play(SoundCue.Victory);
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            onPhaseChanged.Invoke(phase);
        }

        private void SetLastEvent(string message, Turret source = null)
        {
            LastEvent = message;
            LastEventSource = source;
        }
    }
}
