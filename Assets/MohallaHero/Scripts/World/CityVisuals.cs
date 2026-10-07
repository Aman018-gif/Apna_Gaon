using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// "The world reacts": redraws Shanti Nagar from the Mohalla Rating and story state.
    /// Low rating: dim buildings, graffiti and party posters, potholes, broken street lights (plus the camera's gloom tint).
    /// High rating: bright facades, murals, bunting over the streets, every street light working.
    /// </summary>
    public class CityVisuals : MonoBehaviour
    {
        WorldBuilder b;
        float timer;
        int lastRating = -1;

        public void Init(WorldBuilder builder)
        {
            b = builder;
            GameEvents.RatingChanged += Rebuild;
            GameEvents.AreaUnlocked += _ => Rebuild();
            GameEvents.VarChanged += (name, _) =>
            {
                if (name.StartsWith("obj_") || name.StartsWith("m6_") || name == "park_fixed") Rebuild();
            };
            Rebuild();
        }

        public void Rebuild()
        {
            var gm = GameManager.I;
            if (gm == null || b == null) return;
            int rating = gm.Rating;
            lastRating = rating;
            bool bright = rating >= 45;

            foreach (var def in WorldLayout.Buildings)
                if (b.BuildingSprites.TryGetValue(def.id, out var sr)) sr.sprite = SpriteFactory.Building(def, bright);

            bool murals = rating >= 60;
            foreach (var p in b.PosterWalls) p.sprite = SpriteFactory.PosterWall(murals);

            bool festive = rating >= 70 || gm.Story.Is("m6_started");
            foreach (var go in b.Bunting) go.SetActive(festive);

            for (int i = 0; i < b.Potholes.Count; i++) b.Potholes[i].SetActive(rating < 25 + i * 12);

            bool parkFixed = gm.Story.Is("park_fixed");
            foreach (var s in b.Swings) s.sprite = parkFixed ? SpriteFactory.Swing : SpriteFactory.BrokenSwing;

            b.MelaRoot.gameObject.SetActive(gm.Areas.IsUnlocked(AreaId.Maidan));
            foreach (var o in b.Objects) o.Refresh();
            UpdateLights();
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 1f;
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld) return;
            if (gm.Rating != lastRating) Rebuild();
            else UpdateLights();
        }

        /// <summary>Street lights glow after dark; with a low rating every other one is broken.</summary>
        void UpdateLights()
        {
            var gm = GameManager.I;
            bool dark = gm.Clock != null && gm.Clock.IsDark;
            bool working = gm.Rating >= 40;
            for (int i = 0; i < b.StreetLights.Count; i++)
            {
                bool ok = working || i % 2 == 0;
                b.StreetLights[i].sprite = SpriteFactory.StreetLight(ok && dark);
                b.LightGlows[i].On = ok && dark;
            }
        }
    }
}
