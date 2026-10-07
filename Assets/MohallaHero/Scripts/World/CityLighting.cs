using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MohallaHero
{
    /// <summary>
    /// URP 2D lighting and post-processing for Shanti Nagar:
    /// <list type="bullet">
    /// <item>a global "sun" whose colour and strength follow the clock (dawn, noon, golden hour, blue night);</item>
    /// <item>warm window lights after dark; the better the Mohalla Rating, the more homes are lit;</item>
    /// <item>coloured string lights over the Mela Maidan and a soft lantern around the player at night;</item>
    /// <item>bloom (lamps, diyas, fireflies glow), a vignette, and colour grading that drains colour from the city
    /// when the rating is low and makes it vivid when it is high.</item>
    /// </list>
    /// Disabled automatically when URP is not active (the old tint overlay is used instead).
    /// </summary>
    public class CityLighting : MonoBehaviour
    {
        Light2D sun, playerLight;
        Bloom bloom;
        ColorAdjustments grading;
        Vignette vignette;
        readonly List<(GlowLight glow, int threshold)> windows = new List<(GlowLight, int)>();
        readonly List<GlowLight> melaLights = new List<GlowLight>();
        float smoothedRating = -1f;

        public void Init(WorldBuilder builder, Transform player, Camera cam)
        {
            if (!Gfx.Urp) { enabled = false; return; }

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;

            sun = Gfx.CreateLight(transform, "Sun", Light2D.LightType.Global, Color.white, 1f);
            playerLight = Gfx.CreateLight(player, "Lantern", Light2D.LightType.Point, new Color(1f, 0.85f, 0.6f), 0.55f, 4.5f, 0.5f);
            playerLight.transform.localPosition = new Vector3(0, 0.6f, 0);

            var volumeGo = new GameObject("PostFX");
            volumeGo.transform.SetParent(transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);
            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);
            grading = profile.Add<ColorAdjustments>(true);
            grading.saturation.Override(0f);
            grading.contrast.Override(8f);
            grading.postExposure.Override(0f);
            grading.colorFilter.Override(Color.white);
            volume.profile = profile;

            // Window lights: one or two per building, each with its own "who's home" threshold.
            var rng = new System.Random(11);
            foreach (var b in WorldLayout.Buildings)
            {
                if (b.style == BuildingStyle.Stage || b.style == BuildingStyle.Booth) continue;
                int count = b.rect.width >= 9 ? 3 : b.rect.width >= 6 ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float x = b.rect.x + b.rect.width * (i + 1f) / (count + 1f);
                    var g = GlowLight.Create(transform, new Vector2(x, b.rect.y + 1.3f) - (Vector2)transform.position, new Color(1f, 0.78f, 0.45f, 0.2f), 3.4f);
                    windows.Add((g, rng.Next(0, 100)));
                }
            }

            // Fairy lights over the Mela (only visible once the Maidan opens; they live under the Mela root).
            var colors = new[] { new Color(1f, 0.3f, 0.6f), new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.9f, 0.9f), new Color(1f, 0.55f, 0.2f) };
            int k = 0;
            for (float x = 77.5f; x < 94f; x += 2f)
            {
                var g = GlowLight.Create(builder.MelaRoot, new Vector2(x, 16.2f) - (Vector2)builder.MelaRoot.position, new Color(colors[k % 4].r, colors[k % 4].g, colors[k % 4].b, 0.4f), 2.2f);
                melaLights.Add(g);
                k++;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld || gm.Clock == null) return;
            float hour = gm.Clock.HourFloat;
            int rating = gm.Rating;
            smoothedRating = smoothedRating < 0 ? rating : Mathf.MoveTowards(smoothedRating, rating, Time.deltaTime * 15f);

            var (c, i) = Daylight(hour);
            sun.color = c;
            sun.intensity = i;

            bool dark = hour >= 18.8f;
            playerLight.enabled = dark;
            foreach (var (glow, threshold) in windows) glow.On = dark && threshold < 30 + smoothedRating * 0.6f;
            foreach (var g in melaLights) g.On = hour >= 17.5f;

            float mood = Mathf.Clamp01(smoothedRating / 75f);   // 0 = gloomy, 1 = thriving
            grading.saturation.Override(Mathf.Lerp(-55f, 12f, mood));
            grading.postExposure.Override(Mathf.Lerp(-0.25f, 0.05f, mood));
            grading.colorFilter.Override(Color.Lerp(new Color(0.88f, 0.85f, 0.8f), Color.white, mood));
            vignette.intensity.Override(Mathf.Lerp(0.34f, 0.18f, mood));
            bloom.intensity.Override(dark ? 1.3f : 0.6f);
        }

        /// <summary>Colour and intensity of the sun for an hour of the day (07:00–23:00).</summary>
        public static (Color color, float intensity) Daylight(float hour)
        {
            var dawn = (new Color(1f, 0.78f, 0.62f), 0.8f);
            var noon = (new Color(1f, 0.98f, 0.94f), 1.05f);
            var golden = (new Color(1f, 0.72f, 0.48f), 0.85f);
            var dusk = (new Color(0.75f, 0.55f, 0.75f), 0.55f);
            var night = (new Color(0.45f, 0.52f, 0.9f), 0.3f);
            if (hour < 9f) return Mix(dawn, noon, (hour - 7f) / 2f);
            if (hour < 16.5f) return noon;
            if (hour < 18f) return Mix(noon, golden, (hour - 16.5f) / 1.5f);
            if (hour < 19f) return Mix(golden, dusk, hour - 18f);
            if (hour < 20f) return Mix(dusk, night, hour - 19f);
            return night;
        }

        static (Color, float) Mix((Color c, float i) a, (Color c, float i) b, float t)
        {
            t = Mathf.Clamp01(t);
            return (Color.Lerp(a.c, b.c, t), Mathf.Lerp(a.i, b.i, t));
        }
    }
}
