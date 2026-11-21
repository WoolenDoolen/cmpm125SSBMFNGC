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
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/BetweenARock.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Prototype build failed.");
        Debug.Log("PROTOTYPE BUILD PASSED");
    }
}
