using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RockAndHardPlace;

public static class FinalProjectAudio
{
    private static readonly string[] Clips =
        { "Cannon", "StoneHit", "KingdomHit", "Footstep", "MountainWake", "Upgrade", "Victory" };

    [MenuItem("Prototype/Connect Basic Sound Effects")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
        var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (game == null) throw new Exception("Main needs its GameManager.");
        var audio = game.GetComponent<GameAudio>();
        if (audio == null) audio = game.gameObject.AddComponent<GameAudio>();
        var settings = new SerializedObject(audio);
        foreach (string name in Clips)
        {
            string path = "Assets/_Project/Audio/Sfx/" + name + ".wav";
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new Exception("Generate the sound effects first: " + path);
            importer.forceToMono = true;
            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.preloadAudioData = true;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.length <= 0 || clip.channels != 1)
                throw new Exception("Invalid mono sound effect: " + path);
            string field = char.ToLowerInvariant(name[0]) + name.Substring(1);
            settings.FindProperty(field).objectReferenceValue = clip;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        var source = audio.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("BASIC SOUND EFFECTS CONNECTED: seven valid mono clips.");
    }
}
