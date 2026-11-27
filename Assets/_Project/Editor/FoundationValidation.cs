using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FoundationValidation
{
    // Command line: -batchmode -executeMethod FoundationValidation.Run -foundationSmokeTest
    // Leave out -quit: the integration check exits the Editor after the gameplay checks finish.
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
        Debug.Log("FOUNDATION CHECK: entering Play mode.");
    }
}
