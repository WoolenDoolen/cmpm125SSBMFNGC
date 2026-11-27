using System;
using System.Collections.Generic;
using System.IO;
using RockAndHardPlace;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Authored tile compositions. Source tiles are Kenney's unmodified CC0 pixel art.
// The outline, edge choice and shading are intentional; solid fill is never an edge tile.
public static class FinalProjectArt
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string SourceFolder = "Assets/_Project/Art/KenneyPixel";
    private const string TileFolder = "Assets/_Project/Art/ValleyTiles";
    private static readonly Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
    private static Material spriteMaterial;
    private static readonly int[] MountainHeights = { 1, 2, 4, 5, 7, 8, 10, 11, 13, 12, 11, 9, 8, 8, 7, 5, 4, 2, 1 };
    private static readonly Color Ink = Hex("303d45");
    private static readonly Color RockLight = new Color(0.87f, 0.91f, 0.92f);
    private static readonly Color RockShadow = new Color(0.60f, 0.69f, 0.75f);

    [MenuItem("Prototype/Rebuild Mountain and Kingdom Tilemaps")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory(TileFolder);
        AssetDatabase.Refresh();
        ConfigureTiles();
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Prototype/SpriteUnlit.mat");
        if (spriteMaterial == null) throw new Exception("Missing URP sprite material.");
        tiles.Clear();
        HideLegacyTerrain();
        BuildTerrain();
        BuildMountain(Object.FindAnyObjectByType<Mountain>());
        foreach (var turret in Object.FindObjectsByType<Turret>()) BuildKingdom(turret);
        ValleyBackdrop.Build();
        ConfigureCameraAndHud();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Capture();
        Debug.Log("VALLEY ART BAKED: proper slopes, stone mountain, tile castles, continuous terrain, layered backdrop.");
    }

    private static void ConfigureTiles()
    {
        foreach (string file in Directory.GetFiles(SourceFolder, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 21;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }

    private static void HideLegacyTerrain()
    {
        // Retain teammates' original layout objects for reference, but render one coherent style.
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "Grid" || root.name.StartsWith("PF Village Props") ||
                root.name == "LeftHill" || root.name == "RightHill" || root.name == "Ground")
                root.SetActive(false);
        var previous = GameObject.Find("Battlefield Terrain");
        if (previous != null) Object.DestroyImmediate(previous);
    }

    private static int GroundHeight(int x)
    {
        // Heights are tile boundaries, with a broad plateau under each castle and 45-degree slopes.
        int a = Mathf.Abs(x);
        if (a <= 6) return -6;
        if (a <= 12) return a - 12;
        if (a <= 23) return 0;
        return -1;
    }

    private static void BuildTerrain()
    {
        var root = Child(null, "Battlefield Terrain");
        root.gameObject.AddComponent<Grid>();
        var ground = Map(root, "Grass and Soil", Vector3.zero, 0.5f, 10, true);
        var soil = Map(root, "Soil Interior", Vector3.zero, 0.5f, 9, true);
        const string soilMaterialPath = TileFolder + "/Pixel Soil.mat";
        var soilMaterial = AssetDatabase.LoadAssetAtPath<Material>(soilMaterialPath);
        if (soilMaterial == null)
        {
            soilMaterial = new Material(Shader.Find("Universal Render Pipeline/RockAndHardPlace/Pixel Soil"));
            AssetDatabase.CreateAsset(soilMaterial, soilMaterialPath);
        }
        soil.GetComponent<TilemapRenderer>().sharedMaterial = soilMaterial;
        const string grassMaterialPath = TileFolder + "/Pixel Grass Edge.mat";
        var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath);
        if (grassMaterial == null)
        {
            grassMaterial = new Material(soilMaterial);
            AssetDatabase.CreateAsset(grassMaterial, grassMaterialPath);
        }
        grassMaterial.SetFloat("_GrassEdge", 1);
        EditorUtility.SetDirty(grassMaterial);
        ground.GetComponent<TilemapRenderer>().sharedMaterial = grassMaterial;
        var strata = Map(root, "Deep Stone", new Vector3(0, 0, -0.01f), 0.5f, 11);
        for (int x = -30; x <= 30; x++)
        {
            int top = GroundHeight(x);
            for (int y = -18; y < top; y++)
            {
                bool surface = y == top - 1;
                int c = 2, r = 5;
                if (surface)
                {
                    r = 4;
                    c = GroundHeight(x - 1) < top ? 6 : GroundHeight(x + 1) < top ? 6 : 2;
                }
                float depth = Mathf.InverseLerp(top - 1, -13, y);
                var tint = Color.Lerp(new Color(0.85f, 0.92f, 0.78f), new Color(0.53f, 0.59f, 0.53f), depth);
                Tile tile = Piece(c, r);
                if (!surface)
                {
                    tile = SoilFill();
                    tint = Color.white;
                }
                Paint(surface ? ground : soil, x, y, tile, tint,
                    surface && GroundHeight(x + 1) < top ? new Vector3(-1, 1, 1) : Vector3.one);
                if (surface && c == 2) Paint(soil, x, y, SoilFill(), Color.white);
                // Scattered embedded rocks, instead of a repeated edge or wallpaper pattern.
                if (y < top - 2 && Mathf.Abs(x * 31 + y * 17) % 43 == 0)
                    Paint(strata, x, y, Piece((x & 1) == 0 ? 4 : 5, 20),
                        Color.Lerp(new Color(0.67f, 0.63f, 0.50f), new Color(0.39f, 0.45f, 0.42f), depth));
            }
        }
        // Low pines and boulders are rooted on the hills, away from the firing lane.
        var decor = Map(root, "Hill Details", new Vector3(0, 0, -0.02f), 0.5f, 12);
        foreach (int x in new[] { -27, -25, 24, 27 })
        {
            int y = GroundHeight(x);
            Paint(decor, x, y, Piece(14, 22), new Color(0.70f, 0.80f, 0.65f));
            Paint(decor, x, y + 1, Piece(14, 21), new Color(0.70f, 0.80f, 0.65f));
        }
    }

    private static void BuildMountain(Mountain mountain)
    {
        if (mountain == null) throw new Exception("Main needs its Mountain component.");
        RemoveChild(mountain.transform, "Mountain Art");
        foreach (var old in mountain.GetComponents<Collider2D>()) Object.DestroyImmediate(old);
        var sprite = mountain.GetComponent<SpriteRenderer>();
        if (sprite != null) Object.DestroyImmediate(sprite);
        mountain.transform.position = new Vector3(0, -3, 0);
        mountain.transform.localScale = Vector3.one;
        var art = Child(mountain.transform, "Mountain Art");
        var body = Child(art, "Body");
        body.gameObject.AddComponent<Grid>();
        var stone = Map(body, "Mountain Stone", Vector3.zero, 0.5f, 3, true);
        for (int i = 0; i < MountainHeights.Length; i++)
        {
            int x = i - 9, height = MountainHeights[i];
            int before = i > 0 ? MountainHeights[i - 1] : 0;
            int after = i < MountainHeights.Length - 1 ? MountainHeights[i + 1] : 0;
            for (int y = 0; y < height; y++)
            {
                bool top = y == height - 1;
                int c = 2, r = 11;
                Vector3 flip = Vector3.one;
                if (top)
                {
                    r = 10;
                    if (before < height && after < height) c = 7;
                    else if (before < height) c = 6;
                    else if (after < height) { c = 6; flip.x = -1; }
                }
                // Broad lit and shaded faces create a ridge, rather than a wallpaper of boulders.
                float ridge = -0.7f + (13 - y) * 0.13f;
                Color tint = x < ridge ? RockLight : RockShadow;
                if (x < -3 + y * 0.20f) tint *= 0.88f;
                tint *= 0.90f + y * 0.009f;
                tint.a = 1;
                Paint(stone, x, y, Piece(c, r), tint, flip);
            }
        }
        var ridgeMap = Map(body, "Rock Ridges", new Vector3(0, 0, -0.01f), 1f / 42, 4);
        Ridge(ridgeMap, new Vector2(-0.38f, 5.88f), new Vector2(-1.25f, 4.10f), 2, Hex("b1c0bf"));
        Ridge(ridgeMap, new Vector2(-1.25f, 4.10f), new Vector2(-2.35f, 2.35f), 3, Hex("95a9ae"));
        Ridge(ridgeMap, new Vector2(-0.30f, 5.78f), new Vector2(0.12f, 4.20f), 3, Hex("5c747f"));
        Ridge(ridgeMap, new Vector2(0.12f, 4.20f), new Vector2(-0.26f, 2.95f), 3, Hex("5c747f"));
        Ridge(ridgeMap, new Vector2(1.42f, 3.66f), new Vector2(2.55f, 1.35f), 2, Hex("829da5"));
        Ridge(ridgeMap, new Vector2(-2.40f, 2.20f), new Vector2(-3.02f, 0.65f), 2, Hex("69838c"));
        Ridge(ridgeMap, new Vector2(0.8f, 2.4f), new Vector2(1.25f, 0.4f), 2, Hex("516c78"));
        var ore = Map(body, "Ore Seams", new Vector3(0, 0, -0.02f), 1f / 42, 5);
        Ore(ore, -84, 64); Ore(ore, 66, 91); Ore(ore, -19, 163);
        var sleep = Map(body, "Resting Face", new Vector3(0, 0, -0.03f), 1f / 32, 6);
        Rect(sleep, -25, 68, 11, 2, Ink); Rect(sleep, 13, 68, 11, 2, Ink);
        Rect(sleep, -5, 57, 9, 1, Ink);
        var awake = Map(body, "Awake Face", new Vector3(0, 0, -0.03f), 1f / 32, 6);
        foreach (int x in new[] { -25, 13 })
        {
            Rect(awake, x, 65, 11, 9, Ink);
            Rect(awake, x + 1, 66, 9, 7, Hex("e3e4c9"));
            Rect(awake, x + 5, 66, 3, 6, Ink);
        }
        Rect(awake, -4, 53, 6, 5, Ink);
        awake.gameObject.SetActive(false);
        BuildLeg(art, "Back Leg", -1.55f, 1, new Color(0.58f, 0.68f, 0.74f));
        BuildLeg(art, "Front Leg", 1.4f, 2, new Color(0.84f, 0.90f, 0.91f));
        var escape = mountain.GetComponent<MountainEscape>() ?? mountain.gameObject.AddComponent<MountainEscape>();
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
    }

    private static bool InsideMountain(int px, int py)
    {
        int i = Mathf.FloorToInt((px + 0.5f) / 21) + 9;
        return i >= 0 && i < MountainHeights.Length && py >= 0 && py < (MountainHeights[i] - 1) * 21;
    }

    private static void Ridge(Tilemap map, Vector2 from, Vector2 to, int width, Color color)
    {
        Vector2 a = from * 42, b = to * 42;
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b));
        for (int i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(a, b, (float)i / steps);
            for (int w = 0; w < width; w++)
            {
                int x = Mathf.RoundToInt(p.x) + w, y = Mathf.RoundToInt(p.y);
                if (InsideMountain(x, y)) Paint(map, x, y, Pixel(), color);
            }
        }
    }

    private static void Ore(Tilemap map, int x, int y)
    {
        // Embedded, irregular ore veins, at the terrain's pixel scale.
        var points = new[] { new Vector2Int(0, 0), new Vector2Int(2, 1), new Vector2Int(4, 3),
            new Vector2Int(5, 5), new Vector2Int(8, 5), new Vector2Int(10, 7), new Vector2Int(11, 8) };
        foreach (var p in points)
        {
            Rect(map, x + p.x, y + p.y, 3, 2, Hex("a77c41"));
            Rect(map, x + p.x, y + p.y + 1, 2, 1, Hex("d8b568"));
        }
    }

    private static void BuildLeg(Transform root, string name, float x, int order, Color tint)
    {
        var leg = Child(root, name);
        leg.localPosition = new Vector3(x, 0, 0);
        leg.gameObject.AddComponent<Grid>();
        var map = Map(leg, "Boulder Foot", Vector3.zero, 0.5f, order);
        // Upper stone overlaps the leaning body so the shins never appear disconnected.
        Paint(map, 0, 0, Piece(2, 11), tint);
        Paint(map, 0, -1, Piece(2, 11), tint);
        Paint(map, -1, -2, Piece(1, 11), tint);
        Paint(map, 0, -2, Piece(2, 11), tint);
        Paint(map, 1, -2, Piece(3, 11), tint);
        leg.gameObject.SetActive(false);
    }

    private static void BuildKingdom(Turret turret)
    {
        RemoveChild(turret.transform, "Kingdom Tilemaps");
        var settings = new SerializedObject(turret);
        var pivot = (Transform)settings.FindProperty("barrelPivot").objectReferenceValue;
        RemoveChild(pivot, "Cannon Tilemaps");
        // Hide the imported painterly houses and old blockout parts; keep gameplay components.
        foreach (var renderer in turret.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
        foreach (var renderer in turret.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
        foreach (var collider in turret.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        bool blue = turret.DisplayName == "Blue";
        turret.transform.position = new Vector3(blue ? -8.25f : 8.25f, 0, 0);
        turret.transform.localScale = new Vector3(blue ? 1 : -1, 1, 1);
        pivot.localPosition = new Vector3(0, 2.75f, -0.3f);
        var root = Child(turret.transform, "Kingdom Tilemaps");
        // Nine half-unit columns are centered on the cannon and fit the plateau exactly.
        root.localPosition = new Vector3(-0.25f, 0, 0);
        root.gameObject.AddComponent<Grid>();
        var walls = Map(root, "Castle Masonry", Vector3.zero, 0.5f, 30, true);
        int left = blue ? 3 : 0, middle = blue ? 4 : 1, right = blue ? 5 : 2;
        Color tint = blue ? new Color(0.84f, 0.91f, 0.97f) : new Color(0.90f, 0.88f, 0.82f);
        for (int x = -4; x <= 4; x++)
        {
            int height = Mathf.Abs(x) >= 2 ? 5 : 6;
            for (int y = 0; y < height; y++)
            {
                int c = x == -4 || x == -1 || x == 2 ? left : x == -2 || x == 1 || x == 4 ? right : middle;
                int row = y == height - 1 ? 24 : 23;
                if (c == middle && y > 0 && y < height - 1 && (x * x + y) % 3 == 0)
                { c = 9; row = blue ? 24 : 23; }
                Paint(walls, x, y, Piece(c, row), tint);
            }
            if (x == -4 || x == -2 || x == -1 || x == 1 || x == 2 || x == 4)
                Paint(walls, x, height, Piece(middle, 24), tint);
        }
        var windows = Map(root, "Doors and Windows", new Vector3(0, 0, -0.02f), 0.5f, 31);
        Paint(windows, 0, 0, Piece(10, 24), tint);
        Paint(windows, 0, 1, Piece(10, 23), tint);
        Paint(windows, -3, 3, Piece(11, 25), tint);
        Paint(windows, 3, 3, Piece(11, 25), tint);
        ConfigureCenterWindow(windows);
        var detail = Map(root, "Castle Details", new Vector3(0, 0, -0.03f), 1f / 42, 32);
        // Horizontal string courses and a dark footing tie walls together without tiled edge seams.
        var mortar = blue ? Hex("525b6d") : Hex("9f987d");
        Rect(detail, -84, 0, 189, 2, mortar);
        Rect(detail, -84, 40, 189, 2, mortar);
        var banner = blue ? Hex("447dad") : Hex("ad554e");
        foreach (int x in new[] { -61, 65 })
        {
            Rect(detail, x, 43, 13, 25, Ink);
            Rect(detail, x + 1, 44, 11, 24, banner);
            Rect(detail, x + 5, 48, 2, 13, Hex("d9bc79"));
            Rect(detail, x + 3, 54, 6, 2, Hex("d9bc79"));
        }
        Rect(detail, -4, 143, 2, 29, Hex("534b44"));
        for (int y = 0; y < 13; y++) Rect(detail, -2, 158 + y, 22 - (y % 6 < 3 ? 0 : 2), 1, banner);
        Rect(detail, 2, 163, 3, 3, Hex("ead8a0"));
        var cannon = Child(pivot, "Cannon Tilemaps");
        cannon.gameObject.AddComponent<Grid>();
        var barrel = Map(cannon, "Iron Barrel", new Vector3(0, 0, -0.1f), 1f / 42, 35);
        Rect(barrel, -4, -5, 12, 10, Hex("2a3540"));
        Rect(barrel, 1, -4, 52, 8, Hex("394954"));
        Rect(barrel, 2, 1, 49, 2, Hex("7a9298"));
        Rect(barrel, 2, -3, 49, 2, Hex("25343c"));
        Rect(barrel, 13, -4, 3, 8, Hex("a0906b"));
        Rect(barrel, 44, -5, 4, 10, Hex("a0906b"));
        Rect(barrel, 51, -5, 3, 10, Hex("27343c"));
    }

    internal static void ConfigureCenterWindow(Tilemap windows)
    {
        // The center uses the same complete arch as the sides, one course higher.
        var oldWindow = new Vector3Int(0, 3);
        var sideWindow = new Vector3Int(-3, 3);
        if (!windows.HasTile(sideWindow)) return;
        windows.SetTile(oldWindow, null);
        Paint(windows, 0, 4, windows.GetTile(sideWindow), windows.GetColor(sideWindow));
    }

    private static void ConfigureCameraAndHud()
    {
        var camera = Camera.main;
        camera.backgroundColor = Hex("a4c4d3");
        camera.orthographicSize = 6.3f;
        camera.transform.position = new Vector3(0, 0.55f, -10);
        ConfigureHudLayout();
    }

    private static void ConfigureHudLayout()
    {
        var hud = Object.FindAnyObjectByType<Hud>();
        if (hud == null) return;
        var serialized = new SerializedObject(hud);
        var status = (TMP_Text)serialized.FindProperty("statusText").objectReferenceValue;
        var events = (TMP_Text)serialized.FindProperty("eventText").objectReferenceValue;
        if (status != null) { status.fontSize = 19; status.textWrappingMode = TextWrappingModes.NoWrap; }
        var panel = GameObject.Find("HudPanel");
        if (panel != null) panel.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 0);
        if (events == null) return;

        // Keep transient hit/mining messages over the bottom soil, outside the aiming lane.
        // Reuse the same event text so every GameManager message follows this layout.
        var canvas = hud.GetComponent<Canvas>();
        var banner = canvas.transform.Find("Combat Event Banner") as RectTransform;
        if (banner == null)
        {
            var go = new GameObject("Combat Event Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = canvas.gameObject.layer;
            banner = go.GetComponent<RectTransform>();
            banner.SetParent(canvas.transform, false);
        }
        banner.anchorMin = new Vector2(0.15f, 0);
        banner.anchorMax = new Vector2(0.85f, 0);
        banner.pivot = new Vector2(0.5f, 0);
        banner.anchoredPosition = new Vector2(0, 20);
        banner.sizeDelta = new Vector2(0, 64);
        banner.SetAsLastSibling();
        var background = banner.GetComponent<Image>();
        var statusBackground = panel != null ? panel.GetComponent<Image>() : null;
        background.sprite = statusBackground != null ? statusBackground.sprite : null;
        background.type = statusBackground != null ? statusBackground.type : Image.Type.Simple;
        background.color = new Color(0, 0, 0, 0.63f);
        background.raycastTarget = false;

        var textRect = events.rectTransform;
        textRect.SetParent(banner, false);
        textRect.localScale = Vector3.one;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18, 8);
        textRect.offsetMax = new Vector2(-18, -8);
        events.fontSize = 17;
        events.alignment = TextAlignmentOptions.MidlineLeft;
        events.textWrappingMode = TextWrappingModes.Normal;
        events.raycastTarget = false;
    }

    [MenuItem("Prototype/Move Combat Messages to Bottom")]
    public static void MoveCombatMessagesToBottom()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        ConfigureHudLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("COMBAT MESSAGES MOVED TO BOTTOM BANNER");
    }

    public static void MoveCombatMessagesAndBuild()
    {
        MoveCombatMessagesToBottom();
        PrototypeBuild.BuildWindows();
    }

    private static Tile Piece(int column, int row)
    {
        int id = row * 30 + column;
        if (tiles.TryGetValue(id, out var tile)) return tile;
        string path = TileFolder + "/Pixel_" + id.ToString("D4") + ".asset";
        tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.name = "Pixel " + column + "," + row;
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SourceFolder + "/tile_" + id.ToString("D4") + ".png");
        if (tile.sprite == null) throw new Exception("Missing Kenney source tile " + id);
        tile.colliderType = Tile.ColliderType.Sprite;
        tile.flags = TileFlags.None;
        EditorUtility.SetDirty(tile);
        tiles[id] = tile;
        return tile;
    }

    private static Tile Pixel() => AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Project/Art/Tiles/DetailPixel.asset");
    private static Tile SoilFill()
    {
        const string path = TileFolder + "/Soil Fill.asset";
        if (tiles.TryGetValue(-1, out var tile)) return tile;
        tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
        tile.sprite = Pixel().sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        tile.flags = TileFlags.None;
        EditorUtility.SetDirty(tile);
        tiles[-1] = tile;
        return tile;
    }
    private static Transform Child(Transform parent, string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        return root;
    }
    private static Tilemap Map(Transform parent, string name, Vector3 position, float scale, int order, bool collision = false)
    {
        var root = Child(parent, name);
        root.localPosition = position;
        root.localScale = Vector3.one * scale;
        var map = root.gameObject.AddComponent<Tilemap>();
        var renderer = root.gameObject.AddComponent<TilemapRenderer>();
        renderer.sharedMaterial = spriteMaterial;
        renderer.sortingOrder = order;
        if (collision) root.gameObject.AddComponent<TilemapCollider2D>();
        return map;
    }
    private static void Paint(Tilemap map, int x, int y, TileBase tile, Color color) => Paint(map, x, y, tile, color, Vector3.one);
    private static void Paint(Tilemap map, int x, int y, TileBase tile, Color color, Vector3 scale)
    {
        var p = new Vector3Int(x, y);
        map.SetTile(p, tile);
        map.SetColor(p, color);
        if (scale != Vector3.one) map.SetTransformMatrix(p, Matrix4x4.Scale(scale));
    }
    private static void Rect(Tilemap map, int x, int y, int width, int height, Color color)
    {
        for (int dy = 0; dy < height; dy++)
        for (int dx = 0; dx < width; dx++) Paint(map, x + dx, y + dy, Pixel(), color);
    }
    private static void RemoveChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
    private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }

    public static void Capture()
    {
        Directory.CreateDirectory("Logs/ArtReview");
        var camera = Camera.main;
        var target = new RenderTexture(1600, 900, 24);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes("Logs/ArtReview/tilemap-scene.png", image.EncodeToPNG());
        camera.targetTexture = previous;
        RenderTexture.active = active;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
    }
}
