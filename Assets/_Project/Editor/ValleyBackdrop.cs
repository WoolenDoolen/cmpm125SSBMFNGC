using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Original scene geometry: stepped silhouettes and discrete shaded planes, no building images.
// The Terraria reference informs atmospheric depth, valley framing and palette, not copied art.
public static class ValleyBackdrop
{
    private const string Folder = "Assets/_Project/Art/ValleyBackdrop";
    private const float Pixel = 1f / 32;
    private static Transform root;
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public static void Build()
    {
        var old = GameObject.Find("Landscape Background");
        if (old != null) Object.DestroyImmediate(old);
        root = new GameObject("Landscape Background").transform;
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        materials.Clear();

        // Quiet sky bands become lighter toward the valley horizon.
        for (int i = 0; i < 20; i++)
        {
            Color color = Color.Lerp(Hex("bed1d3"), Hex("86acc9"), i / 19f);
            Shape("Sky " + i, color, 12, -24, -5 + i * 0.7f, 24, -5 + i * 0.7f,
                24, -4.3f + i * 0.7f, -24, -4.3f + i * 0.7f);
        }
        Disc("Sun Halo", Hex("b9ccc9"), 11.8f, -2.75f, 5.3f, 0.65f);
        Disc("Sun", Hex("e5dbad"), 11.7f, -2.75f, 5.3f, 0.43f);
        Cloud("Cloud A", -6.0f, 5.8f, 1.25f, 11.6f);
        Cloud("Cloud B", 3.8f, 5.7f, 1.0f, 11.6f);
        Cloud("Cloud C", 0.2f, 4.8f, 0.55f, 11.6f);

        Shape("Far Range", Hex("9cb8c4"), 11,
            -24, -5, -24, 2.7f, -17, 3.5f, -14, 2.8f, -11, 4.6f, -9, 3.3f,
            -6, 4.6f, -4.6f, 3.3f, -2.8f, 4.5f, -1.8f, 4.0f, 0.8f, 6.0f,
            1.5f, 6.1f, 2.4f, 5.0f, 3.6f, 3.9f, 5.0f, 4.6f, 7, 3.5f,
            9.8f, 5.3f, 11.4f, 4.4f, 14, 5.0f, 18, 3.3f, 24, 3.8f, 24, -5);
        Shape("Far Peak Face", Hex("adc3ca"), 10.9f,
            -2.0f, 0, 0.8f, 6.0f, 1.5f, 6.1f, 0.9f, 5.15f, 1.05f, 4.6f,
            0.5f, 4.3f, 0.7f, 3.75f, 0.15f, 3.5f, 0.35f, 3.0f, -0.2f, 2.5f, -0.6f, 1.4f);
        Shape("Far Valley", Hex("8faeaa"), 10.7f,
            -24, -5, -24, 1.5f, -16, 2.0f, -12, 1.0f, -7, 1.6f, -4, 0.7f,
            -0.4f, 1.4f, 2, 2.2f, 4, 1.6f, 8, 0.8f, 13, 1.4f, 24, 0.7f, 24, -5);

        // Large left and right mountain masses frame an open river valley.
        Shape("West Mountains", Hex("8b9fa9"), 10,
            -24, -5, -24, 4.0f, -16.0f, 3.6f, -13.8f, 5.7f, -12.8f, 5.85f,
            -12.2f, 6.2f, -11.8f, 5.85f, -11.4f, 5.9f, -10.6f, 4.55f,
            -10, 4.1f, -9.0f, 3.9f, -8.2f, 2.8f, -7.5f, 2.35f,
            -6.5f, 1.2f, -4.4f, -0.4f, -2.5f, -1.1f, -2, -5);
        Shape("West Light Face", Hex("a8b7bd"), 9.9f,
            -15.5f, -0.6f, -13.8f, 5.7f, -12.8f, 5.85f, -12.2f, 6.2f,
            -12.6f, 5.1f, -12.15f, 4.7f, -12.8f, 3.7f, -12.6f, 3.4f,
            -13.15f, 2.5f, -13.0f, 2.2f, -13.5f, 1.3f, -13.6f, 0.2f);
        Shape("West Ridge", Hex("a0b1b9"), 9.85f,
            -12.2f, 6.2f, -11.8f, 5.85f, -11.4f, 5.9f, -10.6f, 4.55f,
            -10.8f, 3.3f, -10.5f, 2.5f, -10.55f, 1.4f, -11.0f, 0.3f,
            -11.8f, -0.6f, -11.4f, 1.2f, -11.6f, 1.9f, -11.45f, 2.4f,
            -11.9f, 3.15f, -11.75f, 4.0f, -12.05f, 4.8f);
        Shape("West Broken Shoulder", Hex("9aaeb6"), 9.8f,
            -10, 4.1f, -9, 3.9f, -8.2f, 2.8f, -7.5f, 2.35f, -6.5f, 1.2f,
            -5, 0, -6.5f, -0.4f, -7.1f, 0.7f, -7.7f, 0.8f, -8.3f, 1.7f, -8.7f, 2.2f);

        Shape("East Mountains", Hex("8dA3ac"), 9.6f,
            1, -5, 2.8f, -0.9f, 3.7f, 0.3f, 4.6f, 1.5f, 5.2f, 1.8f,
            6.2f, 3.4f, 6.7f, 3.7f, 7.4f, 5.2f, 8.4f, 5.3f,
            8.8f, 6.0f, 9.3f, 6.15f, 10.0f, 4.9f, 10.8f, 4.2f,
            11.7f, 4.0f, 12.5f, 3.2f, 14.3f, 2.9f, 16.5f, 4.4f, 19, 4.1f, 24, 5, 24, -5);
        Shape("East Summit Light", Hex("afbec2"), 9.5f,
            9.3f, 6.15f, 8.8f, 6.0f, 8.4f, 5.3f, 7.4f, 5.2f, 7.1f, 4.2f,
            7.3f, 3.5f, 6.8f, 2.9f, 6.8f, 2.2f, 6.25f, 1.7f, 5.9f, 0.5f,
            6.6f, 1.0f, 7.3f, 1.5f, 7.6f, 2.4f, 7.9f, 2.7f, 7.8f, 3.6f,
            8.4f, 4.25f, 8.4f, 4.8f, 8.9f, 5.25f);
        Shape("East Ridge Shade", Hex("7e98a4"), 9.45f,
            9.3f, 6.15f, 10, 4.9f, 10.8f, 4.2f, 11.7f, 4.0f, 12.5f, 3.2f,
            13.3f, 1.0f, 11.5f, -0.5f, 11.2f, 0.9f, 10.6f, 1.5f,
            10.6f, 2.6f, 9.9f, 3.1f, 10.0f, 3.9f, 9.65f, 4.7f);

        // Broken strata follow the rock faces. They are long directional folds, not noise.
        Shape("West Cleft A", Hex("839ba6"), 9.75f,
            -12.4f, 5.1f, -12.7f, 4.4f, -12.9f, 4.2f, -12.85f, 3.6f,
            -13.2f, 2.6f, -13.1f, 3.8f, -13.0f, 4.4f, -12.6f, 4.8f);
        Shape("West Cleft B", Hex("7c96a2"), 9.74f,
            -11.75f, 4.4f, -11.1f, 3.6f, -11.05f, 2.8f, -10.6f, 2.1f,
            -10.5f, 1.3f, -10.8f, 2.0f, -11.3f, 2.5f, -11.4f, 3.4f);
        Shape("West Cleft C", Hex("b0bec1"), 9.73f,
            -10.0f, 3.6f, -9.7f, 2.8f, -9.8f, 2.4f, -9.25f, 1.7f,
            -9.0f, 0.4f, -9.5f, 1.4f, -10.0f, 1.9f, -10.0f, 2.7f);
        Shape("West Talus", Hex("879eaa"), 9.72f,
            -8.6f, 2.4f, -8.25f, 1.9f, -8.2f, 1.4f, -7.5f, 0.9f,
            -7.2f, 0.0f, -7.9f, 0.6f, -8.5f, 1.0f);
        Shape("West Ledge", Hex("7f9a89"), 9.7f,
            -11.2f, 2.7f, -10.8f, 2.7f, -10.4f, 2.6f, -10.9f, 2.56f, -11.25f, 2.6f);
        Shape("East Cleft A", Hex("819ca8"), 9.4f,
            8.8f, 5.4f, 8.45f, 4.7f, 8.6f, 4.3f, 8.05f, 3.7f,
            7.9f, 2.9f, 8.3f, 3.5f, 8.9f, 4.0f, 8.7f, 4.5f);
        Shape("East Cleft B", Hex("9fb3ba"), 9.39f,
            9.8f, 4.7f, 10.0f, 3.8f, 9.8f, 3.4f, 10.4f, 2.6f,
            10.5f, 1.5f, 10.05f, 2.3f, 9.4f, 3.0f, 9.5f, 3.8f);
        Shape("East Cleft C", Hex("afbec0"), 9.38f,
            6.7f, 3.5f, 6.3f, 2.8f, 6.4f, 2.4f, 5.7f, 1.8f,
            5.4f, 0.9f, 6.05f, 1.6f, 6.7f, 2.15f, 6.55f, 2.7f);
        Shape("East Grassy Shelf", Hex("809d8b"), 9.36f,
            7.15f, 3.45f, 7.65f, 3.5f, 8.0f, 3.38f, 7.5f, 3.3f, 7.0f, 3.3f);
        Pine("West Shelf Pine", -10.85f, 2.66f, 0.4f, Hex("799685"), 9.35f);
        Pine("East Shelf Pine", 7.5f, 3.41f, 0.38f, Hex("789583"), 9.34f);

        Shape("Distant Meadows", Hex("7d9d85"), 9,
            -24, -5, -24, -0.1f, -15, -0.3f, -10, 0.4f, -7, -0.3f,
            -4.5f, 0.2f, -1.4f, 0.1f, 0, 0.6f, 2.3f, 1.3f, 3.4f, 0.75f,
            4.5f, 0.5f, 5.4f, -0.2f, 8, 0.15f, 13, -0.4f, 18, 0.4f, 24, -0.1f, 24, -5);
        Shape("River Valley", Hex("6b8f72"), 8.9f,
            -24, -5, -24, -0.6f, -14, -0.7f, -9, -0.4f, -6, -0.8f,
            -3.7f, -0.5f, -1.8f, -0.9f, 0.3f, -0.3f, 2, 0.5f,
            3, 0.2f, 4.1f, -0.5f, 6, -0.7f, 9, -0.2f, 15, -0.8f, 24, -0.9f, 24, -5);
        float[] river = {
            -6, -5, -4.5f, -3.0f, -2.9f, -2.0f, -0.1f, -0.7f,
            0.9f, 0.0f, 1.3f, 0.8f, 1.1f, 1.35f, 1.8f, 1.8f, 2.1f, 1.85f,
            1.45f, 1.3f, 1.75f, 0.8f, 1.55f, 0.1f, 0.7f, -0.8f,
            -1.3f, -2.0f, -2.0f, -3.1f, -2.4f, -5 };
        Shape("River", Hex("8eb7bd"), 8.7f, river);
        for (int i = 0; i < 15; i++)
        {
            float y = -2.8f + i * 0.25f;
            var edges = HorizontalSpan(river, y);
            float width = (edges.y - edges.x) * 0.36f;
            float x = Mathf.Lerp(edges.x, edges.y, 0.30f + (i % 3) * 0.11f) - width * 0.5f;
            Shape("River Glint " + i, Hex("a5c7ca"), 8.6f,
                x, y, x + width, y, x + width, y + Pixel, x, y + Pixel);
        }
        Shape("Near Meadow West", Hex("577e60"), 8.4f,
            -24, -5, -24, -1.5f, -15, -1.8f, -10, -1.1f, -7.5f, -1.5f,
            -6, -1.1f, -4.5f, -1.45f, -3, -2.1f, -4, -3.1f, -5, -5);
        Shape("Near Meadow East", Hex("5f8565"), 8.3f,
            -1.8f, -5, -1.4f, -3.0f, -0.2f, -2.4f, 1.4f, -1.6f, 3.3f, -1.3f,
            5, -1.0f, 7, -1.5f, 9, -1.1f, 14, -1.5f, 19, -1.1f, 24, -1.5f, 24, -5);

        // Sparse treelines describe scale without competing with foreground silhouettes.
        for (int i = 0; i < 39; i++)
        {
            float x = -15.5f + i * 0.82f;
            float y = -0.55f + 0.18f * Mathf.Sin(i * 1.3f);
            if (x > -2.5f && x < 3.5f) continue;
            Pine("Far Pine " + i, x, y, 0.65f + (i % 4) * 0.14f, Hex("678974"), 8.05f);
        }
        float[] nearX = { -14, -12.9f, -11.3f, -10.6f, -8.9f, -7.8f, -6.5f, -5.8f, -4.8f, 3.5f, 4.8f, 6.2f, 7.6f, 8.5f, 10.1f, 11.5f, 13.1f };
        for (int i = 0; i < nearX.Length; i++)
            Pine("Near Pine " + i, nearX[i], -1.9f + (i % 3) * 0.13f,
                1.0f + (i % 5) * 0.17f, Hex("456d56"), 7.8f);
        Bird("Bird A", -0.5f, 5.25f); Bird("Bird B", -0.15f, 5.38f); Bird("Bird C", 3.7f, 4.4f);
        // Bring the west summit into the frame while leaving the central valley open.
        foreach (Transform child in root)
            if (child.name.StartsWith("West")) child.localPosition += Vector3.right * (77 * Pixel);
    }

