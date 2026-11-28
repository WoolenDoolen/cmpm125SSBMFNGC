#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.IO;
using TMPro;
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
            Canvas.ForceUpdateCanvases();
            var title = (RectTransform)Control("Single Player").transform.parent;
            var menu = (RectTransform)title.parent;
            Vector3 titleCenter = menu.InverseTransformPoint(title.TransformPoint(title.rect.center));
            Check(Vector2.Distance(titleCenter, menu.rect.center) < 0.5f,
                $"The title panel is centered at {Screen.width}x{Screen.height}.");
            Debug.Log($"TITLE LAYOUT CHECK PASSED at {Screen.width}x{Screen.height}.");
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
            Click("2 Player");
            Check(game.Mode == MatchMode.TwoPlayer && game.Phase == GamePhase.Mining, "2 Player starts a local match.");
            trajectory = new ShotTrajectory();
            yield return CheckKeyboard(useWasd: true);

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
            var mountainCollider = mountain.GetComponentInChildren<TilemapCollider2D>();
            Vector3 mountainScale = mountain.transform.localScale;
            Check(escape != null && mountainCollider != null,
                "The tilemap mountain has its escape controller and collision.");
            CheckPoseContinuity();
            escape.Footstep += CountStep;
            Check(Line("Blue turn outline").enabled && !Line("Red turn outline").enabled, "Only Blue is outlined at the start.");
            Check(Line("EZ trajectory").enabled && Line("Predicted impact").enabled, "EZ renders the arc and impact.");
            Check(!UpgradeReminderVisible(), "There is no upgrade reminder before either player earns ore.");
            yield return Capture("01-mining");

            int miningTurns = Mathf.CeilToInt((float)mountain.StartingOre / mountain.OrePerHit);
            for (int hit = 0; hit < miningTurns; hit++)
            {
                int before = mountain.OreRemaining;
                var shooter = game.CurrentTurret;
                if (hit == 2)
                {
                    Check(UpgradeReminderVisible(), "Blue gets a gold upgrade reminder when it can afford a node.");
                    var label = Control("Open upgrade tree").GetComponentInChildren<TMP_Text>();
                    Check(label.preferredWidth <= label.rectTransform.rect.width, "The upgrade reminder fits on one line.");
                    yield return new WaitForSecondsRealtime(0.12f);
                    yield return Capture("01-upgrade-reminder");
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
                    yield return null;
                    Check(UpgradeReminderVisible() == (shooter.Bank.Ore >= CastleUpgrades.Get(UpgradeId.HeavyShot).Cost(shooter.Weapon)),
                        "The reminder updates after spending ore.");
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
                {
                    Check(Line("Red turn outline").enabled && !Line("Blue turn outline").enabled, "The outline passes to Red.");
                    Check(!UpgradeReminderVisible(), "Blue's ore does not show a reminder on Red's turn.");
                    yield return CheckKeyboard(useWasd: false);
                }
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
                if (shot == 4)
                {
                    yield return CheckMountainReturn(origin, mountainScale, mountainCollider);
                    continue;
                }

                var target = game.Turrets[(game.CurrentIndex + 1) % game.Turrets.Count];
                var shooter = game.CurrentTurret;
                int health = target.Health.Current;
                int damage = shooter.Damage;
                Check(FindShot(c => c.GetComponentInParent<Turret>() == target), "A ballistic shot reaches the opposing kingdom.");
                Check(game.TryFire(), "Combat accepts the next shot.");
                yield return WaitForShot();
                Check(target.Health.Current == Mathf.Max(0, health - damage),
                    "Local opponents deal full damage on a real kingdom impact. " + game.LastEvent);
                if (shot < 4)
                    Check(game.Phase == GamePhase.Combat && !mountain.gameObject.activeSelf,
                        "The mountain stays away for the first four duel shots.");
                if (shot < 2)
                {
                    CheckHitBanner(shooter, target, damage);
                    yield return new WaitForSecondsRealtime(0.12f);
                    yield return Capture(shot == 0 ? "05-damage" : "05-red-hit");
                }
            }
            Check(game.Phase == GamePhase.GameOver && game.Winner != null && !game.TryFire(),
                "A destroyed kingdom ends the match and stops firing.");
            yield return Capture("06-game-over");
            var oldGame = game;
            Click("Rematch");
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game != oldGame && game.Mode == MatchMode.TwoPlayer && game.Phase == GamePhase.Mining &&
                !game.Mountain.HasLeft && game.EasyMode && game.Turrets[0].Bank.Ore == 0 && !game.Turrets[0].Bank.Upgraded,
                "Rematch resets the battlefield and upgrades while keeping the EZ setting.");
            yield return Tap(Key.Escape);
            Click("Main Menu");
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Phase == GamePhase.Title, "Main Menu returns to the title screen.");
            yield return CheckSinglePlayer();
            Finish(0, "FOUNDATION CHECK PASSED: Mountain returns as cover after five duel shots in both match modes. Mining, combat, pause and restart passed.");
        }

        private IEnumerator CheckMountainReturn(Vector3 origin, Vector3 originalScale, TilemapCollider2D mountainCollider)
        {
            var mountain = game.Mountain;
            int shooterIndex = game.CurrentIndex;
            int stepsBeforeReturn = steps;
            Check(FindShot(c => c.GetComponentInParent<Turret>() == null) && game.TryFire(), "The fifth duel shot misses the castle.");
            Check(game.Phase == GamePhase.Combat && mountain.HasLeft && !mountain.gameObject.activeSelf,
                "The return waits for the fifth shell to resolve.");
            yield return WaitForShot();
            Check(game.Phase == GamePhase.MountainReturning && mountain.gameObject.activeSelf &&
                game.LastEvent.EndsWith(" missed.", StringComparison.Ordinal), "A resolved miss counts as the fifth shot.");
            Check(game.CurrentIndex == shooterIndex && !mountainCollider.enabled && !game.TryFire() && !game.TryBuyUpgrade(),
                "Turns, firing, upgrades, and the mountain collider wait for the return to finish.");
            float startingScale = mountain.transform.localScale.x;
            float start = Time.time;
            yield return Tap(Key.Escape);
            Check(game.IsPaused, "The return can be paused.");
            Vector3 pausedPosition = mountain.transform.localPosition;
            yield return new WaitForSecondsRealtime(0.18f);
            Check(mountain.transform.localPosition == pausedPosition, "The paused mountain holds its pose.");
            Click("Resume");
            yield return new WaitUntil(() => Time.time >= start + MountainEscape.ReturnRunDuration * 0.76f);
            Check(mountain.transform.localPosition.z > origin.z && mountain.transform.localScale.x > startingScale &&
                mountain.transform.localScale.x < originalScale.x, "The mountain runs in from the distance and grows toward the foreground.");
            yield return Capture("05-mountain-returning");
            while (game.Phase == GamePhase.MountainReturning) yield return null;
            Check(game.Phase == GamePhase.Combat && game.CurrentIndex == (shooterIndex + 1) % game.Turrets.Count,
                "The return passes the turn exactly once.");
            Check(!mountain.HasLeft && mountainCollider.enabled && mountain.transform.localPosition == origin &&
                mountain.transform.localScale == originalScale && steps > stepsBeforeReturn + 4,
                "The running mountain reaches its original scale and position with its collider restored.");
            yield return Capture("05-mountain-cover");

            var shooter = game.CurrentTurret;
            int ore = shooter.Bank.Ore;
            Check(FindShot(mountain.Owns) && game.TryFire(), "A low shell can hit the returned mountain.");
            yield return WaitForShot();
            Check(game.Phase == GamePhase.Combat && shooter.Bank.Ore == ore && mountain.IsDepleted &&
                game.LastEvent == shooter.DisplayName + " hit the mountain.", "The returned mountain intercepts a real shot as cover.");
        }

        private IEnumerator CheckSinglePlayer()
        {
            Click("Single Player");
            Check(game.Mode == MatchMode.SinglePlayer && game.Phase == GamePhase.Mining && game.CanHumanAct,
                "Single Player starts with Blue in control.");
            trajectory = new ShotTrajectory();
            yield return CheckKeyboard(useWasd: true);
            yield return CheckKeyboard(useWasd: false);
            var mountain = game.Mountain;
            var cpu = game.Turrets[1];
            for (int round = 0; game.Phase == GamePhase.Mining && round < 4; round++)
            {
                Check(FindShot(mountain.Owns) && game.TryFire(), "Blue can mine in Single Player.");
                yield return WaitForShot();
                if (game.Phase == GamePhase.MountainLeaving) break;
                Check(game.IsCpuTurn, "The opponent gets the next turn.");
                if (round == 0)
                {
                    Check(!Line("EZ trajectory").enabled && !Control("Open upgrade tree").interactable,
                        "The arc and purchase button are unavailable during the CPU turn.");
                    Check(!UpgradeReminderVisible(), "There is no upgrade reminder during the CPU turn.");
                    Check(!game.TryFire() && !game.TryBuyUpgrade(), "The player cannot fire or purchase for the CPU.");
                    yield return Capture("07-solo-turn");
                    yield return Tap(Key.Escape);
                    Check(game.IsPaused, "The player can pause the CPU's turn.");
                    float angle = cpu.Angle;
                    yield return new WaitForSecondsRealtime(1.1f);
                    Check(game.IsCpuTurn && !game.ShotActive && cpu.Angle == angle, "The opponent waits while paused.");
                    Click("Resume");
                }
                int ore = mountain.OreRemaining;
                yield return WaitForCpuShot();
                Check(mountain.OreRemaining < ore, "The CPU collects ore with a real shot.");
            }
            Check(game.Phase == GamePhase.MountainLeaving && cpu.Bank.HasUpgrade(UpgradeId.ForgedShells),
                "The CPU spends its ore before the mountain leaves.");
            while (game.Phase == GamePhase.MountainLeaving) yield return null;
            Check(game.Phase == GamePhase.Combat, "Single Player continues into combat.");
            Check(game.CanHumanAct && FindShot(c => c.GetComponentInParent<Turret>() == cpu) && game.TryFire(),
                "Blue can open the duel with a hit.");
            yield return WaitForShot();
            var blue = game.Turrets[0];
            int health = blue.Health.Current;
            yield return WaitForCpuShot();
            int damage = health - blue.Health.Current;
            Check(damage > 0 && damage < cpu.Damage && damage == game.HitDamage(cpu),
                "The CPU hits Blue for the reduced damage shown in the HUD.");
            Check(3 * damage < health, "Blue can survive three hits at the CPU's opening damage.");
            var cpuStats = GameObject.Find("Red kingdom status").transform.Find("Ore and damage").GetComponent<TMP_Text>();
            Check(cpuStats.text.EndsWith("Damage " + damage, StringComparison.Ordinal), "Red's HUD displays its actual damage.");
            CheckHitBanner(cpu, blue, damage);
            Check(UpgradeReminderVisible(), "Blue gets the reminder again when its solo turn starts.");
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Capture("08-solo-hit-and-reminder");
            for (int round = 0; round < 2; round++)
            {
                Check(FindShot(c => c.GetComponentInParent<Turret>() == null) && game.TryFire(),
                    "Blue can fire the third and fifth solo duel shots.");
                yield return WaitForShot();
                if (round == 0) yield return WaitForCpuShot();
            }
            Check(game.Phase == GamePhase.MountainReturning && !game.CanAct, "CPU shots count toward the mountain's return.");
            while (game.Phase == GamePhase.MountainReturning) yield return null;
            Check(game.IsCpuTurn, "The CPU receives the next turn after the mountain returns.");
            health = blue.Health.Current;
            yield return WaitForCpuShot();
            Check(blue.Health.Current < health, "The CPU can lob a shot over the returned mountain.");
            yield return Tap(Key.R);
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Mode == MatchMode.SinglePlayer && game.Phase == GamePhase.Mining && game.CanHumanAct,
                "Restart preserves Single Player and returns control to Blue.");
        }

        private IEnumerator WaitForCpuShot()
        {
            while (game.IsCpuTurn && !game.ShotActive) yield return null;
            Check(game.ShotActive, "The CPU fires on its own.");
            yield return WaitForShot();
        }

        private IEnumerator CheckKeyboard(bool useWasd)
        {
            var turret = game.CurrentTurret;
            float angle = turret.Angle;
            float power = turret.Power;
            if (game.Mode == MatchMode.TwoPlayer)
            {
                yield return Tap(useWasd ? Key.UpArrow : Key.W, useWasd ? Key.RightArrow : Key.D);
                Check(turret.Angle == angle && turret.Power == power, turret.DisplayName + " ignores the other player's key set.");
            }
            yield return Tap(useWasd ? Key.W : Key.UpArrow, useWasd ? Key.D : Key.RightArrow);
            Check(turret.Angle > angle && turret.Power > power,
                turret.DisplayName + " can raise aim and power with their keys.");
            float raisedAngle = turret.Angle;
            float raisedPower = turret.Power;
            yield return Tap(useWasd ? Key.S : Key.DownArrow, useWasd ? Key.A : Key.LeftArrow);
            Check(turret.Angle < raisedAngle && turret.Power < raisedPower,
                turret.DisplayName + " can lower aim and power with their keys.");
            turret.SetAim(angle, power);
        }

        private IEnumerator Tap(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
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

        private static bool UpgradeReminderVisible()
        {
            var button = Control("Open upgrade tree");
            return button.interactable && button.colors.normalColor == MatchUi.Gold &&
                button.GetComponentInChildren<TMP_Text>().text.StartsWith("Upgrade ready", StringComparison.Ordinal);
        }

        private static void CheckHitBanner(Turret shooter, Turret target, int damage)
        {
            var banner = GameObject.Find("EventText").GetComponent<TMP_Text>();
            Check(banner.text == $"{shooter.DisplayName} hit {target.DisplayName} for {damage}." &&
                banner.color == MatchUi.Team(shooter), "The hit banner keeps the shooter's color after the turn changes.");
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
            var entry = MountainEscape.SampleReturn(0, destination);
            Check(Vector3.Distance(entry.RootOffset, end.RootOffset) < 0.01f && Mathf.Abs(entry.FigureScale - end.FigureScale) < 0.001f,
                "The mountain reappears where it left the valley.");
            var running = MountainEscape.SampleReturn(MountainEscape.ReturnRunDuration - 0.0001f, destination);
            var settling = MountainEscape.SampleReturn(MountainEscape.ReturnRunDuration + 0.0001f, destination);
            Check(Vector3.Distance(running.RootOffset, settling.RootOffset) < 0.01f &&
                Vector3.Distance(running.BodyOffset, settling.BodyOffset) < 0.01f, "The return joins the landing without a snap.");
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
