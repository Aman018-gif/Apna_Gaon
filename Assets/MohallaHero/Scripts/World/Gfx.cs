using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MohallaHero
{
    /// <summary>
    /// Rendering helpers. With URP's 2D Renderer (set up by Editor/GraphicsSetup) the city gets real 2D lights, bloom and
    /// colour grading; without it, everything falls back to the original sprite glows and tint overlay.
    /// </summary>
    public static class Gfx
    {
        static Material unlit;
        static int[] allLayers;

        /// <summary>True when the URP 2D renderer is active.</summary>
        public static bool Urp => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

        /// <summary>Sprite material that ignores 2D lights: markers, signs and glows stay readable at night.</summary>
        public static Material Unlit
        {
            get
            {
                if (unlit != null) return unlit;
                unlit = Resources.Load<Material>("Materials/SpriteUnlit");
                if (unlit == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader != null) unlit = new Material(shader);
                }
                return unlit;
            }
        }

        /// <summary>Makes a sprite ignore lighting (no-op without URP, where nothing is lit anyway).</summary>
        public static void MakeUnlit(Renderer r)
        {
            if (Urp && Unlit != null) r.sharedMaterial = Unlit;
        }

        static int[] AllLayers => allLayers ?? (allLayers = SortingLayer.layers.Select(l => l.id).ToArray());

        /// <summary>Creates a 2D light. Lights are configured while inactive, then enabled (required for global lights).</summary>
        public static Light2D CreateLight(Transform parent, string name, Light2D.LightType type, Color color, float intensity, float radius = 3f, float inner = 0f)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light2D>();
            l.lightType = type;
            l.targetSortingLayers = AllLayers;
            l.color = color;
            l.intensity = intensity;
            if (type == Light2D.LightType.Point)
            {
                l.pointLightOuterRadius = radius;
                l.pointLightInnerRadius = inner;
                l.falloffIntensity = 0.65f;
            }
            go.SetActive(true);
            return l;
        }
    }

    /// <summary>
    /// A warm glow (street lamp, bulb, diya, candle, window): a 2D point light with URP, otherwise a soft additive sprite.
    /// Toggle with <see cref="On"/>.
    /// </summary>
    public class GlowLight : MonoBehaviour
    {
        Light2D light2D;
        SpriteRenderer sprite;
        float baseIntensity;
        bool on = true;
        public bool Flicker;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                if (light2D != null) light2D.enabled = value;
                if (sprite != null) sprite.enabled = value;
            }
        }

        public static GlowLight Create(Transform parent, Vector2 offset, Color color, float size)
        {
            var go = new GameObject("Glow");
            go.transform.SetParent(parent, false);
            go.transform.position = (Vector2)parent.position + offset;
            var g = go.AddComponent<GlowLight>();
            if (Gfx.Urp)
            {
                var l = Gfx.CreateLight(go.transform, "Light", Light2D.LightType.Point, new Color(color.r, color.g, color.b, 1f), 1.1f + color.a, size * 0.75f, size * 0.12f);
                g.light2D = l;
                g.baseIntensity = l.intensity;
            }
            else
            {
                var sr = WorldObjects.CreateSprite("Sprite", SpriteFactory.SoftCircle, go.transform.position, go.transform, 210);
                sr.color = color;
                sr.transform.localScale = Vector3.one * size;
                g.sprite = sr;
            }
            return g;
        }

        void Update()
        {
            if (!Flicker || light2D == null || !on) return;
            light2D.intensity = baseIntensity * (0.85f + Mathf.PerlinNoise(Time.time * 6f, transform.position.x) * 0.3f);
        }
    }
}
