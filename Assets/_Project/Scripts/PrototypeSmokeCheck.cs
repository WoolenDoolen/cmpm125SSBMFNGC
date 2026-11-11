#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RockAndHardPlace
{
    // Local verification only; this is not part of the gameplay flow.
    public sealed class PrototypeSmokeCheck : MonoBehaviour
    {
        private IEnumerator checks;
        private float deadline;
        private bool capture;
        private string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BeginIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-prototypeSmokeTest") < 0) return;
            new GameObject("Prototype check").AddComponent<PrototypeSmokeCheck>();
        }

        private void Start()
        {
            capture = Array.IndexOf(Environment.GetCommandLineArgs(), "-prototypeCapture") >= 0;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Checks"));
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            Time.timeScale = capture ? 1 : 4;
            deadline = Time.realtimeSinceStartup + 60;
            checks = RunChecks();
        }

        private void Update()
        {
            if (checks == null) return;
            try
            {
                Require(Time.realtimeSinceStartup < deadline, "Timed out");
                if (checks.MoveNext()) return;
                Debug.Log("STARTING LAYOUT CHECK PASSED: shots, turns, mining, one upgrade, mountain clearance, direct damage.");
                File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: starting layout\n");
                checks = null;
                Application.Quit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL\n" + error);
                checks = null;
                Application.Quit(1);
            }
        }

        private IEnumerator RunChecks()
        {
            yield return null;
            var game = FindAnyObjectByType<PrototypeGame>();
            Require(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset,
                "The starting layout must use URP");
            Require(game != null && game.tanks.Length == 2 && game.mountain != null, "Scene references");
            Require(!game.BuyUpgrade(), "Cannot buy with no ore");
            float wait = Time.time + 0.5f;
            while (Time.time < wait) yield return null;
            if (capture) ScreenCapture.CaptureScreenshot(Path.Combine(output, "starting-layout.png"));
            wait = Time.time + 0.5f;
            while (Time.time < wait) yield return null;

            game.tanks[0].SetAim(85, 5);
            Require(game.Fire() && !game.Fire(), "Single active shell");
            while (game.shotActive) yield return null;
            Require(game.currentPlayer == 1 && game.mountainOre == 80, "A miss passes the turn without ore");
            game.tanks[0].SetAim(40, 14);

            for (int i = 0; i < 4; i++)
            {
                int player = game.currentPlayer;
                int oreBefore = game.tanks[player].ore;
                Require(game.Fire(), "Mining shot");
                while (game.shotActive || game.phase == PrototypePhase.MovingMountain) yield return null;
                Require(game.tanks[player].ore == oreBefore + 20, "Mining earns ore");
                Require(game.currentPlayer == 1 - player, "Turns alternate");
                if (i == 1)
                {
                    Require(game.BuyUpgrade(), "Test upgrade purchase");
                    Require(game.tanks[1].ore == 0 && game.tanks[1].Damage == 30, "Upgrade spends ore and changes damage");
                }
            }
            Require(game.phase == PrototypePhase.Combat && !game.mountain.gameObject.activeSelf,
                "Empty mountain clears the field");
            Require(!game.mountain.GetComponent<Collider2D>().enabled, "Mountain collider switches off");
            game.tanks[game.currentPlayer].SetAim(40, 14.5f);
            var shooter = game.tanks[game.currentPlayer];
            var target = game.tanks[1 - game.currentPlayer];
            Require(game.Fire(), "Combat shot");
            while (game.shotActive) yield return null;
            Require(target.health == 100 - shooter.Damage, "A direct hit reduces health");
            if (capture) ScreenCapture.CaptureScreenshot(Path.Combine(output, "combat-test.png"));
            wait = Time.time + 0.5f;
            while (Time.time < wait) yield return null;
        }

        private static void Require(bool passed, string message)
        {
            if (!passed) throw new Exception("Starting layout check: " + message);
        }
    }
}
#endif