    private static void Cloud(string name, float x, float y, float size, float depth)
    {
        Shape(name, Hex("c6d9de"), depth,
            x, y, x + 1.9f * size, y, x + 1.9f * size, y + 0.12f * size,
            x + 1.5f * size, y + 0.12f * size, x + 1.45f * size, y + 0.25f * size,
            x + size, y + 0.25f * size, x + 0.9f * size, y + 0.38f * size,
            x + 0.6f * size, y + 0.38f * size, x + 0.45f * size, y + 0.18f * size,
            x + 0.1f * size, y + 0.18f * size);
    }

    private static void Disc(string name, Color color, float depth, float x, float y, float radius)
    {
        var points = new float[48];
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI / 12;
            points[i * 2] = x + Mathf.Cos(a) * radius;
            points[i * 2 + 1] = y + Mathf.Sin(a) * radius;
        }
        Shape(name, color, depth, points);
    }

    private static void Pine(string name, float x, float y, float size, Color color, float depth)
    {
        Shape(name + " Trunk", color * 0.86f, depth + 0.01f,
            x - Pixel, y, x + Pixel, y, x + Pixel, y + size * 0.7f, x - Pixel, y + size * 0.7f);
        Shape(name, color, depth,
            x, y + size, x + size * 0.18f, y + size * 0.68f, x + size * 0.08f, y + size * 0.68f,
            x + size * 0.28f, y + size * 0.37f, x + size * 0.16f, y + size * 0.37f,
            x + size * 0.37f, y + size * 0.10f, x - size * 0.37f, y + size * 0.10f,
            x - size * 0.16f, y + size * 0.37f, x - size * 0.28f, y + size * 0.37f,
            x - size * 0.08f, y + size * 0.68f, x - size * 0.18f, y + size * 0.68f);
    }

    private static void Bird(string name, float x, float y) => Shape(name, Hex("7e9da9"), 7.6f,
        x - 0.1f, y + Pixel, x, y, x + 0.1f, y + Pixel, x + 0.1f, y + 2 * Pixel,
        x, y + Pixel, x - 0.1f, y + 2 * Pixel);

    private static Vector2 HorizontalSpan(float[] polygon, float y)
    {
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        for (int i = 0; i < polygon.Length; i += 2)
        {
            int next = (i + 2) % polygon.Length;
            float ay = polygon[i + 1], by = polygon[next + 1];
            if (!((ay <= y && by > y) || (by <= y && ay > y))) continue;
            float x = polygon[i] + (y - ay) / (by - ay) * (polygon[next] - polygon[i]);
            left = Mathf.Min(left, x); right = Mathf.Max(right, x);
        }
        return new Vector2(left, right);
    }

    // Raster-like scanline geometry gives stepped pixel contours, using only a few quads
    // per scanline rather than filling the background with hundreds of thousands of tiles.
    private static void Shape(string name, Color color, float depth, params float[] coordinates)
    {
        var points = new Vector2[coordinates.Length / 2];
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
            min = Mathf.Min(min, points[i].y); max = Mathf.Max(max, points[i].y);
        }
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var intersections = new List<float>();
        for (float bottom = Mathf.Floor(min / Pixel) * Pixel; bottom < max; bottom += Pixel)
        {
            float scan = bottom + Pixel * 0.5f;
            intersections.Clear();
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                if ((a.y <= scan && b.y > scan) || (b.y <= scan && a.y > scan))
                    intersections.Add(a.x + (scan - a.y) / (b.y - a.y) * (b.x - a.x));
            }
            intersections.Sort();
            for (int i = 0; i + 1 < intersections.Count; i += 2)
            {
                float left = Mathf.Round(intersections[i] / Pixel) * Pixel;
                float right = Mathf.Round(intersections[i + 1] / Pixel) * Pixel;
                if (right <= left) continue;
                int n = vertices.Count;
                vertices.Add(new Vector3(left, bottom)); vertices.Add(new Vector3(left, bottom + Pixel));
                vertices.Add(new Vector3(right, bottom + Pixel)); vertices.Add(new Vector3(right, bottom));
                indices.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        string path = Folder + "/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); }
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(0, 0, depth);
        go.GetComponent<MeshFilter>().sharedMesh = saved;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        EditorUtility.SetDirty(saved);
    }

    private static Material MaterialFor(Color color)
    {
        color.a = 1;
        string id = ColorUtility.ToHtmlStringRGB(color);
        if (materials.TryGetValue(id, out var material)) return material;
        string path = Folder + "/Palette " + id + ".mat";
        material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Cull", 0);
        materials[id] = material;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
}
