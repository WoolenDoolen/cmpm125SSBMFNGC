using System;
using System.IO;
using RockAndHardPlace;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeBuild
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

    [MenuItem("Prototype/Build Windows Prototype")]
    public static void BuildWindows()
    {
        UrpSetup.Validate();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindAnyObjectByType<GameManager>() == null)
            throw new Exception("Main.unity needs a GameManager.");
        string path = "Builds/Windows/BetweenARock.exe";
        string[] args = Environment.GetCommandLineArgs();
        int outputArg = Array.IndexOf(args, "-rockBuildOutput");
        if (outputArg >= 0 && outputArg + 1 < args.Length) path = args[outputArg + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Prototype build failed.");
        Debug.Log("PROTOTYPE BUILD PASSED");
    }
}
