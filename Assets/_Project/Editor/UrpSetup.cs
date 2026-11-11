using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UrpSetup
{
    private const string SettingsFolder = "Assets/_Project/Settings";
    private const string ResourceFolder = "Assets/_Project/Resources/Prototype";
    private const string PipelinePath = SettingsFolder + "/PrototypeURP.asset";

    public static void MigrateAndBuild()
    {
        Configure();
        Validate();
        PrototypeBuild.BuildWindows();
    }

    [MenuItem("Prototype/Configure URP")]
    public static void Configure()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SettingsFolder + "/PrototypeRenderer.asset");
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, SettingsFolder + "/PrototypeRenderer.asset");
            ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal");
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }
        pipeline.supportsHDR = false;
        pipeline.msaaSampleCount = 1;
        pipeline.supportsCameraDepthTexture = false;
        pipeline.supportsCameraOpaqueTexture = false;
        pipeline.shadowDistance = 0;
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(renderer);

        GraphicsSettings.defaultRenderPipeline = pipeline;
        int originalQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(originalQuality, false);

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Resources", "Prototype");
        CreateRuntimeMaterial("SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default", false);
        CreateRuntimeMaterial("ShapeUnlit", "Universal Render Pipeline/Unlit", true);
        CreateRuntimeMaterial("LineUnlit", "Universal Render Pipeline/Particles/Unlit", true);

        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            Color color = material.color;
            material.shader = unlit;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }

        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity", OpenSceneMode.Single);
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>())
        {
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.SetRenderer(0);
        }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("URP CONFIGURED: default graphics pipeline, every quality level, cameras, and materials.");
    }

    private static void CreateRuntimeMaterial(string name, string shaderName, bool transparent)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new Exception("Missing URP shader: " + shaderName);
        string path = ResourceFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        material.color = Color.white;
        if (transparent)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Prototype/Validate URP")]
    public static void Validate()
    {
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
            throw new Exception("The default graphics pipeline must use URP.");
        int originalQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            if (!(QualitySettings.renderPipeline is UniversalRenderPipelineAsset))
                throw new Exception("Quality level does not use URP: " + QualitySettings.names[i]);
        }
        QualitySettings.SetQualityLevel(originalQuality, false);
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material.shader == null || !material.shader.name.StartsWith("Universal Render Pipeline/"))
                throw new Exception("Material does not use a URP shader: " + material.name);
        }
        Debug.Log("URP VALIDATION PASSED");
    }
}
