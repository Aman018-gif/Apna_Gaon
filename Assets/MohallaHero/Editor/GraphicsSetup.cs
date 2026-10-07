using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MohallaHero.EditorTools
{
    /// <summary>
    /// Switches the project to URP with the 2D Renderer (2D lights, bloom, colour grading) on first open.
    /// Creates Assets/MohallaHero/Settings/{Renderer2D, URP} and Resources/Materials/SpriteUnlit (for markers, signs and
    /// glows that must not be darkened at night), then assigns the pipeline in Graphics and every Quality level.
    /// Also runnable from the menu or with -executeMethod MohallaHero.EditorTools.GraphicsSetup.Run.
    /// </summary>
    [InitializeOnLoad]
    public static class GraphicsSetup
    {
        const string SettingsDir = "Assets/MohallaHero/Settings";
        const string RendererPath = SettingsDir + "/Renderer2D.asset";
        const string PipelinePath = SettingsDir + "/URP_2D.asset";
        const string UnlitPath = "Assets/MohallaHero/Resources/Materials/SpriteUnlit.mat";

        static GraphicsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) Ensure(false);
            };
        }

        [MenuItem("Mohalla Hero/Setup 2D Lighting (URP)")]
        static void FromMenu() => Ensure(true);

        /// <summary>Entry point for -executeMethod.</summary>
        public static void Run() => Ensure(true);

        static void Ensure(bool log)
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(UnlitPath));

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = true;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            bool changed = false;
            if (GraphicsSettings.defaultRenderPipeline != pipeline) { GraphicsSettings.defaultRenderPipeline = pipeline; changed = true; }
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != pipeline) { QualitySettings.renderPipeline = pipeline; changed = true; }
            }
            QualitySettings.SetQualityLevel(current, false);

            if (AssetDatabase.LoadAssetAtPath<Material>(UnlitPath) == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null)
                {
                    AssetDatabase.CreateAsset(new Material(shader) { name = "SpriteUnlit" }, UnlitPath);
                    changed = true;
                }
                else Debug.LogWarning("Mohalla Hero: URP 2D unlit sprite shader not found.");
            }

            AssetDatabase.SaveAssets();
            if (changed || log) Debug.Log("Mohalla Hero: URP 2D lighting is set up (" + PipelinePath + ").");
        }
    }
}
