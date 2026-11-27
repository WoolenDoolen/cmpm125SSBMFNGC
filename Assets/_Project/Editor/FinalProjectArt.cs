using System;
using System.IO;
using RockAndHardPlace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// Bakes ordinary, paintable Tilemaps into Main. No scene geometry is generated at runtime.
public static class FinalProjectArt
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string TilesFolder = "Assets/_Project/Art/Tiles";
    private const string GroundFolder = "Assets/Cainos/Pixel Art Platformer - Village Props/Tileset Palette/TP Ground";
    private static Material spriteMaterial;

    [MenuItem("Prototype/Rebuild Mountain and Kingdom Tilemaps")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory(TilesFolder);
        AssetDatabase.Refresh();
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Prototype/SpriteUnlit.mat");
        if (spriteMaterial == null) throw new Exception("Configure the project's URP sprite material first.");

        var mountain = Object.FindAnyObjectByType<Mountain>();
        BuildMountain(mountain);
        foreach (var turret in Object.FindObjectsByType<Turret>()) BuildKingdom(turret);
        foreach (var map in Object.FindObjectsByType<Tilemap>())
            if (map.name == "Tilemap" && map.GetComponent<TilemapCollider2D>() == null)
                map.gameObject.AddComponent<TilemapCollider2D>();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture();
        Debug.Log("FINAL PROJECT TILEMAPS BAKED");
    }

    private static void BuildMountain(Mountain mountain)
    {
        if (mountain == null) throw new Exception("Main needs its Mountain component.");
        RemoveChild(mountain.transform, "Mountain Art");
        var legacySprite = mountain.GetComponent<SpriteRenderer>();
        if (legacySprite != null) Object.DestroyImmediate(legacySprite);
        foreach (var collider in mountain.GetComponents<Collider2D>()) Object.DestroyImmediate(collider);

        // A ground-level pivot makes both scene placement and the later walk cycle predictable.
        mountain.transform.position = new Vector3(0, -3, 0);
        mountain.transform.localScale = Vector3.one;
        var art = Child(mountain.transform, "Mountain Art");
        var body = Child(art, "Body");
        body.gameObject.AddComponent<Grid>();
        var core = Map(body, "Mountain Core", Vector3.zero, 1, 1);
        var stone = Map(body, "Mountain Stone", Vector3.zero, 1, 2, true);
        int[] left = { -5, -5, -4, -3, -3, -2, -1 };
        int[] right = { 4, 4, 3, 3, 2, 1, 0 };
        int[] fill = { 97, 115, 116 };
        for (int y = 0; y < left.Length; y++)
        for (int x = left[y]; x <= right[y]; x++)
        {
            int id = fill[(x * x + y * 7) % fill.Length];
            bool top = y == left.Length - 1 || x < left[y + 1] || x > right[y + 1];
            if (top) id = x == left[y] ? 0 : x == right[y] ? 2 : 1;
            else if (y == 0) id = x == left[y] ? 6 : x == right[y] ? 8 : 7;
            Paint(core, x, y, Rock(4), StoneColor(x, y));
            Paint(stone, x, y, Rock(id), StoneColor(x, y));
        }
        var details = Map(body, "Ore Seams", new Vector3(0, 0, -0.02f), 0.125f, 3);
        var gold = new Color(0.91f, 0.67f, 0.30f);
        Diamond(details, -23, 15, gold);
        Diamond(details, 12, 28, gold);
        Diamond(details, -9, 43, gold);
        var sleep = Map(body, "Resting Face", new Vector3(0, 0, -0.03f), 0.125f, 4);
        var dark = new Color(0.16f, 0.18f, 0.16f);
        Rect(sleep, -16, 25, 7, 2, dark);
        Rect(sleep, 2, 25, 7, 2, dark);
        Rect(sleep, -6, 19, 6, 1, dark);
        var awake = Map(body, "Awake Face", new Vector3(0, 0, -0.03f), 0.125f, 4);
        Rect(awake, -16, 23, 7, 6, dark);
        Rect(awake, 2, 23, 7, 6, dark);
        Rect(awake, -15, 24, 5, 4, new Color(0.98f, 0.91f, 0.70f));
        Rect(awake, 3, 24, 5, 4, new Color(0.98f, 0.91f, 0.70f));
        Rect(awake, -12, 24, 2, 4, dark);
        Rect(awake, 6, 24, 2, 4, dark);
        Rect(awake, -6, 17, 5, 3, dark);
        awake.gameObject.SetActive(false);

        // Feet are tucked into the ground until the escape begins; each is its own Tilemap.
        BuildLeg(art, "Back Leg", -2.3f, 0, new Color(0.61f, 0.63f, 0.57f));
        BuildLeg(art, "Front Leg", 1.8f, 5, new Color(0.91f, 0.91f, 0.79f));
        var escape = mountain.GetComponent<MountainEscape>();
        if (escape == null) escape = mountain.gameObject.AddComponent<MountainEscape>();
        var settings = new SerializedObject(escape);
        settings.FindProperty("body").objectReferenceValue = body;
        settings.FindProperty("frontLeg").objectReferenceValue = art.Find("Front Leg");
        settings.FindProperty("backLeg").objectReferenceValue = art.Find("Back Leg");
        settings.FindProperty("restingFace").objectReferenceValue = sleep.gameObject;
        settings.FindProperty("awakeFace").objectReferenceValue = awake.gameObject;
        settings.ApplyModifiedPropertiesWithoutUndo();
        settings = new SerializedObject(mountain);
        settings.FindProperty("escape").objectReferenceValue = escape;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var legacyAnimator = mountain.GetComponent<Animator>();
        if (legacyAnimator != null) Object.DestroyImmediate(legacyAnimator);
    }

    private static void BuildLeg(Transform root, string name, float x, int order, Color tint)
    {
        var leg = Child(root, name);
        leg.localPosition = new Vector3(x, -0.15f, 0);
        leg.gameObject.AddComponent<Grid>();
        var map = Map(leg, "Boulder Foot", Vector3.zero, 0.5f, order);
        Paint(map, -1, -1, Rock(115), tint);
        Paint(map, 0, -1, Rock(116), tint);
        Paint(map, -1, -2, Rock(6), tint);
        Paint(map, 0, -2, Rock(7), tint);
        Paint(map, 1, -2, Rock(8), tint);
        leg.gameObject.SetActive(false);
    }

    private static void BuildKingdom(Turret turret)
    {
        RemoveChild(turret.transform, "Kingdom Tilemaps");
        var root = Child(turret.transform, "Kingdom Tilemaps");
        root.localPosition = new Vector3(0, -0.1f, 0);
        root.gameObject.AddComponent<Grid>();
        var core = Map(root, "Rampart Core", Vector3.zero, 0.5f, 0);
        var walls = Map(root, "Ramparts", Vector3.zero, 0.5f, 1, true);
        bool blue = turret.DisplayName == "Blue";
        var tint = blue ? new Color(0.72f, 0.80f, 0.85f) : new Color(0.87f, 0.75f, 0.67f);
        // Low curtain wall leaves the team's existing house and cannon visible.
        for (int x = -4; x <= 4; x++)
        for (int y = 0; y <= 1; y++) Paint(walls, x, y, Rock((x + y) % 2 == 0 ? 115 : 116), tint);
        foreach (int x in new[] { -4, -3, 3, 4 })
        {
            for (int y = 2; y <= 4; y++) Paint(walls, x, y, Rock(97), tint);
            if (x == -4 || x == 4) Paint(walls, x, 5, Rock(97), tint);
        }
        foreach (var cell in walls.cellBounds.allPositionsWithin)
            if (walls.HasTile(cell)) Paint(core, cell.x, cell.y, Rock(4), tint);
        var banners = Map(root, "Kingdom Banners", new Vector3(0, 0, -0.05f), 0.125f, 4);
        var flag = blue ? new Color(0.28f, 0.60f, 0.89f) : new Color(0.83f, 0.28f, 0.25f);
        Rect(banners, 11, 22, 1, 12, new Color(0.29f, 0.23f, 0.18f));
        Rect(banners, 12, 28, 7, 6, flag);
        Rect(banners, 13, 29, 1, 4, new Color(0.96f, 0.81f, 0.43f));
        Rect(banners, 15, 30, 3, 1, new Color(0.96f, 0.81f, 0.43f));

        // Keep the art on the actual aim pivot, so the visible barrel agrees with the shot.
        var turretData = new SerializedObject(turret);
        var pivot = (Transform)turretData.FindProperty("barrelPivot").objectReferenceValue;
        RemoveChild(pivot, "Cannon Tilemaps");
        var cannon = Child(pivot, "Cannon Tilemaps");
        cannon.gameObject.AddComponent<Grid>();
        var barrel = Map(cannon, "Iron Barrel", new Vector3(0, 0, -0.1f), 0.0625f, 6);
        Rect(barrel, -2, -3, 22, 6, new Color(0.19f, 0.22f, 0.22f));
        Rect(barrel, 0, 0, 18, 2, new Color(0.51f, 0.55f, 0.52f));
        Rect(barrel, 1, 2, 16, 1, new Color(0.70f, 0.70f, 0.59f));
        Rect(barrel, 17, -4, 3, 8, new Color(0.28f, 0.31f, 0.30f));
    }

    private static Color StoneColor(int x, int y)
    {
        float light = Mathf.Clamp01(0.80f + x * 0.025f + y * 0.015f);
        return new Color(light * 0.96f, light, light * 0.90f);
    }

    private static Tile Rock(int id)
    {
        string path = TilesFolder + "/TerrainStone" + id + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        var source = AssetDatabase.LoadAssetAtPath<Tile>(GroundFolder + "/TX Tileset Ground_" + id + ".asset");
        if (source == null || source.sprite == null) throw new Exception("Missing terrain tile " + id);
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = "Terrain Stone " + id;
        tile.sprite = source.sprite;
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    private static Tile Pixel()
    {
        string path = TilesFolder + "/DetailPixel.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = "Detail Pixel";
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, path);
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "White Pixel", filterMode = FilterMode.Point };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        AssetDatabase.AddObjectToAsset(texture, tile);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        sprite.name = "Detail Pixel Sprite";
        AssetDatabase.AddObjectToAsset(sprite, tile);
        tile.sprite = sprite;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Tilemap Map(Transform parent, string name, Vector3 offset, float scale, int order, bool collider = false)
    {
        var root = Child(parent, name);
        root.localPosition = offset;
        root.localScale = Vector3.one * scale;
        var map = root.gameObject.AddComponent<Tilemap>();
        var renderer = root.gameObject.AddComponent<TilemapRenderer>();
        renderer.sharedMaterial = spriteMaterial;
        renderer.sortingOrder = order;
        if (collider) root.gameObject.AddComponent<TilemapCollider2D>();
        return map;
    }

    private static void Paint(Tilemap map, int x, int y, TileBase tile, Color color)
    {
        var cell = new Vector3Int(x, y);
        map.SetTile(cell, tile);
        map.SetColor(cell, color);
    }

    private static void Rect(Tilemap map, int x, int y, int width, int height, Color color)
    {
        for (int dy = 0; dy < height; dy++)
        for (int dx = 0; dx < width; dx++) Paint(map, x + dx, y + dy, Pixel(), color);
    }

    private static void Diamond(Tilemap map, int x, int y, Color color)
    {
        Rect(map, x, y, 3, 1, color);
        Rect(map, x - 1, y + 1, 5, 2, color);
        Rect(map, x, y + 3, 3, 1, new Color(1, 0.88f, 0.53f));
    }

    private static void RemoveChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }

    public static void Capture()
    {
        Directory.CreateDirectory("Logs/ArtReview");
        var camera = Camera.main;
        if (camera == null) throw new Exception("Main needs a camera.");
        var target = new RenderTexture(1600, 900, 24);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var png = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        png.Apply();
        File.WriteAllBytes("Logs/ArtReview/tilemap-scene.png", png.EncodeToPNG());
        camera.targetTexture = previous;
        RenderTexture.active = active;
        Object.DestroyImmediate(png);
        Object.DestroyImmediate(target);
        foreach (var turret in Object.FindObjectsByType<Turret>())
            Debug.Log("KINGDOM " + turret.DisplayName + " root " + turret.transform.position);
    }
}
