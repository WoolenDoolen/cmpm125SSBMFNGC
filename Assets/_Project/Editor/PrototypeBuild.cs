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

    // Used once to save the editable starting layout. Normal builds keep scene edits.
    public static void CreateLayoutAndBuild()
    {
        CreateLayout();
        BuildWindows();
    }

    private static void CreateLayout()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 10.5f;
        camera.transform.position = new Vector3(0, 5.5f, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.2f, 0.22f);

        var blue = MaterialAsset("BlockoutBlue", new Color(0.2f, 0.4f, 0.8f));
        var red = MaterialAsset("BlockoutRed", new Color(0.8f, 0.25f, 0.2f));
        var gray = MaterialAsset("BlockoutGray", new Color(0.45f, 0.45f, 0.45f));
        var dark = MaterialAsset("BlockoutDark", new Color(0.3f, 0.3f, 0.3f));
        var yellow = MaterialAsset("BlockoutShell", Color.yellow);
        var ground = Block("Ground", null, new Vector3(0, -0.5f, 0), new Vector3(40, 1, 1), dark);
        ground.AddComponent<BoxCollider2D>();

        var game = new GameObject("Prototype Controller").AddComponent<PrototypeGame>();
        game.shellMaterial = yellow;
        game.tanks = new ArtilleryTank[2];
        for (int i = 0; i < 2; i++)
        {
            var root = new GameObject(i == 0 ? "Blue placeholder" : "Red placeholder");
            root.transform.position = new Vector3(i == 0 ? -12 : 12, 0, 0);
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
            tank.SetAim(40, 14);
        }

        var mountain = new GameObject("Mountain placeholder");
        var points = new[] { new Vector2(-4.5f, 0), new Vector2(0, 7), new Vector2(4.5f, 0) };
        var mesh = new Mesh { name = "Mountain triangle" };
        mesh.vertices = new[] { (Vector3)points[0], (Vector3)points[1], (Vector3)points[2] };
        mesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
        mesh.RecalculateBounds();
        const string meshPath = "Assets/_Project/Art/BlockoutMountain.asset";
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (savedMesh == null) AssetDatabase.CreateAsset(mesh, meshPath);
        else
        {
            EditorUtility.CopySerialized(mesh, savedMesh);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = savedMesh;
        }
        mountain.AddComponent<MeshFilter>().sharedMesh = mesh;
        mountain.AddComponent<MeshRenderer>().sharedMaterial = gray;
        mountain.AddComponent<PolygonCollider2D>().points = points;
        game.mountain = mountain.transform;
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
