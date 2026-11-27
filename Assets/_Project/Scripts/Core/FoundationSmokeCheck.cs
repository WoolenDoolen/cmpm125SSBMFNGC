#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace RockAndHardPlace
{
    // Opt-in integration check: actual projectiles, collisions, escape timing, and match flow.
    // Run with -foundationSmokeTest; it never installs in a normal game session.
    public sealed class FoundationSmokeCheck : MonoBehaviour
    {
        private GameManager game;
        private ShotTrajectory trajectory;
        private Keyboard keyboard;
        private static Action cleanup;
        private int steps;
        private float deadline;
        private static string Output => Path.Combine(Directory.GetCurrentDirectory(), "Logs", "FoundationCheck");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-foundationSmokeTest") < 0) return;
            new GameObject("Foundation Integration Check").AddComponent<FoundationSmokeCheck>();
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "result.txt"), "FOUNDATION CHECK RUNNING");
            deadline = Time.realtimeSinceStartup + 90;
            yield return null;
            yield return new WaitForFixedUpdate();
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Phase == GamePhase.Title && !game.TryFire(), "The title screen blocks gameplay.");
            bool savedEasyMode = game.EasyMode;
            var backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            cleanup = () =>
            {
                if (game != null && game.EasyMode != savedEasyMode) game.SetEasyMode(savedEasyMode);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
            };
            yield return Capture("00-title");
            Click("How to Play");
            Check(GameObject.Find("Upgrade guide heading") != null, "How to Play explains the upgrades.");
            yield return Capture("00-how-to-play");
            Click("Back");
            Click("Credits");
            Check(GameObject.Find("Project team") != null, "Credits opens from the start screen.");
            Click("Back");
            Click("Options");
            Click("EZ Mode Toggle");
            Check(game.EasyMode != savedEasyMode, "The EZ option toggles.");
            if (!game.EasyMode) Click("EZ Mode Toggle");
            yield return Capture("00-options");
            Click("Back");
            Click("Play local match");
            Check(game.Phase == GamePhase.Mining, "Play starts the mining phase.");
            trajectory = new ShotTrajectory();
            yield return CheckKeyboard();

            yield return Tap(Key.Escape);
            Check(game.IsPaused && !game.TryFire(), "Escape opens Pause and blocks firing.");
            Click("Options");
            Click("EZ Mode Toggle");
            Check(!game.EasyMode && game.IsPaused, "Options accessed from Pause keeps the game paused.");
            Click("Back");
            Click("Resume");
            yield return null;
            Check(!game.IsPaused && !Line("EZ trajectory").enabled, "EZ off hides the arc when the match resumes.");
            yield return Tap(Key.Escape);
            Click("Options");
            Click("EZ Mode Toggle");
            Click("Back");
            Click("Resume");
            yield return null;
            var mountain = game.Mountain;
            var escape = mountain.GetComponent<MountainEscape>();
            Check(escape != null && mountain.GetComponentInChildren<TilemapCollider2D>() != null,
                "The tilemap mountain has its escape controller and collision.");
            CheckPoseContinuity();
            escape.Footstep += CountStep;
            Check(Line("Blue turn outline").enabled && !Line("Red turn outline").enabled, "Only Blue is outlined at the start.");
            Check(Line("EZ trajectory").enabled && Line("Predicted impact").enabled, "EZ renders the arc and impact.");
            yield return Capture("01-mining");

            int miningTurns = Mathf.CeilToInt((float)mountain.StartingOre / mountain.OrePerHit);
            for (int hit = 0; hit < miningTurns; hit++)
            {
                int before = mountain.OreRemaining;
                var shooter = game.CurrentTurret;
                if (hit == 2)
                {
                    yield return Tap(Key.U);
                    int beforePurchase = shooter.Bank.Ore;
                    int damage = shooter.Damage;
                    Check(!Control("Heavy Shot").interactable && !game.TryBuyUpgrade(UpgradeId.HeavyShot) &&
                        shooter.Bank.Ore == beforePurchase, "The second cannon upgrade needs its first node.");
                    yield return Capture("01-upgrades");
                    Click("Forged Shells");
                    Check(shooter.Bank.HasUpgrade(UpgradeId.ForgedShells) && shooter.Damage ==
                        damage + shooter.Weapon.UpgradedDamage - shooter.Weapon.Damage, "Forged Shells increases damage.");
                    Check(!Control("Forged Shells").interactable, "A purchased upgrade cannot be bought again.");
                    Click("Back to aiming");
                }
                if (hit == 3)
                {
                    yield return Tap(Key.U);
                    int health = shooter.Health.Current;
                    int max = shooter.Health.Max;
                    Click("Reinforced Walls");
                    int bonus = CastleUpgrades.Get(UpgradeId.ReinforcedWalls).HealthBonus;
                    Check(shooter.Health.Current == health + bonus && shooter.Health.Max == max + bonus,
                        "Reinforced Walls adds current and maximum HP.");
                    Check(!shooter.Bank.HasUpgrade(UpgradeId.ForgedShells), "Purchases are unique to the acting castle.");
                    Click("Back to aiming");
                }
                Check(FindShot(mountain.Owns), "A ballistic shot can reach the mountain.");
                Check(game.TryFire(), "The current kingdom fires.");
                Check(!game.TryFire(), "A second shot is blocked while a shell is in flight.");
                yield return null;
                Check(!Line("EZ trajectory").enabled, "The EZ arc disappears while a shell is in flight.");
                yield return WaitForShot();
                Check(mountain.OreRemaining == Mathf.Max(0, before - mountain.OrePerHit), "A real projectile mines exactly one reward.");
                if (hit == 0)
                    Check(Line("Red turn outline").enabled && !Line("Blue turn outline").enabled, "The outline passes to Red.");
            }
            Check(game.Phase == GamePhase.MountainLeaving, "Depletion starts the escape phase.");
            Check(!game.TryFire() && !game.TryBuyUpgrade(), "Combat and purchases pause during the escape.");
            foreach (var part in mountain.GetComponentsInChildren<Collider2D>())
                Check(!part.enabled, "Depleted mountain colliders switch off immediately.");
            int lastShooter = game.CurrentIndex;
            Vector3 origin = mountain.transform.localPosition;
            float animationStart = Time.time;
            yield return new WaitForSeconds(0.25f);
            Check(Vector3.Distance(origin, mountain.transform.localPosition) < 0.01f, "The mountain wakes without teleporting.");
            yield return Capture("02-awake");
            yield return new WaitUntil(() => Time.time >= animationStart + MountainEscape.RiseEnd - 0.09f);
            yield return Capture("03-standing");
            Check(Vector3.Distance(origin, mountain.transform.localPosition) < 0.02f,
                "The mountain finishes its slow rise at its starting location.");
            yield return new WaitUntil(() => Time.time >= animationStart + MountainEscape.RunStart + 0.40f);
            Check(mountain.transform.localPosition.z > origin.z + 2 && mountain.transform.localScale.x < 0.6f,
                "The sprint recedes into the valley quickly.");
            yield return Capture("04-running");
            while (game.Phase == GamePhase.MountainLeaving) yield return null;
            Check(game.Phase == GamePhase.Combat && mountain.HasLeft && !mountain.gameObject.activeSelf,
                "The escape finishes once and releases Combat.");
            Check(game.CurrentIndex == (lastShooter + 1) % game.Turrets.Count && steps >= 6,
                "The run has alternating footfalls and passes exactly one turn.");
            yield return Capture("05-combat");
            var upgraded = game.CurrentTurret;
            int baseDamage = upgraded.Damage;
            yield return Tap(Key.U);
            Click("Heavy Shot");
            Check(upgraded.Damage == baseDamage + CastleUpgrades.HeavyShotDamage, "An unlocked second tier applies its damage bonus.");
            Click("Back to aiming");

            for (int shot = 0; game.Phase != GamePhase.GameOver && shot < 20; shot++)
            {
                var target = game.Turrets[(game.CurrentIndex + 1) % game.Turrets.Count];
                int health = target.Health.Current;
                Check(FindShot(c => c.GetComponentInParent<Turret>() == target), "A ballistic shot reaches the opposing kingdom.");
                Check(game.TryFire(), "Combat accepts the next shot.");
                yield return WaitForShot();
                Check(target.Health.Current < health, "A real kingdom impact reduces health. " + game.LastEvent);
                if (shot == 0) yield return Capture("05-damage");
            }
            Check(game.Phase == GamePhase.GameOver && game.Winner != null && !game.TryFire(),
                "A destroyed kingdom ends the match and stops firing.");
            yield return Capture("06-game-over");
            var oldGame = game;
            Click("Rematch");
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game != oldGame && game.Phase == GamePhase.Mining && !game.Mountain.HasLeft && game.EasyMode &&
                game.Turrets[0].Bank.Ore == 0 && !game.Turrets[0].Bank.Upgraded,
                "Rematch resets the battlefield and upgrades while keeping the EZ setting.");
            yield return Tap(Key.Escape);
            Click("Main Menu");
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Phase == GamePhase.Title, "Main Menu returns to the title screen.");
            Finish(0, "FOUNDATION CHECK PASSED: menus, WASD, EZ arc, both upgrade branches, castle turns, mining, sprint, combat, win, rematch.");
        }

        private IEnumerator CheckKeyboard()
        {
            var turret = game.CurrentTurret;
            float angle = turret.Angle;
            float power = turret.Power;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return null;
            yield return null;
            Check(turret.Angle > angle && turret.Power > power,
                $"W raises aim and D raises power. Aim {turret.Angle}, power {turret.Power}, W pressed {keyboard.wKey.isPressed}.");
            angle = turret.Angle;
            power = turret.Power;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.A));
            yield return null;
            yield return null;
            Check(turret.Angle < angle && turret.Power < power, "S lowers aim and A lowers power.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            turret.SetAim(40, 8);
        }

        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static Button Control(string name)
        {
            var widget = GameObject.Find(name);
            Check(widget != null && widget.GetComponent<Button>() != null, "Expected visible button " + name + ".");
            return widget.GetComponent<Button>();
        }

        private static void Click(string name)
        {
            var button = Control(name);
            Check(button.interactable, name + " should be usable.");
            button.onClick.Invoke();
        }

        private static LineRenderer Line(string name)
        {
            var item = GameObject.Find(name);
            Check(item != null && item.GetComponent<LineRenderer>() != null, "Expected renderer " + name + ".");
            return item.GetComponent<LineRenderer>();
        }

        private void Update()
        {
            if (deadline > 0 && Time.realtimeSinceStartup > deadline)
                Finish(1, "FOUNDATION CHECK FAILED: timed out.");
        }

        private IEnumerator WaitForShot()
        {
            while (game.ShotActive) yield return null;
            yield return null;
        }

        private void CountStep(Vector3 _) => steps++;

        private static IEnumerator Capture(string name)
        {
            yield return null;
            // Explicit camera rendering also works when batch mode has no active Game view.
            var camera = Camera.main;
            var target = new RenderTexture(1600, 900, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            camera.targetTexture = target;
            // Overlay canvases normally bypass Camera.Render. Temporarily route them through
            // the same camera so the review captures the real HUD and reward layout as well.
            var canvases = FindObjectsByType<Canvas>();
            var overlay = new System.Collections.Generic.List<(Canvas canvas, bool sorting, int order)>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                overlay.Add((canvas, canvas.overrideSorting, canvas.sortingOrder));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.overrideSorting = true;
                canvas.sortingOrder = short.MaxValue;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
            camera.targetTexture = previous;
            RenderTexture.active = active;
            foreach (var saved in overlay)
            {
                var canvas = saved.canvas;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.overrideSorting = saved.sorting;
                canvas.sortingOrder = saved.order;
            }
            Destroy(image);
            Destroy(target);
        }

        // Search using the same gravity and muzzle positions as the live projectile.
        // This avoids a brittle test tied to one particular hill height or reward roll.
        private bool FindShot(Func<Collider2D, bool> target)
        {
            var turret = game.CurrentTurret;
            for (float angle = 55; angle <= 80; angle += 5)
            for (float power = 6; power <= 20; power += 0.25f)
            {
                turret.SetAim(angle, power);
                var hit = trajectory.Trace(turret);
                if (hit.collider != null && hit.normal.y > 0.5f && target(hit.collider)) return true;
            }
            return false;
        }

        private static void CheckPoseContinuity()
        {
            var destination = new Vector3(2.05f, 4.78f, 10);
            foreach (float boundary in new[] { MountainEscape.WakeDuration,
                MountainEscape.WakeDuration + MountainEscape.CrouchDuration, MountainEscape.RiseEnd, MountainEscape.RunStart })
            {
                var a = MountainEscape.Sample(boundary - 0.0001f, destination);
                var b = MountainEscape.Sample(boundary + 0.0001f, destination);
                Check(Vector3.Distance(a.BodyOffset, b.BodyOffset) < 0.01f &&
                    Vector3.Distance(a.RootOffset, b.RootOffset) < 0.01f &&
                    Vector3.Distance(a.BodyScale, b.BodyScale) < 0.01f, "Escape poses join without a snap.");
            }
            var end = MountainEscape.Sample(MountainEscape.Duration, destination);
            Check(Vector3.Distance(end.RootOffset, destination) < 0.01f && end.FigureScale < 0.04f,
                "The mountain reaches the river bend at a distant scale.");
        }

        private static void Check(bool condition, string message)
        {
            if (condition) return;
            Finish(1, "FOUNDATION CHECK FAILED: " + message);
            throw new Exception(message);
        }

        private static void Finish(int code, string message)
        {
            cleanup?.Invoke();
            cleanup = null;
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "result.txt"), message);
            Debug.Log(message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
#endif
