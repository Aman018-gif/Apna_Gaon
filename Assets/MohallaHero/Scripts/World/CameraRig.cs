using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Smooth-follow orthographic camera clamped to the map, plus one full-screen tint that combines the time of day
    /// with the mohalla's mood: a low Mohalla Rating makes the city grey and dim, a high one keeps it bright.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;
        public Camera Cam { get; private set; }
        SpriteRenderer overlay;
        const float Smooth = 8f;
        public const float Zoom = 7.5f;
        float shake;

        public static CameraRig Create()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = Zoom;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.12f, 0.22f);
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
            cam.transform.position = new Vector3(0, 0, -10f);
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Cam = cam;
            return rig;
        }

        /// <summary>The tint overlay is the fallback mood/day-night effect when URP 2D lighting isn't active.</summary>
        public void EnableOverlay(bool on)
        {
            if (Gfx.Urp) on = false;   // CityLighting does it with real lights and colour grading
            if (overlay == null)
            {
                overlay = WorldObjects.CreateSprite("MoodOverlay", SpriteFactory.White, transform.position, transform, 200);
                overlay.transform.localPosition = new Vector3(0, 0, 5f);
                overlay.transform.localScale = new Vector3(400f, 400f, 1f);
            }
            overlay.gameObject.SetActive(on);
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        public void SnapToTarget()
        {
            if (Target == null) return;
            transform.position = Clamp(Target.position);
        }

        void LateUpdate()
        {
            if (Target != null)
            {
                var desired = Clamp(Target.position);
                var p = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-Smooth * Time.unscaledDeltaTime));
                if (shake > 0f)
                {
                    shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime);
                    p += (Vector3)(Random.insideUnitCircle * shake * 0.4f);
                }
                transform.position = p;
            }
            UpdateOverlay();
        }

        Vector3 Clamp(Vector3 p)
        {
            float halfH = Cam.orthographicSize, halfW = halfH * Cam.aspect;
            float x = halfW * 2 >= WorldLayout.Width ? WorldLayout.Width / 2f : Mathf.Clamp(p.x, halfW, WorldLayout.Width - halfW);
            float y = Mathf.Clamp(p.y + 0.5f, halfH, WorldLayout.Height - halfH);
            return new Vector3(x, y, -10f);
        }

        void UpdateOverlay()
        {
            if (overlay == null || !overlay.gameObject.activeSelf) return;
            var gm = GameManager.I;
            if (gm == null || gm.Clock == null) return;
            overlay.color = Tint(gm.Clock.HourFloat, gm.Rating);
        }

        /// <summary>Time-of-day light blended with the mood of the mohalla (rating 0..100).</summary>
        public static Color Tint(float hour, int rating)
        {
            Color day;
            if (hour < 8f) day = Color.Lerp(new Color(1f, 0.65f, 0.4f, 0.18f), new Color(1, 1, 1, 0f), hour - 7f);
            else if (hour < 17.5f) day = new Color(1, 1, 1, 0f);
            else if (hour < 19f) day = Color.Lerp(new Color(1, 1, 1, 0f), new Color(1f, 0.5f, 0.3f, 0.22f), (hour - 17.5f) / 1.5f);
            else if (hour < 20.5f) day = Color.Lerp(new Color(1f, 0.5f, 0.3f, 0.22f), new Color(0.08f, 0.08f, 0.3f, 0.48f), (hour - 19f) / 1.5f);
            else day = new Color(0.06f, 0.07f, 0.26f, 0.52f);

            // Gloom: grey-brown haze when the rating is low, none above 70.
            float gloom = Mathf.Lerp(0.32f, 0f, Mathf.Clamp01(rating / 70f));
            var haze = new Color(0.42f, 0.4f, 0.36f, gloom);
            float a = 1f - (1f - day.a) * (1f - haze.a);
            if (a <= 0.001f) return new Color(1, 1, 1, 0);
            Color rgb = (day * day.a + haze * haze.a * (1f - day.a)) / a;
            rgb.a = a;
            return rgb;
        }
    }
}
