using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Small things that make the city feel alive: cloud shadows drifting over the streets by day, fireflies in
    /// Gulmohar Park after dark and gulmohar petals drifting down around the trees. Pure sprites, cheap on phones.
    /// </summary>
    public class Ambience : MonoBehaviour
    {
        readonly List<SpriteRenderer> clouds = new List<SpriteRenderer>();
        readonly List<(SpriteRenderer sr, Vector2 home, float phase)> fireflies = new List<(SpriteRenderer, Vector2, float)>();
        readonly List<(SpriteRenderer sr, Vector2 origin, float t, float speed)> petals = new List<(SpriteRenderer, Vector2, float, float)>();

        public void Init()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 4; i++)
            {
                var sr = WorldObjects.CreateSprite("CloudShadow", SpriteFactory.SoftCircle, new Vector2(rng.Next(0, WorldLayout.Width), rng.Next(5, WorldLayout.Height - 5)), transform, 150);
                sr.color = new Color(0.1f, 0.12f, 0.2f, 0f);
                sr.transform.localScale = new Vector3(14f + rng.Next(0, 8), 8f + rng.Next(0, 4), 1f);
                clouds.Add(sr);
            }
            var park = WorldLayout.Park;
            for (int i = 0; i < 26; i++)
            {
                var home = new Vector2(park.x + 1 + (float)rng.NextDouble() * (park.width - 2), park.y + 1 + (float)rng.NextDouble() * (park.height - 2));
                var sr = WorldObjects.CreateSprite("Firefly", SpriteFactory.SoftCircle, home, transform, 160);
                sr.transform.localScale = Vector3.one * 0.22f;
                fireflies.Add((sr, home, (float)rng.NextDouble() * 10f));
            }
            foreach (var t in WorldLayout.Trees)
            {
                if ((t.x + t.y) % 2 == 0) continue;   // only the gulmohar trees (the red-flowered variant)
                for (int i = 0; i < 2; i++)
                {
                    var sr = WorldObjects.CreateSprite("Petal", SpriteFactory.Petal, WorldLayout.CellCenter(t), transform, 31);
                    petals.Add((sr, WorldLayout.CellCenter(t) + new Vector2(0, 1.6f), (float)rng.NextDouble(), 0.15f + (float)rng.NextDouble() * 0.15f));
                }
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld || gm.Clock == null) return;
            float hour = gm.Clock.HourFloat;
            float dt = Time.deltaTime;
            bool night = hour >= 19.3f;

            // clouds: drift east, visible as soft shade only in daylight
            float cloudAlpha = Mathf.Clamp01((18.5f - hour) / 1.5f) * 0.16f;
            foreach (var c in clouds)
            {
                var p = c.transform.position;
                p.x += dt * 0.6f;
                if (p.x > WorldLayout.Width + 12f) p.x = -12f;
                c.transform.position = p;
                c.color = new Color(0.1f, 0.12f, 0.2f, cloudAlpha);
            }

            // fireflies: wander and pulse after dark
            float t = Time.time;
            for (int i = 0; i < fireflies.Count; i++)
            {
                var (sr, home, phase) = fireflies[i];
                sr.enabled = night;
                if (!night) continue;
                sr.transform.position = home + new Vector2(Mathf.Sin(t * 0.7f + phase) * 1.2f, Mathf.Cos(t * 0.9f + phase * 1.3f) * 0.8f);
                float glow = 0.4f + 0.6f * Mathf.Max(0f, Mathf.Sin(t * 2.2f + phase * 3f));
                sr.color = new Color(1.6f * glow, 2f * glow, 0.6f * glow, glow);
            }

            // petals: fall and drift from the gulmohar trees
            for (int i = 0; i < petals.Count; i++)
            {
                var (sr, origin, life, speed) = petals[i];
                life += dt * speed;
                if (life > 1f) life -= 1f;
                petals[i] = (sr, origin, life, speed);
                sr.transform.position = origin + new Vector2(Mathf.Sin(life * 9f + i) * 0.5f + life * 0.8f, -life * 2.2f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Sin(life * Mathf.PI));
                sr.transform.localRotation = Quaternion.Euler(0, 0, life * 540f + i * 40f);
            }
        }
    }
}
