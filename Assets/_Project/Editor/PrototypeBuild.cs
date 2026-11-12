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
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeGame>() == null)
            throw new Exception("Open the prototype scene first.");
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

    // Reuse the team's terrain when creating the playable test layout.
    public static void CreateLayoutAndBuild()
    {
        CreateLayout();
        BuildWindows();
    }

    private static void CreateLayout()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/TeamLayout.unity", OpenSceneMode.Single);
        if (Camera.main == null) throw new Exception("Team layout is missing its camera.");
        Camera.main.orthographicSize = 5.8f;
        Camera.main.transform.position = new Vector3(0, 0.8f, -10);

        var blue = MaterialAsset("BlockoutBlue", new Color(0.2f, 0.4f, 0.8f));
        var red = MaterialAsset("BlockoutRed", new Color(0.8f, 0.25f, 0.2f));
        var yellow = MaterialAsset("BlockoutShell", Color.yellow);

        var game = new GameObject("Prototype Controller").AddComponent<PrototypeGame>();
        game.shellMaterial = yellow;
        game.tanks = new ArtilleryTank[2];
        Physics2D.SyncTransforms();
        for (int i = 0; i < 2; i++)
        {
            var hill = GameObject.Find(i == 0 ? "LeftHill" : "RightHill");
            if (hill == null || hill.GetComponent<Collider2D>() == null)
                throw new Exception("Team layout is missing a hill collider.");
            var root = new GameObject(i == 0 ? "Blue placeholder" : "Red placeholder");
            root.transform.position = new Vector3(hill.transform.position.x, hill.GetComponent<Collider2D>().bounds.max.y, 0);
            var tank = root.AddComponent<ArtilleryTank>();
            game.tanks[i] = tank;
            tank.playerIndex = i;
            Block("Body", root.transform, new Vector3(0, 0.55f, 0), new Vector3(2, 1.1f, 0.5f), i == 0 ? blue : red);
            var hitbox = root.AddComponent<BoxCollider2D>();
            hitbox.offset = new Vector2(0, 0.7f);
            hitbox.size = new Vector2(2.3f, 1.4f);
            tank.barrelPivot = new GameObject("Barrel pivot").transform;
            tank.barrelPivot.SetParent(root.transform, false);
            tank.barrelPivot.localPosition = new Vector3(0, 1.25f, -0.5f);
            Block("Barrel", tank.barrelPivot, new Vector3(0.6f, 0, 0), new Vector3(1.3f, 0.22f, 0.2f), i == 0 ? blue : red);
            tank.SetAim(40, 8);
        }

        game.mountain = GameObject.Find("Mountain").transform;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/_Project/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }
}
