using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Generates and caches all of the game's sprites procedurally (16 px per world unit).
    /// Replacing any of these with hand-drawn art (see Docs/ASSET_LIST.md) only requires returning a different Sprite.
    /// Split into: this file (basics, ground tiles, characters, effects), SpriteFactory.City.cs (buildings and props)
    /// and SpriteFactory.Icons.cs (UI icons).
    /// </summary>
    public static partial class SpriteFactory
    {
        public const int PPU = 16;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Vector2 BottomLeft = Vector2.zero;
        public static readonly Color Ink = new Color(0.12f, 0.09f, 0.1f, 1f);

        /// <summary>
        /// Returns the cached sprite for a key, drawing it on first use. An imported sprite at
        /// <c>Resources/ArtOverrides/&lt;key&gt;</c> (e.g. <c>tile_Grass_0</c>, <c>bld_school_True</c>, <c>char_npc_Asha_0_1</c>)
        /// replaces the procedural one, so free pixel-art packs can be dropped in without code changes
        /// (see Docs/ASSET_LIST.md; Mohalla Hero → List Art Keys prints every key).
        /// </summary>
        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!cache.TryGetValue(key, out var s) || s == null)
            {
                s = Resources.Load<Sprite>("ArtOverrides/" + key);
                if (s == null)
                {
                    s = make();
                    s.name = key;
                }
                cache[key] = s;
            }
            return s;
        }

        /// <summary>Every sprite key drawn so far (for the art-override listing).</summary>
        public static IEnumerable<string> Keys => cache.Keys;

        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static Color Shade(Color c, float f) { var o = c * f; o.a = c.a; return o; }

        // ------------------------------------------------------------------ basics

        static Material spriteMaterial;

        /// <summary>A copy of the default sprite material (works in builds even if Shader.Find would be stripped).</summary>
        public static Material SpriteMaterial()
        {
            if (spriteMaterial == null)
            {
                var go = new GameObject("MaterialProbe");
                var sr = go.AddComponent<SpriteRenderer>();
                spriteMaterial = new Material(sr.sharedMaterial);
                Object.Destroy(go);
            }
            return new Material(spriteMaterial);
        }

        public static Sprite White => Cached("white", () =>
        {
            var c = new PixelCanvas(4, 4);
            c.Fill(Color.white);
            return c.ToSprite(Center, 4);
        });

        public static Sprite SoftCircle => Cached("softcircle", () =>
        {
            var c = new PixelCanvas(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16)) / 16f;
                    c.Set(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d)));
                }
            return c.ToSprite(Center, 16);
        });

        /// <summary>Rounded rectangle used for UI panels and buttons (9-sliced).</summary>
        public static Sprite RoundedRect => Cached("roundrect", () =>
        {
            const int S = 32, R = 10;
            var c = new PixelCanvas(S, S);
            for (int x = 0; x < S; x++)
                for (int y = 0; y < S; y++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(R - x - 0.5f, x + 0.5f - (S - R))), dy = Mathf.Max(0, Mathf.Max(R - y - 0.5f, y + 0.5f - (S - R)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    c.Set(x, y, new Color(1, 1, 1, Mathf.Clamp01(R - d + 0.5f)));
                }
            var tex = c.ToTexture();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, S, S), Center, 32, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        });

        public static Sprite Circle => Cached("circle", () =>
        {
            var c = new PixelCanvas(64, 64);
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 64; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                    c.Set(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - d)));
                }
            var tex = c.ToTexture();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), Center, 64);
        });

        public static Sprite Shadow => Cached("shadow", () =>
        {
            var c = new PixelCanvas(12, 5);
            c.Ellipse(6, 2.5f, 6, 2.5f, new Color(0, 0, 0, 0.28f));
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        // ------------------------------------------------------------------ ground tiles

        public static Sprite Ground(TileKind k, int variant)
        {
            return Cached($"tile_{k}_{variant}", () =>
            {
                var c = new PixelCanvas(16, 16, (int)k * 31 + variant * 7 + 1);
                switch (k)
                {
                    case TileKind.Asphalt:
                        c.Noise(Hex("#4b4f57"), 0.05f);
                        c.Speckle(Hex("#5d626b"), 8);
                        if (variant == 3) { c.Ellipse(8, 7, 3, 2, Hex("#3b3e45")); }   // the odd pothole
                        break;
                    case TileKind.LaneMark:
                        c.Noise(Hex("#4b4f57"), 0.05f);
                        c.Speckle(Hex("#5d626b"), 8);
                        for (int x = 2; x < 11; x++) { c.Set(x, 15, Hex("#f1f1e6")); c.Set(x, 14, Hex("#e6e6d8")); }
                        break;
                    case TileKind.Zebra:
                        c.Noise(Hex("#4b4f57"), 0.04f);
                        for (int y = 1; y < 16; y += 5)
                            for (int x = 1; x < 15; x++) { c.Set(x, y, Hex("#f4f4ea")); c.Set(x, y + 1, Hex("#f4f4ea")); c.Set(x, y + 2, Hex("#e8e8dc")); }
                        break;
                    case TileKind.Sidewalk:
                        c.Noise(Hex("#cfc8bb"), 0.04f);
                        for (int i = 0; i < 16; i++) { c.Set(i, 0, Hex("#aea696")); c.Set(0, i, Hex("#aea696")); c.Set(i, 8, Hex("#bdb5a6")); }
                        c.Speckle(Hex("#b8b0a0"), 6);
                        break;
                    case TileKind.Plaza:
                        c.Noise(Hex("#d9cfbf"), 0.04f);
                        for (int i = 0; i < 16; i += 8) for (int j = 0; j < 16; j++) { c.Set(i, j, Hex("#c4b9a6")); c.Set(j, i, Hex("#c4b9a6")); }
                        c.Speckle(Hex("#cbbfab"), 5);
                        break;
                    case TileKind.Grass:
                        c.Noise(Hex("#5fae4f"), 0.07f);
                        GrassBlades(c, Hex("#47913c"), 5);
                        break;
                    case TileKind.Flowers:
                        c.Noise(Hex("#5fae4f"), 0.07f);
                        GrassBlades(c, Hex("#47913c"), 3);
                        var petals = new[] { Hex("#ffc20e"), Hex("#ff6fa8"), Color.white, Hex("#ff7f11") };
                        for (int i = 0; i < 3; i++)
                        {
                            int x = c.Rand(2, 14), y = c.Rand(2, 14);
                            var p = petals[c.Rand(0, petals.Length)];
                            c.Set(x, y, p); c.Set(x + 1, y, p); c.Set(x - 1, y, p); c.Set(x, y + 1, p); c.Set(x, y - 1, p);
                            c.Set(x, y, Hex("#7a3d00"));
                        }
                        break;
                    case TileKind.ParkPath:
                        c.Noise(Hex("#c98b62"), 0.05f);
                        for (int y = 0; y < 16; y += 4)
                            for (int x = 0; x < 16; x++) c.Set(x, y, Hex("#a96f4a"));
                        for (int y = 0; y < 16; y++) c.Set(((y / 4) % 2) * 4 + 2, y, Hex("#a96f4a"));
                        break;
                    case TileKind.Ground:
                        c.Noise(Hex("#d8b680"), 0.06f);
                        c.Speckle(Hex("#c19d68"), 10);
                        break;
                    case TileKind.Water:
                        c.Noise(Hex("#3d8fd1"), 0.05f);
                        for (int i = 0; i < 3; i++)
                        {
                            int x = c.Rand(1, 12), y = c.Rand(2, 14);
                            for (int j = 0; j < 4; j++) c.Set(x + j, y, Hex("#9fd0f5"));
                        }
                        break;
                    case TileKind.Wall:
                        c.Fill(Hex("#b5603e"));
                        for (int y = 0; y < 16; y += 4)
                        {
                            for (int x = 0; x < 16; x++) c.Set(x, y, Hex("#8e4529"));
                            for (int x = (y / 4 % 2) * 4; x < 16; x += 8) for (int j = 0; j < 4; j++) c.Set(x, y + j, Hex("#8e4529"));
                        }
                        c.Rect(0, 13, 16, 3, Hex("#d9d2c5"));
                        break;
                    case TileKind.Hedge:
                        c.Fill(Hex("#2f7d32"));
                        c.Circle(4, 5, 5, Hex("#3b9440"));
                        c.Circle(12, 10, 5, Hex("#3b9440"));
                        c.Circle(12, 3, 3, Hex("#2a6b2d"));
                        c.Speckle(Hex("#6cc26f"), 7);
                        break;
                    default:
                        c.Fill(Color.magenta);
                        break;
                }
                return c.ToSprite(Center);
            });
        }

        static void GrassBlades(PixelCanvas c, Color col, int n)
        {
            for (int i = 0; i < n; i++)
            {
                int x = c.Rand(0, 16), y = c.Rand(0, 15);
                c.Set(x, y, col);
                c.Set(x, y + 1, col);
            }
        }

        // ------------------------------------------------------------------ characters

        /// <summary>dir: 0 = down (front), 1 = up (back), 2 = side (facing right; flip for left). frame 0/1 for walking.</summary>
        public static Sprite Character(NpcDef d, int dir, int frame) =>
            Character(d.skin, d.shirt, d.pants, d.hair, d.accent, d.female, d.outfit, dir, frame, "npc_" + d.id);

        public static Sprite Player(Gender g, int dir, int frame) =>
            g == Gender.Female
                ? Character(Hex("#c99572"), Hex("#ff7f11"), Hex("#3a2e6e"), Hex("#1a1210"), Hex("#ffc20e"), true, Outfit.Plain, dir, frame, "player_f")
                : Character(Hex("#c99572"), Hex("#00a6a6"), Hex("#2b2d42"), Hex("#1a1210"), Hex("#ffc20e"), false, Outfit.Plain, dir, frame, "player_m");

        public static Sprite Character(Color skin, Color shirt, Color pants, Color hair, Color accent, bool female, Outfit outfit, int dir, int frame, string key)
        {
            return Cached($"char_{key}_{dir}_{frame}", () =>
            {
                var c = new PixelCanvas(16, 24, key.GetHashCode());
                int leg = frame == 1 ? 1 : 0;
                Color dark = Shade(pants, 0.8f);
                bool longGarment = female || outfit == Outfit.Kurta || outfit == Outfit.Saree || outfit == Outfit.Coat;

                // legs / lower garment
                if (outfit == Outfit.Saree)
                {
                    c.Rect(4, 0, 8, 10, pants);
                    for (int y = 0; y < 10; y += 3) c.Rect(4, y, 8, 1, Shade(pants, 0.85f));
                }
                else if (female)
                {
                    c.Rect(4, 1, 8, 9, pants);
                    c.Rect(5, 0, 2, 1, Shade(skin, 0.8f)); c.Rect(9, 0, 2, 1, Shade(skin, 0.8f));
                }
                else
                {
                    c.Rect(5, leg, 2, 8 - leg, pants);
                    c.Rect(9, 1 - leg, 2, 7 + leg, pants);
                    c.Rect(5, leg, 2, 1, Hex("#2a1f18")); c.Rect(9, 0, 2, 1, Hex("#2a1f18"));
                }
                // torso
                int torsoBottom = longGarment && outfit != Outfit.Saree && !female ? 4 : 7;
                c.Rect(4, torsoBottom, 8, 15 - torsoBottom, shirt);
                c.Rect(4, 7, 8, 1, Shade(shirt, 0.82f));
                // outfit details
                switch (outfit)
                {
                    case Outfit.NetaJacket:
                        c.Rect(4, 7, 3, 8, accent); c.Rect(9, 7, 3, 8, accent);           // sleeveless jacket
                        c.Set(8, 12, Hex("#ffd23f")); c.Set(8, 10, Hex("#ffd23f"));        // buttons
                        break;
                    case Outfit.Suit:
                        c.Rect(7, 8, 2, 7, Hex("#f2f2f2")); c.Rect(7, 9, 2, 5, accent);   // shirt + tie
                        break;
                    case Outfit.Saree:
                        for (int i = 0; i < 8; i++) c.Rect(4 + i, 7 + i, 2, 1, accent);    // pallu
                        break;
                    case Outfit.Coat:
                        c.Rect(7, 7, 2, 8, accent);                                         // coat opening / scrubs
                        if (dir == 0) { c.Set(5, 12, Hex("#e63946")); c.Set(6, 12, Hex("#e63946")); }
                        break;
                    case Outfit.Uniform:
                        c.Rect(4, 13, 8, 1, accent);                                        // collar stripe
                        break;
                    case Outfit.Kurta:
                        c.Rect(7, 9, 2, 5, Shade(shirt, 0.9f));
                        c.Rect(4, 4, 8, 1, accent);
                        break;
                }
                // arms
                Color sleeve = Shade(shirt, 0.9f);
                if (dir == 2)
                {
                    c.Rect(7, 8 + leg, 3, 6, sleeve);
                    c.Rect(8, 7 + leg, 2, 2, skin);
                }
                else
                {
                    c.Rect(2, 8 + leg, 2, 6, sleeve);
                    c.Rect(12, 9 - leg, 2, 6, sleeve);
                    c.Rect(2, 8 + leg, 2, 1, skin); c.Rect(12, 9 - leg, 2, 1, skin);
                }
                // head
                c.Rect(4, 15, 8, 7, skin);
                if (dir == 1)
                {
                    c.Rect(4, 15, 8, 8, hair);
                    if (female) c.Rect(5, 10, 6, 6, hair);
                }
                else
                {
                    c.Rect(4, 20, 8, 3, hair);
                    if (dir == 2) c.Rect(4, 16, 3, 5, hair);
                    else { c.Rect(4, 18, 1, 3, hair); c.Rect(11, 18, 1, 3, hair); }
                    if (female) { c.Rect(3, 13, 2, 9, hair); if (dir != 2) c.Rect(11, 13, 2, 9, hair); }
                    if (dir == 0)
                    {
                        c.Set(6, 18, Ink); c.Set(9, 18, Ink);
                        c.Set(7, 16, Shade(skin, 0.72f)); c.Set(8, 16, Shade(skin, 0.72f));
                        if (female) c.Set(7, 19, Hex("#e5157a"));
                        if (outfit == Outfit.Suit) { c.Rect(5, 18, 2, 1, Hex("#9fd3ff")); c.Rect(8, 18, 3, 1, Hex("#9fd3ff")); }   // glasses
                    }
                    else c.Set(10, 18, Ink);
                }
                // simple shading: light from the top-left
                for (int y = 0; y < 22; y++)
                {
                    var px = c.Get(11, y);
                    if (px.a > 0.5f && dir != 1) c.Set(11, y, new Color(px.r * 0.82f, px.g * 0.82f, px.b * 0.82f, 1f));
                    var left = c.Get(4, y);
                    if (left.a > 0.5f && y > 2 && y < 20) c.Set(4, y, new Color(Mathf.Min(1f, left.r * 1.1f), Mathf.Min(1f, left.g * 1.1f), Mathf.Min(1f, left.b * 1.1f), 1f));
                }
                if (dir != 1) { c.Set(6, 22, Color.Lerp(hair, Color.white, 0.35f)); c.Set(7, 22, Color.Lerp(hair, Color.white, 0.25f)); }
                if (outfit == Outfit.Cap)
                {
                    c.Rect(4, 21, 8, 2, accent);
                    if (dir == 0) c.Rect(3, 21, 10, 1, Shade(accent, 0.8f));
                    else if (dir == 2) c.Rect(11, 21, 3, 1, Shade(accent, 0.8f));
                }
                c.Outline(Ink);
                return c.ToSprite(new Vector2(0.5f, 0.02f));
            });
        }

        // ------------------------------------------------------------------ effects & markers

        public static Sprite Bubble(string kind) => Cached("bubble_" + kind, () =>
        {
            var c = new PixelCanvas(12, 14, 3);
            Color bg = kind == "!" ? Hex("#ffc20e") : kind == "?" ? Hex("#5ce65c") : kind == "z" ? Hex("#c9d6ff") : Hex("#ffffff");
            c.Ellipse(6, 8, 6, 6, bg);
            c.Set(5, 1, bg); c.Set(6, 1, bg); c.Set(6, 2, bg); c.Set(5, 2, bg);
            switch (kind)
            {
                case "!": c.Rect(5, 7, 2, 6, Ink); c.Rect(5, 4, 2, 2, Ink); break;
                case "?": c.Rect(4, 11, 4, 1, Ink); c.Rect(7, 9, 1, 2, Ink); c.Rect(5, 8, 2, 1, Ink); c.Rect(5, 7, 1, 1, Ink); c.Rect(5, 4, 1, 2, Ink); break;
                case "z": c.Rect(4, 11, 5, 1, Ink); c.Set(7, 10, Ink); c.Set(6, 9, Ink); c.Set(5, 8, Ink); c.Rect(4, 7, 5, 1, Ink); break;
                case "talk": c.Rect(3, 8, 2, 2, Ink); c.Rect(6, 8, 2, 2, Ink); c.Rect(9, 8, 1, 2, Ink); break;
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        /// <summary>A small golden diamond floating above the next objective.</summary>
        public static Sprite ObjectiveArrow => Cached("objective", () =>
        {
            var c = new PixelCanvas(11, 13, 4);
            for (int y = 0; y < 13; y++)
            {
                int half = y < 6 ? y : 12 - y;
                for (int x = 5 - half; x <= 5 + half; x++) c.Set(x, y, y < 6 ? Hex("#ffb000") : Hex("#ffd84d"));
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Sparkle => Cached("sparkle", () =>
        {
            var c = new PixelCanvas(7, 7, 5);
            c.Rect(3, 0, 1, 7, Color.white); c.Rect(0, 3, 7, 1, Color.white);
            c.Set(2, 2, new Color(1, 1, 1, 0.6f)); c.Set(4, 4, new Color(1, 1, 1, 0.6f)); c.Set(2, 4, new Color(1, 1, 1, 0.6f)); c.Set(4, 2, new Color(1, 1, 1, 0.6f));
            return c.ToSprite(Center);
        });

        public static Sprite Drop => Cached("drop", () =>
        {
            var c = new PixelCanvas(3, 4, 6);
            c.Rect(1, 0, 1, 4, Hex("#7cc3ff")); c.Rect(0, 0, 3, 2, Hex("#7cc3ff"));
            return c.ToSprite(Center);
        });

        public static Sprite Smoke => Cached("smoke", () =>
        {
            var c = new PixelCanvas(10, 10, 7);
            c.Circle(5, 5, 4.5f, new Color(0.45f, 0.45f, 0.45f, 0.55f));
            return c.ToSprite(Center);
        });

        public static Sprite Fly => Cached("fly", () =>
        {
            var c = new PixelCanvas(3, 2, 8);
            c.Set(1, 0, Ink); c.Set(0, 1, new Color(1, 1, 1, 0.7f)); c.Set(2, 1, new Color(1, 1, 1, 0.7f));
            return c.ToSprite(Center);
        });
    }
}
