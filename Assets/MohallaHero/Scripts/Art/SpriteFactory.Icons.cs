using UnityEngine;

namespace MohallaHero
{
    /// <summary>UI icons: stars, karma stats, phone apps, items, waste, bins, ballot symbols and the inked finger.</summary>
    public static partial class SpriteFactory
    {
        static Sprite Icon(string key, int size, System.Action<PixelCanvas> draw, bool outline = true) => Cached("icon_" + key, () =>
        {
            var c = new PixelCanvas(size, size, key.GetHashCode());
            draw(c);
            if (outline) c.Outline(Ink);
            return c.ToSprite(Center);
        });

        public static Sprite StarIcon(bool filled) => Icon("star" + filled, 16, c =>
        {
            Color fill = filled ? Hex("#ffc20e") : new Color(1, 1, 1, 0.18f);
            Vector2 center = new Vector2(8, 7.6f);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float a = Mathf.Atan2(p.y, p.x) - Mathf.PI / 2f;
                    float r = p.magnitude;
                    float k = Mathf.Cos(5f * a);
                    float radius = Mathf.Lerp(3.3f, 7.4f, (k + 1f) / 2f);
                    if (r <= radius) c.Set(x, y, fill);
                }
        }, filled);

        public static Sprite StatIcon(KarmaStat s) => Icon("stat" + s, 16, c =>
        {
            switch (s)
            {
                case KarmaStat.Civic:   // a little building with a heart
                    c.Rect(3, 1, 10, 9, Hex("#ffffff"));
                    c.Rect(2, 10, 12, 2, Hex("#ffffff"));
                    for (int i = 0; i < 4; i++) c.Rect(4 + i, 12 + i, 8 - i * 2, 1, Hex("#ffffff"));
                    c.Rect(7, 1, 2, 4, Hex("#00a6a6"));
                    c.Rect(4, 6, 2, 2, Hex("#00a6a6")); c.Rect(10, 6, 2, 2, Hex("#00a6a6"));
                    break;
                case KarmaStat.Green:   // leaf
                    for (int y = 2; y < 15; y++)
                    {
                        float t = (y - 2) / 12f;
                        int half = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 5.5f);
                        for (int x = 8 - half; x <= 8 + half; x++) c.Set(x, y, Hex("#5ce65c"));
                    }
                    c.Line(8, 1, 8, 13, Hex("#1e7b1e"));
                    c.Line(8, 7, 11, 10, Hex("#1e7b1e")); c.Line(8, 5, 5, 8, Hex("#1e7b1e"));
                    break;
                case KarmaStat.Courage: // shield
                    for (int y = 1; y < 15; y++)
                    {
                        int half = y > 6 ? 6 : Mathf.Max(1, y - 1);
                        for (int x = 8 - half; x < 8 + half; x++) c.Set(x, y, Hex("#ff7f11"));
                    }
                    c.Rect(7, 4, 2, 8, Hex("#fff3b0")); c.Rect(5, 8, 6, 2, Hex("#fff3b0"));
                    break;
                default:                // balance scale
                    c.Rect(7, 2, 2, 11, Hex("#ffffff"));
                    c.Rect(3, 12, 10, 2, Hex("#ffffff"));
                    c.Rect(4, 2, 8, 1, Hex("#ffffff"));
                    c.Ellipse(4, 6, 3, 1.5f, Hex("#ffd23f")); c.Ellipse(12, 6, 3, 1.5f, Hex("#ffd23f"));
                    c.Line(4, 7, 4, 3, Hex("#ffffff")); c.Line(12, 7, 12, 3, Hex("#ffffff"));
                    break;
            }
        });

        public static Sprite AppIcon(string app) => Icon("app_" + app, 24, c =>
        {
            Color w = Hex("#ffffff");
            switch (app)
            {
                case "missions":
                    c.Rect(5, 2, 14, 18, w); c.Rect(9, 19, 6, 3, Hex("#c97c3b"));
                    for (int i = 0; i < 4; i++) { c.Rect(7, 5 + i * 4, 2, 2, Hex("#2e8b57")); c.Rect(10, 5 + i * 4, 7, 2, Hex("#555555")); }
                    break;
                case "karma":
                    c.Circle(12, 12, 9, Hex("#ffc20e"));
                    c.Rect(7, 9, 2, 3, Ink); c.Rect(15, 9, 2, 3, Ink);
                    c.Rect(8, 6, 8, 1, Ink); c.Set(7, 7, Ink); c.Set(16, 7, Ink);
                    break;
                case "evidence":
                    c.Rect(3, 4, 18, 14, Hex("#ffd23f")); c.Rect(3, 18, 8, 2, Hex("#ffd23f"));
                    c.Rect(6, 7, 12, 8, w); c.Rect(8, 9, 8, 1, Hex("#555555")); c.Rect(8, 11, 6, 1, Hex("#555555"));
                    break;
                case "trust":
                    c.Rect(2, 8, 9, 6, Hex("#c99572")); c.Rect(13, 8, 9, 6, Hex("#8d5524"));
                    c.Rect(8, 10, 8, 5, Hex("#c99572")); c.Rect(10, 12, 6, 3, Hex("#8d5524"));
                    c.Rect(2, 7, 4, 8, Hex("#00a6a6")); c.Rect(18, 7, 4, 8, Hex("#e5157a"));
                    break;
                case "map":
                    c.Rect(3, 4, 18, 16, Hex("#a8dadc"));
                    c.Line(3, 10, 21, 14, Hex("#ffffff")); c.Line(11, 4, 13, 20, Hex("#ffffff"));
                    c.Circle(15, 15, 3, Hex("#e63946")); c.Rect(14, 10, 2, 3, Hex("#e63946"));
                    break;
                case "hint":
                    c.Circle(12, 14, 7, Hex("#fff176"));
                    c.Rect(9, 3, 6, 4, Hex("#9e9e9e")); c.Rect(10, 2, 4, 1, Hex("#9e9e9e"));
                    c.Rect(11, 10, 2, 5, Hex("#ff7f11"));
                    break;
                case "settings":
                    c.Circle(12, 12, 8, w);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f;
                        c.Rect(Mathf.RoundToInt(12 + Mathf.Cos(a) * 9) - 2, Mathf.RoundToInt(12 + Mathf.Sin(a) * 9) - 2, 4, 4, w);
                    }
                    c.Circle(12, 12, 3.5f, Hex("#6d6875"));
                    break;
                case "phone":
                    c.Rect(6, 1, 12, 22, Hex("#2b2d42")); c.Rect(7, 4, 10, 15, Hex("#00a6a6"));
                    c.Rect(10, 2, 4, 1, Hex("#555555")); c.Circle(12, 20.5f, 1.2f, Hex("#555555"));
                    c.Rect(9, 7, 2, 2, w); c.Rect(13, 7, 2, 2, w); c.Rect(9, 11, 2, 2, w); c.Rect(13, 11, 2, 2, w);
                    break;
                case "save":
                    c.Rect(4, 3, 16, 18, Hex("#1d70b8")); c.Rect(7, 13, 10, 7, w); c.Rect(7, 4, 10, 6, Hex("#a8dadc"));
                    break;
                case "talk":
                    c.Ellipse(12, 13, 9, 7, w);
                    c.Line(7, 7, 4, 3, w); c.Line(8, 7, 5, 3, w);
                    c.Rect(8, 12, 2, 2, Ink); c.Rect(11, 12, 2, 2, Ink); c.Rect(14, 12, 2, 2, Ink);
                    break;
                case "run":
                    c.Circle(14, 19, 2.5f, w);
                    c.Line(13, 16, 10, 9, w); c.Line(12, 16, 9, 9, w);
                    c.Line(10, 9, 6, 3, w); c.Line(10, 9, 14, 3, w); c.Line(12, 14, 17, 12, w); c.Line(12, 14, 7, 13, w);
                    break;
            }
        });

        public static Sprite ItemIcon(string id) => Icon("item_" + id, 20, c =>
        {
            switch (id)
            {
                case "photo":
                    c.Rect(2, 4, 16, 12, Hex("#ffffff")); c.Rect(4, 7, 12, 7, Hex("#a8dadc"));
                    c.Rect(5, 7, 4, 3, Hex("#6f5e4b")); c.Rect(11, 7, 1, 5, Hex("#8a8f96")); c.Rect(13, 10, 3, 3, Hex("#ffc20e"));
                    break;
                case "receipt":
                    c.Rect(4, 1, 12, 18, Hex("#fff8e1"));
                    for (int y = 4; y < 16; y += 3) c.Rect(6, y, 8 - (y % 2) * 2, 1, Hex("#555555"));
                    c.Rect(11, 2, 4, 2, Hex("#ffc20e"));
                    break;
                case "rti":
                    c.Rect(3, 1, 14, 18, Hex("#ffffff")); c.Rect(3, 15, 14, 4, Hex("#1d70b8"));
                    for (int y = 4; y < 13; y += 3) c.Rect(5, y, 10, 1, Hex("#555555"));
                    c.Circle(13, 5, 2, Hex("#e63946"));
                    break;
                case "groceries":
                    c.Rect(3, 1, 14, 12, Hex("#c97c3b")); c.Rect(5, 12, 3, 6, Hex("#f4d35e")); c.Rect(9, 12, 4, 5, Hex("#8bc34a")); c.Rect(13, 12, 2, 4, Hex("#e63946"));
                    break;
                case "ration_card":
                    c.Rect(1, 4, 18, 12, Hex("#ffc20e")); c.Rect(3, 6, 5, 6, Hex("#ffffff")); c.Rect(10, 7, 7, 1, Hex("#555555")); c.Rect(10, 10, 6, 1, Hex("#555555"));
                    break;
                case "petition":
                    c.Rect(3, 1, 14, 18, Hex("#fff8e1"));
                    for (int y = 12; y < 17; y += 2) c.Rect(5, y, 10, 1, Hex("#555555"));
                    c.Line(5, 4, 9, 8, Hex("#1d70b8")); c.Line(9, 8, 12, 4, Hex("#1d70b8")); c.Line(12, 4, 15, 7, Hex("#1d70b8"));
                    break;
                case "voter_slip":
                    c.Rect(2, 4, 16, 12, Hex("#ffffff")); c.Rect(2, 13, 16, 3, Hex("#1d70b8")); c.Rect(4, 6, 4, 5, Hex("#a8dadc")); c.Rect(10, 8, 6, 1, Hex("#555555"));
                    break;
                default:
                    c.Rect(4, 4, 12, 12, Hex("#9e9e9e"));
                    break;
            }
        });

        public static Sprite WasteIcon(string key) => Icon("waste_" + key, 24, c =>
        {
            switch (key)
            {
                case "peel": c.Ellipse(12, 10, 8, 3, Hex("#ffe135")); c.Line(4, 10, 2, 16, Hex("#e6c200")); c.Line(20, 10, 22, 16, Hex("#e6c200")); c.Rect(11, 12, 2, 4, Hex("#8b5a2b")); break;
                case "food": c.Ellipse(12, 8, 10, 4, Hex("#ffffff")); c.Ellipse(12, 10, 7, 3, Hex("#f4a261")); c.Speckle(Hex("#ffffff"), 12); break;
                case "tea": c.Ellipse(12, 7, 8, 5, Hex("#5c4033")); c.Speckle(Hex("#3b2a1a"), 20); break;
                case "flower": for (int i = 0; i < 3; i++) c.Circle(7 + i * 5, 10 + (i % 2) * 3, 3.5f, Hex("#ff9933")); c.Line(10, 2, 12, 8, Hex("#3b9440")); break;
                case "bottle": c.Rect(9, 2, 6, 16, Hex("#9fd3ff")); c.Rect(10, 18, 4, 3, Hex("#1d70b8")); c.Rect(9, 8, 6, 4, Hex("#e63946")); break;
                case "paper": c.Rect(3, 4, 18, 14, Hex("#efe6d2")); for (int y = 6; y < 16; y += 2) c.Rect(5, y, 14, 1, Hex("#8a8a8a")); c.Rect(5, 13, 6, 4, Hex("#555555")); break;
                case "chips": c.Rect(6, 3, 12, 17, Hex("#e63946")); c.Rect(6, 19, 12, 2, Hex("#c1121f")); c.Circle(12, 11, 3.5f, Hex("#ffc20e")); break;
                case "box": c.Rect(4, 4, 16, 12, Hex("#c97c3b")); c.Rect(4, 14, 16, 2, Hex("#a0612c")); c.Rect(11, 4, 2, 12, Hex("#e6c08a")); break;
                case "jar": c.Rect(6, 3, 12, 14, Hex("#bde0fe")); c.Rect(7, 17, 10, 3, Hex("#6d6875")); c.Rect(8, 5, 2, 9, Hex("#ffffff")); break;
                case "battery": c.Rect(8, 3, 8, 16, Hex("#2b2d42")); c.Rect(8, 13, 8, 6, Hex("#ffb000")); c.Rect(10, 19, 4, 2, Hex("#9e9e9e")); break;
                case "bulb": c.Circle(12, 13, 6, Hex("#fff8e1")); c.Rect(9, 3, 6, 5, Hex("#9e9e9e")); c.Line(8, 15, 15, 10, Hex("#555555")); break;
                case "medicine": c.Rect(4, 6, 16, 10, Hex("#ffffff")); for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) c.Circle(7.5f + i * 4.5f, 9 + j * 4, 1.5f, Hex("#e63946")); break;
                case "charger": c.Rect(7, 12, 10, 8, Hex("#f1f1f1")); c.Rect(9, 20, 2, 3, Hex("#9e9e9e")); c.Rect(13, 20, 2, 3, Hex("#9e9e9e")); c.Line(12, 12, 6, 2, Hex("#2b2d42")); break;
                case "paint": c.Rect(5, 3, 14, 14, Hex("#9e9e9e")); c.Rect(5, 13, 14, 4, Hex("#7b2ff7")); c.Rect(9, 17, 6, 3, Hex("#555555")); break;
                default: c.Rect(6, 6, 12, 12, Hex("#9e9e9e")); break;
            }
        });

        public static Sprite BinIcon(WasteBin b) => Icon("bin_" + b, 32, c =>
        {
            Color col = b == WasteBin.Wet ? Hex("#2e8b57") : b == WasteBin.Dry ? Hex("#1d70b8") : Hex("#d62828");
            c.Rect(6, 1, 20, 24, col);
            c.Rect(4, 24, 24, 4, Shade(col, 0.8f));
            c.Rect(13, 28, 6, 2, Shade(col, 0.6f));
            for (int x = 10; x < 24; x += 5) c.Rect(x, 4, 2, 17, Shade(col, 0.85f));
            // symbol: leaf / recycle arrows / skull-ish warning
            if (b == WasteBin.Wet) c.Circle(16, 13, 4, Hex("#9ef01a"));
            else if (b == WasteBin.Dry) { c.Rect(12, 11, 8, 2, Hex("#ffffff")); c.Rect(12, 15, 8, 2, Hex("#ffffff")); }
            else { c.Rect(15, 10, 2, 7, Hex("#ffffff")); c.Rect(15, 7, 2, 2, Hex("#ffffff")); }
        });

        public static Sprite CandidateSymbol(Candidate cand) => Icon("cand_" + cand, 32, c =>
        {
            switch (cand)
            {
                case Candidate.Jugaad:   // rocket
                    c.Rect(13, 6, 6, 18, Hex("#f1f1f1"));
                    for (int i = 0; i < 5; i++) c.Rect(14 + i / 3, 24 + i, 4 - (i / 3) * 2, 1, Hex("#e63946"));
                    c.Rect(9, 6, 4, 6, Hex("#6a2c91")); c.Rect(19, 6, 4, 6, Hex("#6a2c91"));
                    c.Circle(16, 17, 2, Hex("#9fd3ff"));
                    c.Rect(14, 2, 4, 4, Hex("#ffb000"));
                    break;
                case Candidate.Vaada:    // see-saw
                    c.Line(3, 10, 29, 20, Hex("#2b9bb3")); c.Line(3, 11, 29, 21, Hex("#2b9bb3"));
                    for (int i = 0; i < 6; i++) c.Rect(13 + i / 2, 4 + i, 6 - i, 1, Hex("#ffd23f"));
                    c.Circle(5, 14, 3, Hex("#e63946")); c.Circle(27, 24, 3, Hex("#00a6a6"));
                    break;
                case Candidate.Meera:    // lantern
                    c.Rect(10, 6, 12, 16, Hex("#ffd23f")); c.Rect(12, 8, 8, 12, Hex("#fff3b0"));
                    c.Rect(9, 4, 14, 2, Hex("#5a5f66")); c.Rect(9, 22, 14, 2, Hex("#5a5f66"));
                    c.Rect(15, 24, 2, 4, Hex("#5a5f66")); c.Rect(12, 28, 8, 1, Hex("#5a5f66"));
                    c.Ellipse(16, 13, 2, 3, Hex("#ff7f11"));
                    break;
                default:                 // NOTA: a cross in a box
                    c.RectOutline(5, 5, 22, 22, Hex("#ffffff")); c.RectOutline(6, 6, 20, 20, Hex("#ffffff"));
                    c.Line(9, 9, 22, 22, Hex("#e63946")); c.Line(22, 9, 9, 22, Hex("#e63946"));
                    c.Line(10, 9, 23, 22, Hex("#e63946")); c.Line(23, 9, 10, 22, Hex("#e63946"));
                    break;
            }
        });

        /// <summary>
        /// A left hand seen from the back with the index finger raised at its edge and the thumb folded across,
        /// the way polling officers mark the finger. The ink line is drawn by the UI over x = 10..18.
        /// </summary>
        public static Sprite Finger => Cached("finger", () =>
        {
            var c = new PixelCanvas(48, 64, 50);
            Color skin = Hex("#c99572"), dark = Hex("#a97452"), nail = Hex("#f2d0b8");
            // index finger, raised at the left edge of the hand
            c.Rect(8, 24, 12, 32, skin);
            c.Ellipse(14, 56, 6, 5, skin);
            c.Rect(10, 51, 8, 6, nail);
            c.Ellipse(14, 57, 4, 3, nail);
            c.Line(9, 40, 19, 40, dark);
            c.Line(9, 32, 19, 32, dark);
            // the other three fingers curled into the palm, stepping down to the right
            c.Rect(20, 16, 10, 12, skin); c.Rect(30, 14, 9, 12, skin); c.Rect(39, 12, 7, 11, skin);
            c.Line(20, 27, 29, 27, dark); c.Line(30, 25, 38, 25, dark); c.Line(39, 22, 45, 22, dark);
            c.Line(29, 16, 29, 27, dark); c.Line(38, 14, 38, 25, dark);
            // back of the hand and the wrist
            c.Rect(8, 0, 36, 18, skin);
            c.Ellipse(26, 16, 18, 5, skin);
            // thumb folded across the front of the curled fingers
            c.Ellipse(26, 15, 13, 4, Hex("#d6a37f"));
            c.Line(14, 13, 38, 13, dark);
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 0f), 16);
        });

        /// <summary>A bold arrow pointing up (rotated by the HUD to point at off-screen objectives).</summary>
        public static Sprite EdgeArrow => Icon("edgearrow", 24, c =>
        {
            for (int y = 2; y < 22; y++)
            {
                int half = (22 - y) / 2;
                for (int x = 12 - half; x <= 11 + half; x++) c.Set(x, y, y > 12 ? Hex("#ffd84d") : Hex("#ffb000"));
            }
        });

        public static Sprite Silhouette => Cached("silhouette", () =>
        {
            var c = new PixelCanvas(16, 24, 51);
            c.Rect(4, 0, 8, 15, Hex("#6d6875"));
            c.Rect(4, 15, 8, 7, Hex("#8d8699"));
            c.Rect(4, 20, 8, 3, Hex("#4a4458"));
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 0.02f));
        });
    }
}
