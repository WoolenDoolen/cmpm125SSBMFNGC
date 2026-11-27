#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RockAndHardPlace
{
    // Opt-in integration check: actual projectiles, collisions, escape timing, and match flow.
    // Run with -foundationSmokeTest; it never installs in a normal game session.
    public sealed class FoundationSmokeCheck : MonoBehaviour
    {
        private GameManager game;
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
            Check(game != null && game.Phase == GamePhase.Mining, "The match starts in Mining.");
            var mountain = game.Mountain;
            var escape = mountain.GetComponent<MountainEscape>();
            Check(escape != null && mountain.GetComponentInChildren<TilemapCollider2D>() != null,
                "The tilemap mountain has its escape controller and collision.");
            CheckPoseContinuity();
            escape.Footstep += CountStep;
            yield return Capture("01-mining");

            int miningTurns = Mathf.CeilToInt((float)mountain.StartingOre / mountain.OrePerHit);
            for (int hit = 0; hit < miningTurns; hit++)
            {
                int before = mountain.OreRemaining;
                var shooter = game.CurrentTurret;
                if (hit == 2)
                {
                    Check(game.TryBuyUpgrade(), "Mined ore buys an upgrade through GameManager.");
                    int ore = shooter.Bank.Ore;
                    Check(!game.TryBuyUpgrade() && shooter.Bank.Ore == ore, "Repeat upgrades do not spend ore.");
                }
                Check(FindShot(mountain.Owns), "A ballistic shot can reach the mountain.");
                Check(game.TryFire(), "The current kingdom fires.");
                Check(!game.TryFire(), "A second shot is blocked while a shell is in flight.");
                yield return WaitForShot();
                Check(mountain.OreRemaining == Mathf.Max(0, before - mountain.OrePerHit), "A real projectile mines exactly one reward.");
            }
            Check(game.Phase == GamePhase.MountainLeaving, "Depletion starts the escape phase.");
            Check(!game.TryFire() && !game.TryBuyUpgrade(), "Combat and purchases pause during the escape.");
            foreach (var part in mountain.GetComponentsInChildren<Collider2D>())
                Check(!part.enabled, "Depleted mountain colliders switch off immediately.");
            int lastShooter = game.CurrentIndex;
            Vector3 origin = mountain.transform.localPosition;
            yield return new WaitForSeconds(0.25f);
            Check(Vector3.Distance(origin, mountain.transform.localPosition) < 0.01f, "The mountain wakes without teleporting.");
            yield return Capture("02-awake");
            yield return new WaitForSeconds(0.9f);
            yield return Capture("03-standing");
            yield return new WaitForSeconds(0.6f);
            yield return Capture("04-running");
            while (game.Phase == GamePhase.MountainLeaving) yield return null;
            Check(game.Phase == GamePhase.Combat && mountain.HasLeft && !mountain.gameObject.activeSelf,
                "The escape finishes once and releases Combat.");
            Check(game.CurrentIndex == (lastShooter + 1) % game.Turrets.Count && steps >= 6,
                "The run has alternating footfalls and passes exactly one turn.");
            yield return Capture("05-combat");

            for (int shot = 0; game.Phase != GamePhase.GameOver && shot < 20; shot++)
            {
                var target = game.Turrets[(game.CurrentIndex + 1) % game.Turrets.Count];
                int health = target.Health.Current;
                Check(FindShot(c => c.GetComponentInParent<Turret>() == target), "A ballistic shot reaches the opposing kingdom.");
                Check(game.TryFire(), "Combat accepts the next shot.");
                yield return WaitForShot();
                Check(target.Health.Current < health, "A real kingdom impact reduces health. " + game.LastEvent);
            }
            Check(game.Phase == GamePhase.GameOver && game.Winner != null && !game.TryFire(),
                "A destroyed kingdom ends the match and stops firing.");
            yield return Capture("06-game-over");
            var oldGame = game;
            game.Restart();
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game != oldGame && game.Phase == GamePhase.Mining && !game.Mountain.HasLeft,
                "Restart creates a fresh mining match.");
            Finish(0, "FOUNDATION CHECK PASSED: mining, upgrade, tile collisions, escape, footsteps, combat, win, restart.");
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
            float gravity = Physics2D.gravity.y * turret.Weapon.ShellPrefab.GetComponent<Rigidbody2D>().gravityScale;
            for (float angle = 55; angle <= 80; angle += 5)
            for (float power = 6; power <= 20; power += 0.25f)
            {
                turret.SetAim(angle, power);
                // AimDirection * the known barrel length gives the exact muzzle from its pivot.
                var settings = turret.GetComponentsInChildren<Transform>();
                Transform muzzle = null;
                foreach (var part in settings) if (part.name == "Muzzle") muzzle = part;
                if (muzzle == null) throw new Exception("Turret needs its named Muzzle for this check.");
                Vector2 start = muzzle.position;
                Vector2 velocity = turret.AimDirection * turret.Power;
                Vector2 previous = start;
                bool blocked = false;
                float dt = Time.fixedDeltaTime;
                for (float t = dt; t <= 6 && !blocked; t += dt)
                {
                    velocity += Vector2.up * (gravity * dt);
                    Vector2 next = previous + velocity * dt;
                    Vector2 segment = next - previous;
                    foreach (var hit in Physics2D.CircleCastAll(previous, 0.13f, segment.normalized, segment.magnitude))
                    {
                        if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(turret.transform)) continue;
                        if (target(hit.collider) && segment.y < 0 && hit.normal.y > 0.5f)
                        {
                            Debug.Log("FOUNDATION SHOT " + turret.DisplayName + " angle " + angle + " power " + power);
                            return true;
                        }
                        blocked = true;
                        break;
                    }
                    previous = next;
                }
            }
            return false;
        }

        private static void CheckPoseContinuity()
        {
            foreach (float boundary in new[] { MountainEscape.WakeDuration,
                MountainEscape.WakeDuration + MountainEscape.CrouchDuration, MountainEscape.RunStart })
            {
                var a = MountainEscape.Sample(boundary - 0.0001f, 24);
                var b = MountainEscape.Sample(boundary + 0.0001f, 24);
                Check(Vector3.Distance(a.BodyOffset, b.BodyOffset) < 0.01f &&
                    Vector3.Distance(a.BodyScale, b.BodyScale) < 0.01f, "Escape poses join without a snap.");
            }
            float previous = 0;
            for (int i = 0; i <= 500; i++)
            {
                var pose = MountainEscape.Sample(i * MountainEscape.Duration / 500, 24);
                Check(pose.Distance >= previous && pose.FrontFoot.y >= 0 && pose.BackFoot.y >= 0 && pose.BodyScale.y > 0,
                    "Run travel stays forward, feet stay above ground, and scale stays positive.");
                previous = pose.Distance;
            }
            Check(Mathf.Abs(previous - 24) < 0.01f, "The run covers its complete clearance distance.");
        }

        private static void Check(bool condition, string message)
        {
            if (condition) return;
            Finish(1, "FOUNDATION CHECK FAILED: " + message);
            throw new Exception(message);
        }

        private static void Finish(int code, string message)
        {
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
