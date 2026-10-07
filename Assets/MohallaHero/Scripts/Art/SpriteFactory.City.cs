using UnityEngine;

namespace MohallaHero
{
    /// <summary>City buildings and street props, drawn in a 3/4 top-down view: front wall at the bottom, roof above.</summary>
    public static partial class SpriteFactory
    {
        /// <summary>Extra tiles a building rises above its footprint.</summary>
        public static int ExtraHeight(BuildingStyle s)
        {
            switch (s)
            {
                case BuildingStyle.Apartment: return 4;
                case BuildingStyle.School: case BuildingStyle.Office: return 3;
                case BuildingStyle.TeaStall: case BuildingStyle.Stage: return 1;
                default: return 2;
            }
        }

        public static Sprite Building(BuildingDef b, bool bright)
        {
            return Cached($"bld_{b.id}_{bright}", () =>
            {
                int w = b.rect.width, h = b.rect.height, extra = ExtraHeight(b.style);
                int pw = w * 16, ph = (h + extra) * 16;
                int facadeH = (extra + 1) * 16;           // vertical wall facing the street
                var c = new PixelCanvas(pw, ph, b.id.GetHashCode());
                Color wall, roof, trim;
                StyleColors(b, out wall, out roof, out trim);
                if (!bright) { wall = Color.Lerp(wall, Hex("#8d877d"), 0.35f); trim = Color.Lerp(trim, Hex("#6b665e"), 0.35f); }

                // roof (seen from above)
                c.Rect(0, facadeH, pw, ph - facadeH, roof);
                c.Speckle(Shade(roof, 0.9f), pw);
                c.Rect(0, facadeH, pw, 2, Shade(roof, 0.75f));
                c.RectOutline(0, facadeH, pw, ph - facadeH, Shade(roof, 0.7f));
                // facade
                c.Rect(1, 0, pw - 2, facadeH, wall);
                c.Rect(1, facadeH - 3, pw - 2, 3, trim);
                c.Rect(1, 0, pw - 2, 2, Shade(wall, 0.7f));

                switch (b.style)
                {
                    case BuildingStyle.Apartment: Apartment(c, pw, facadeH, ph, bright); break;
                    case BuildingStyle.School: School(c, pw, facadeH, ph, bright); break;
                    case BuildingStyle.Office: Office(c, pw, facadeH, ph, Hex("#1d3557"), bright); break;
                    case BuildingStyle.Clinic: Clinic(c, pw, facadeH, ph, bright); break;
                    case BuildingStyle.Shop: Shop(c, pw, facadeH, Hex("#e76f51"), bright); break;
                    case BuildingStyle.RationOffice: Office(c, pw, facadeH, ph, Hex("#606c38"), bright); break;
                    case BuildingStyle.PartyOffice: PartyOffice(c, pw, facadeH, b.id.StartsWith("jugaad") ? Hex("#6a2c91") : Hex("#2b9bb3")); break;
                    case BuildingStyle.TeaStall: TeaStall(c, pw, facadeH, ph); break;
                    case BuildingStyle.CommunityHall: Hall(c, pw, facadeH, bright); break;
                    case BuildingStyle.Stage: StageSprite(c, pw, facadeH, ph); break;
                    case BuildingStyle.Booth: BoothTent(c, pw, facadeH, ph); break;
                    default: HouseFront(c, pw, facadeH, ph, b, bright); break;
                }
                c.Outline(Ink);
                return c.ToSprite(BottomLeft);
            });
        }

        static void StyleColors(BuildingDef b, out Color wall, out Color roof, out Color trim)
        {
            switch (b.style)
            {
                case BuildingStyle.Apartment: wall = Hex("#f2d6a2"); roof = Hex("#b9b3a8"); trim = Hex("#e07a5f"); return;
                case BuildingStyle.School: wall = Hex("#ffd166"); roof = Hex("#c44536"); trim = Hex("#ffffff"); return;
                case BuildingStyle.Office: wall = Hex("#e9e4d8"); roof = Hex("#9a9a9a"); trim = Hex("#1d3557"); return;
                case BuildingStyle.Clinic: wall = Hex("#f7f7f7"); roof = Hex("#a8dadc"); trim = Hex("#00a6a6"); return;
                case BuildingStyle.Shop: wall = Hex("#f4a261"); roof = Hex("#8d6e63"); trim = Hex("#e76f51"); return;
                case BuildingStyle.RationOffice: wall = Hex("#e9edc9"); roof = Hex("#8a8a7a"); trim = Hex("#606c38"); return;
                case BuildingStyle.PartyOffice: wall = Hex("#fdf6e3"); roof = Hex("#a0a0a0"); trim = b.id.StartsWith("jugaad") ? Hex("#6a2c91") : Hex("#2b9bb3"); return;
                case BuildingStyle.TeaStall: wall = Hex("#8b5a2b"); roof = Hex("#2d6cdf"); trim = Hex("#ffc20e"); return;
                case BuildingStyle.CommunityHall: wall = Hex("#ffe8d6"); roof = Hex("#cb997e"); trim = Hex("#e5157a"); return;
                case BuildingStyle.Stage: wall = Hex("#8b5a2b"); roof = Hex("#c1121f"); trim = Hex("#ffc20e"); return;
                case BuildingStyle.Booth: wall = Hex("#f8f9fa"); roof = Hex("#f8f9fa"); trim = Hex("#1d70b8"); return;
            }
            var walls = new[] { Hex("#f4a6a6"), Hex("#a8dadc"), Hex("#ffd6a5"), Hex("#caffbf"), Hex("#bdb2ff"), Hex("#ffc6ff"), Hex("#fdffb6") };
            int hsh = Mathf.Abs(b.id.GetHashCode());
            wall = walls[hsh % walls.Length];
            roof = Hex("#b0aaa0");
            trim = Shade(wall, 0.75f);
        }

        static Color WindowColor(bool lit) => lit ? Hex("#ffe08a") : Hex("#2f3e55");

        static void Window(PixelCanvas c, int x, int y, int w, int h, bool lit, bool curtain = false)
        {
            c.Rect(x, y, w, h, WindowColor(lit));
            c.RectOutline(x - 1, y - 1, w + 2, h + 2, Hex("#ece6d6"));
            c.Rect(x, y + h / 2, w, 1, Hex("#ece6d6"));
            if (curtain) c.Rect(x, y + h - 2, w, 2, Hex("#e5157a"));
        }

        static void Door(PixelCanvas c, int cx, int w, int h, Color color)
        {
            c.Rect(cx - w / 2, 0, w, h, color);
            c.RectOutline(cx - w / 2 - 1, 0, w + 2, h + 1, Shade(color, 0.6f));
            c.Set(cx + w / 2 - 2, h / 2, Hex("#ffd23f"));
        }

        static void WaterTank(PixelCanvas c, int x, int y)
        {
            c.Ellipse(x, y + 6, 6, 7, Hex("#1d1d1d"));
            c.Ellipse(x, y + 10, 6, 3, Hex("#333333"));
            c.Rect(x - 6, y + 5, 12, 1, Hex("#3a3a3a"));
        }

        static void Apartment(PixelCanvas c, int pw, int fh, int ph, bool bright)
        {
            int floors = 4;
            for (int f = 0; f < floors; f++)
            {
                int y = 18 + f * 15;
                if (y + 10 > fh - 4) break;
                for (int x = 8; x < pw - 14; x += 22)
                {
                    bool lit = bright && ((x / 22 + f) % 3 == 0);
                    Window(c, x, y, 10, 9, lit, (x / 22 + f) % 2 == 0);
                    // balcony rail + plants/clothes
                    c.Rect(x - 2, y - 2, 14, 1, Hex("#6d6875"));
                    if ((x + f) % 3 == 0) { c.Set(x + 2, y - 1, Hex("#2e8b57")); c.Set(x + 3, y - 1, Hex("#2e8b57")); }
                    if ((x + f) % 4 == 1) { c.Rect(x + 6, y + 3, 2, 4, Hex("#ff7f11")); }
                    // AC unit
                    if ((x / 22 + f) % 4 == 2) c.Rect(x + 12, y + 2, 5, 4, Hex("#f1f1f1"));
                }
            }
            Door(c, pw / 2, 14, 14, Hex("#6b4226"));
            c.Rect(pw / 2 - 18, 15, 36, 2, Hex("#e07a5f"));
            c.Rect(pw / 2 - 50, fh - 14, 100, 9, Hex("#e07a5f"));
            WaterTank(c, 20, fh + 8);
            WaterTank(c, pw - 22, fh + 6);
            // clothes line
            c.Line(40, fh + 26, pw - 50, fh + 26, Hex("#555555"));
            var cloth = new[] { Hex("#e63946"), Hex("#ffc20e"), Hex("#00a6a6"), Hex("#ffffff") };
            for (int i = 0; i < 6; i++) c.Rect(46 + i * 14, fh + 20, 6, 6, cloth[i % cloth.Length]);
            c.Circle(pw / 2 + 20, fh + 12, 5, Hex("#dddddd"));    // dish antenna
            c.Set(pw / 2 + 20, fh + 12, Hex("#555555"));
        }

        static void School(PixelCanvas c, int pw, int fh, int ph, bool bright)
        {
            for (int row = 0; row < 2; row++)
                for (int x = 10; x < pw - 16; x += 20)
                    if (Mathf.Abs(x + 5 - pw / 2) > 14) Window(c, x, 18 + row * 18, 11, 10, bright && (x + row) % 3 == 0);
            Door(c, pw / 2, 18, 16, Hex("#1d70b8"));
            // signboard + clock
            c.Rect(pw / 2 - 40, fh - 14, 80, 9, Hex("#1d70b8"));
            c.Circle(pw / 2, fh + 8, 7, Hex("#ffffff"));
            c.RectOutline(pw / 2 - 1, fh + 8, 1, 6, Ink);
            c.Line(pw / 2, fh + 8, pw / 2 + 4, fh + 8, Ink);
            // flag pole (a plain pennant)
            c.Rect(14, fh, 1, 22, Hex("#777777"));
            c.Rect(15, fh + 16, 9, 6, Hex("#ff9933"));
        }

        static void Office(PixelCanvas c, int pw, int fh, int ph, Color band, bool bright)
        {
            for (int x = 8; x < pw - 12; x += 16)
                if (Mathf.Abs(x + 4 - pw / 2) > 12) { Window(c, x, 20, 8, 10, bright && x % 3 == 0); Window(c, x, 38, 8, 8, false); }
            Door(c, pw / 2, 14, 16, Hex("#5c4033"));
            c.Rect(pw / 2 - 26, fh - 14, 52, 9, band);
            // pillars
            c.Rect(pw / 2 - 14, 0, 3, 18, Hex("#ffffff")); c.Rect(pw / 2 + 11, 0, 3, 18, Hex("#ffffff"));
        }

        static void Clinic(PixelCanvas c, int pw, int fh, int ph, bool bright)
        {
            Window(c, 8, 18, 12, 9, bright);
            Window(c, pw - 20, 18, 12, 9, bright);
            Door(c, pw / 2, 14, 15, Hex("#00a6a6"));
            // green plus sign (pharmacy/clinic)
            int cx = pw / 2, cy = fh - 6;
            c.Rect(cx - 6, cy - 2, 12, 5, Hex("#2a9d8f"));
            c.Rect(cx - 2, cy - 6, 5, 13, Hex("#2a9d8f"));
        }

        static void Shop(PixelCanvas c, int pw, int fh, Color sign, bool bright)
        {
            // open shutter with goods
            c.Rect(8, 2, pw - 16, 18, Hex("#3d2b1f"));
            var goods = new[] { Hex("#f4d35e"), Hex("#ee964b"), Hex("#f95738"), Hex("#0d3b66"), Hex("#faf0ca"), Hex("#8bc34a") };
            for (int y = 4; y < 18; y += 5)
                for (int x = 10; x < pw - 10; x += 4) c.Rect(x, y, 3, 3, goods[(x / 4 + y) % goods.Length]);
            c.Rect(6, 20, pw - 12, 4, Hex("#9e9e9e"));            // rolled-up shutter
            for (int x = 6; x < pw - 6; x += 2) c.Set(x, 21, Hex("#7a7a7a"));
            // signboard
            c.Rect(4, fh - 12, pw - 8, 10, sign);
            if (bright) for (int x = 6; x < pw - 6; x += 6) c.Set(x, fh - 2, Hex("#ffe08a"));
        }

        static void PartyOffice(PixelCanvas c, int pw, int fh, Color party)
        {
            Door(c, pw / 2, 14, 15, Hex("#5c4033"));
            Window(c, 10, 18, 12, 9, true);
            Window(c, pw - 22, 18, 12, 9, true);
            c.Rect(4, fh - 13, pw - 8, 10, party);
            // bunting
            for (int x = 2; x < pw - 2; x += 6)
            {
                c.Set(x, fh + 2, party); c.Set(x + 1, fh + 1, party); c.Set(x + 2, fh + 2, Hex("#ffd23f"));
            }
            // posters on the wall
            c.Rect(26, 6, 8, 10, Hex("#ffd23f")); c.Rect(27, 12, 6, 3, party);
            c.Rect(pw - 34, 6, 8, 10, Hex("#ffd23f")); c.Rect(pw - 33, 12, 6, 3, party);
        }

        static void TeaStall(PixelCanvas c, int pw, int fh, int ph)
        {
            // open counter, tarp roof, kettle and glasses
            c.Rect(2, 0, pw - 4, 14, Hex("#8b5a2b"));
            c.Rect(2, 12, pw - 4, 3, Hex("#6b4226"));
            c.Rect(10, 15, 10, 8, Hex("#9a9a9a"));                   // kettle
            c.Rect(12, 23, 6, 2, Hex("#7a7a7a"));
            c.Line(20, 19, 24, 22, Hex("#9a9a9a"));
            for (int i = 0; i < 5; i++) c.Rect(32 + i * 6, 15, 3, 5, new Color(0.95f, 0.85f, 0.6f, 0.9f));
            c.Rect(0, fh - 6, pw, 6, Hex("#2d6cdf"));
            for (int x = 0; x < pw; x += 8) c.Rect(x, fh - 6, 4, 6, Hex("#ffc20e"));
            c.Rect(pw - 18, 22, 12, 8, Hex("#ffffff"));               // "CHAI" board
            c.Rect(pw - 16, 25, 8, 2, Hex("#e63946"));
        }

        static void Hall(PixelCanvas c, int pw, int fh, bool bright)
        {
            for (int x = 10; x < pw - 14; x += 18)
                if (Mathf.Abs(x + 5 - pw / 2) > 14) Window(c, x, 16, 10, 12, bright);
            Door(c, pw / 2, 20, 16, Hex("#7f5539"));
            c.Rect(pw / 2 - 30, fh - 13, 60, 9, Hex("#e5157a"));
        }

        static void StageSprite(PixelCanvas c, int pw, int fh, int ph)
        {
            c.Rect(0, 0, pw, 10, Hex("#8b5a2b"));
            for (int x = 0; x < pw; x += 6) c.Rect(x, 0, 1, 10, Hex("#6b4226"));
            c.Rect(0, 10, pw, ph - 10, Hex("#c1121f"));
            for (int x = 0; x < pw; x += 5) c.Rect(x, 10, 2, ph - 10, Hex("#9b0f18"));
            c.Rect(0, ph - 5, pw, 5, Hex("#ffc20e"));
            for (int x = 3; x < pw; x += 10) c.Ellipse(x + 2, ph - 6, 3, 2, Hex("#ff7f11"));
        }

        static void BoothTent(PixelCanvas c, int pw, int fh, int ph)
        {
            for (int y = 0; y < ph; y++)
            {
                float t = y / (float)ph;
                int inset = y > fh ? (int)((y - fh) / (float)(ph - fh) * pw * 0.35f) : 0;
                for (int x = inset; x < pw - inset; x++)
                    c.Set(x, y, ((x / 8) % 2 == 0) ? Hex("#f8f9fa") : Hex("#d0e4f5"));
            }
            c.Rect(pw / 2 - 9, 0, 18, 18, Hex("#3a3a3a"));           // opening
            c.Rect(pw / 2 - 20, fh - 12, 40, 8, Hex("#1d70b8"));      // banner
            c.Rect(pw / 2 - 16, fh - 9, 32, 2, Hex("#ffffff"));
            c.Rect(8, 6, 10, 10, Hex("#6c757d"));                     // ballot box icon
            c.Rect(11, 15, 4, 1, Ink);
        }

        static void HouseFront(PixelCanvas c, int pw, int fh, int ph, BuildingDef b, bool bright)
        {
            Door(c, pw / 2, 10, 14, Hex("#7f5539"));
            Window(c, 6, 16, 9, 8, bright && (b.rect.x % 3 == 0));
            Window(c, pw - 15, 16, 9, 8, bright && (b.rect.x % 2 == 0));
            // a potted plant and a nameplate
            c.Rect(pw / 2 + 8, 0, 4, 5, Hex("#b5603e"));
            c.Circle(pw / 2 + 10, 7, 3, Hex("#2e8b57"));
            WaterTank(c, pw - 12, fh + 2);
        }

        // ------------------------------------------------------------------ props

        public static Sprite StreetLight(bool on) => Cached("streetlight" + on, () =>
        {
            var c = new PixelCanvas(12, 40, 21);
            c.Rect(5, 0, 2, 34, Hex("#5a5f66"));
            c.Rect(3, 0, 6, 2, Hex("#40444a"));
            c.Rect(5, 33, 6, 2, Hex("#5a5f66"));
            c.Rect(8, 30, 4, 3, on ? Hex("#fff3b0") : Hex("#8a8f96"));
            if (on) { c.Set(7, 29, new Color(1, 0.95f, 0.6f, 0.6f)); c.Set(11, 29, new Color(1, 0.95f, 0.6f, 0.6f)); }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        /// <summary>A wall switchboard with a bulb above it (school/office lights) or a fan.</summary>
        public static Sprite WastedLight(bool on, int variant) => Cached($"light_{on}_{variant}", () =>
        {
            var c = new PixelCanvas(14, 22, 22 + variant);
            if (variant % 2 == 0)
            {
                // bulb on a bracket
                c.Rect(6, 0, 2, 14, Hex("#6d6875"));
                c.Rect(4, 14, 6, 2, Hex("#6d6875"));
                c.Circle(7, 18, 3.5f, on ? Hex("#fff176") : Hex("#9e9e9e"));
                if (on) { c.Circle(7, 18, 2, Hex("#ffffff")); }
            }
            else
            {
                // ceiling fan seen through a window
                c.Rect(1, 6, 12, 12, on ? Hex("#ffe08a") : Hex("#2f3e55"));
                c.RectOutline(0, 5, 14, 14, Hex("#ece6d6"));
                c.Rect(3, 11, 8, 2, Hex("#5c4033"));
                c.Rect(6, 8, 2, 8, Hex("#5c4033"));
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Tap(bool leaking) => Cached("tap" + leaking, () =>
        {
            var c = new PixelCanvas(12, 18, 23);
            c.Rect(5, 0, 3, 14, Hex("#7d8597"));
            c.Rect(5, 12, 7, 3, Hex("#7d8597"));
            c.Rect(10, 9, 2, 3, Hex("#7d8597"));
            c.Rect(4, 15, 5, 2, Hex("#e63946"));                    // handle
            c.Rect(1, 0, 11, 2, Hex("#9aa0a6"));                     // basin edge
            if (leaking) { c.Rect(10, 4, 2, 4, Hex("#7cc3ff")); c.Ellipse(9, 1, 3, 1, Hex("#7cc3ff")); }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite SaplingSpot(bool planted) => Cached("sapling" + planted, () =>
        {
            var c = new PixelCanvas(16, 22, 24);
            c.Ellipse(8, 3, 7, 3, Hex("#6b4226"));
            c.Ellipse(8, 3, 5, 2, Hex("#4e2f18"));
            if (planted)
            {
                c.Rect(7, 3, 2, 10, Hex("#6b4226"));
                c.Circle(8, 15, 5, Hex("#3b9440"));
                c.Circle(5, 12, 3, Hex("#4caf50"));
                c.Circle(11, 12, 3, Hex("#4caf50"));
                c.Rect(2, 3, 1, 8, Hex("#c8a24a")); c.Rect(13, 3, 1, 8, Hex("#c8a24a"));    // tree guard
                c.Rect(2, 9, 12, 1, Hex("#c8a24a"));
            }
            else
            {
                c.Rect(12, 3, 3, 4, Hex("#b5603e"));                  // sapling in a pot
                c.Rect(13, 7, 1, 3, Hex("#3b9440")); c.Set(12, 9, Hex("#4caf50")); c.Set(14, 9, Hex("#4caf50"));
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite GarbagePile(int variant) => Cached("garbage" + variant, () =>
        {
            var c = new PixelCanvas(24, 16, 25 + variant);
            c.Ellipse(12, 5, 11, 5, Hex("#6f5e4b"));
            var junk = new[] { Hex("#e63946"), Hex("#f1faee"), Hex("#2d6cdf"), Hex("#ffc20e"), Hex("#8bc34a"), Hex("#9e9e9e"), Hex("#ff7f11") };
            for (int i = 0; i < 16; i++)
            {
                int x = c.Rand(3, 20), y = c.Rand(2, 10);
                c.Rect(x, y, c.Rand(2, 4), c.Rand(1, 3), junk[c.Rand(0, junk.Length)]);
            }
            c.Rect(4, 9, 6, 4, Hex("#2b2d42"));                       // black bag
            c.Rect(15, 8, 3, 6, Hex("#9fd3ff"));                      // bottle
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Bench => Cached("bench", () =>
        {
            var c = new PixelCanvas(24, 14, 26);
            c.Rect(2, 0, 2, 6, Hex("#5a5f66")); c.Rect(20, 0, 2, 6, Hex("#5a5f66"));
            c.Rect(0, 5, 24, 3, Hex("#c97c3b"));
            c.Rect(0, 9, 24, 3, Hex("#c97c3b"));
            c.Rect(0, 8, 24, 1, Hex("#8b5a2b"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite BrokenSwing => Cached("swing", () =>
        {
            var c = new PixelCanvas(28, 30, 27);
            c.Line(2, 0, 8, 28, Hex("#8a8f96")); c.Line(14, 0, 8, 28, Hex("#8a8f96"));
            c.Line(14, 0, 20, 28, Hex("#8a8f96")); c.Line(26, 0, 20, 28, Hex("#8a8f96"));
            c.Rect(8, 27, 13, 2, Hex("#8a8f96"));
            c.Line(11, 27, 11, 10, Hex("#555555"));
            c.Rect(9, 8, 6, 2, Hex("#c97c3b"));                      // seat hanging by one chain
            c.Line(18, 27, 19, 22, Hex("#555555"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Swing => Cached("swing_ok", () =>
        {
            var c = new PixelCanvas(28, 30, 28);
            c.Line(2, 0, 8, 28, Hex("#e63946")); c.Line(14, 0, 8, 28, Hex("#e63946"));
            c.Line(14, 0, 20, 28, Hex("#e63946")); c.Line(26, 0, 20, 28, Hex("#e63946"));
            c.Rect(8, 27, 13, 2, Hex("#e63946"));
            c.Line(11, 27, 11, 9, Hex("#555555")); c.Line(17, 27, 17, 9, Hex("#555555"));
            c.Rect(10, 7, 8, 2, Hex("#ffc20e"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Tree(int variant) => Cached("tree" + variant, () =>
        {
            var c = new PixelCanvas(28, 36, 30 + variant);
            c.Rect(12, 0, 4, 14, Hex("#6b4226"));
            Color leaf = variant == 0 ? Hex("#3b9440") : Hex("#2f7d32");
            c.Circle(14, 22, 11, leaf);
            c.Circle(7, 18, 6, Shade(leaf, 1.1f));
            c.Circle(21, 19, 6, Shade(leaf, 0.9f));
            c.Speckle(Shade(leaf, 1.3f), 20);
            if (variant == 1) for (int i = 0; i < 14; i++) c.Set(c.Rand(4, 24), c.Rand(14, 32), Hex("#ff4d2e"));   // gulmohar flowers
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite DeadTree => Cached("deadtree", () =>
        {
            var c = new PixelCanvas(20, 28, 31);
            c.Rect(9, 0, 3, 18, Hex("#6b4226"));
            c.Line(10, 14, 3, 24, Hex("#6b4226"));
            c.Line(11, 16, 17, 25, Hex("#6b4226"));
            c.Line(10, 10, 15, 15, Hex("#6b4226"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Dustbins => Cached("dustbins", () =>
        {
            var c = new PixelCanvas(26, 16, 32);
            var cols = new[] { Hex("#2e8b57"), Hex("#1d70b8"), Hex("#d62828") };
            for (int i = 0; i < 3; i++)
            {
                c.Rect(1 + i * 8, 0, 7, 11, cols[i]);
                c.Rect(0 + i * 8, 11, 9, 2, Shade(cols[i], 0.8f));
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite RenovationBoard => Cached("renboard", () =>
        {
            var c = new PixelCanvas(36, 30, 33);
            c.Rect(4, 0, 2, 16, Hex("#5a5f66")); c.Rect(30, 0, 2, 16, Hex("#5a5f66"));
            c.Rect(0, 12, 36, 18, Hex("#ffc20e"));
            c.Rect(2, 14, 32, 14, Hex("#fff8e1"));
            c.Rect(4, 24, 28, 2, Hex("#6a2c91"));
            for (int i = 0; i < 3; i++) c.Rect(4, 17 + i * 2, 18 + (i * 5) % 9, 1, Hex("#555555"));
            c.Rect(25, 16, 7, 6, Hex("#2e8b57"));                     // big "tick"
            c.Line(26, 19, 28, 17, Hex("#ffffff")); c.Line(28, 17, 31, 21, Hex("#ffffff"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite NoticeBoard => Cached("notice", () =>
        {
            var c = new PixelCanvas(26, 26, 34);
            c.Rect(3, 0, 2, 12, Hex("#6b4226")); c.Rect(21, 0, 2, 12, Hex("#6b4226"));
            c.Rect(0, 10, 26, 16, Hex("#8b5a2b"));
            c.Rect(2, 12, 22, 12, Hex("#d4a373"));
            c.Rect(4, 14, 7, 8, Hex("#ffffff")); c.Rect(13, 16, 9, 6, Hex("#fff3b0")); c.Rect(14, 13, 6, 2, Hex("#9fd3ff"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite RaddiPile => Cached("raddi", () =>
        {
            var c = new PixelCanvas(22, 14, 35);
            for (int i = 0; i < 5; i++) c.Rect(2 + i, 1 + i * 2, 16 - i, 2, i % 2 == 0 ? Hex("#efe6d2") : Hex("#d9cbb0"));
            c.Rect(3, 10, 8, 3, Hex("#6a2c91")); c.Rect(12, 9, 6, 4, Hex("#ffd23f"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Stall(int variant) => Cached("stall" + variant, () =>
        {
            var c = new PixelCanvas(48, 36, 36 + variant);
            Color aw = variant == 0 ? Hex("#6a2c91") : variant == 1 ? Hex("#2b9bb3") : variant == 2 ? Hex("#00a6a6") : variant == 3 ? Hex("#ff7f11") : Hex("#e5157a");
            c.Rect(2, 0, 3, 26, Hex("#6b4423")); c.Rect(43, 0, 3, 26, Hex("#6b4423"));
            c.Rect(0, 6, 48, 10, Hex("#8b5a2b"));
            c.Rect(0, 15, 48, 2, Hex("#6b4226"));
            for (int x = 0; x < 48; x++)
                for (int y = 26; y < 36; y++)
                    c.Set(x, y, ((x / 6) % 2 == 0) ? aw : Color.white);
            for (int x = 0; x < 48; x += 6) c.Ellipse(x + 3, 26, 3, 1.5f, ((x / 6) % 2 == 0) ? aw : Color.white);
            // what's on the counter
            switch (variant)
            {
                case 0: c.Rect(18, 17, 12, 8, Hex("#ffffff")); c.Rect(21, 19, 6, 4, aw); break;                          // rocket poster
                case 2: c.Rect(20, 17, 8, 7, Hex("#ffd23f")); c.Rect(22, 24, 4, 2, Hex("#5a5f66")); break;               // lantern
                case 3: c.Rect(14, 17, 6, 6, Hex("#c0c0c0")); c.Rect(26, 17, 4, 7, Hex("#f4a261")); c.Rect(32, 17, 4, 7, Hex("#f4a261")); break; // chai
                default:
                    var goods = new[] { Hex("#f28c28"), Hex("#f6d743"), Hex("#8bc34a"), Hex("#c0392b") };
                    for (int i = 0; i < 8; i++) c.Circle(6 + i * 5, 18, 2.2f, goods[(i + variant) % goods.Length]);
                    break;
            }
            c.Outline(Ink);
            return c.ToSprite(BottomLeft);
        });

        public static Sprite Barricade => Cached("barricade", () =>
        {
            var c = new PixelCanvas(16, 18, 37);
            c.Rect(1, 0, 2, 12, Hex("#5a5f66")); c.Rect(13, 0, 2, 12, Hex("#5a5f66"));
            for (int x = 0; x < 16; x++)
                for (int y = 7; y < 13; y++)
                    c.Set(x, y, (((x + y) / 3) % 2 == 0) ? Hex("#ff7f11") : Hex("#ffffff"));
            c.Rect(6, 13, 4, 4, Hex("#e63946"));                       // warning lamp
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite PosterWall(bool clean) => Cached("poster" + clean, () =>
        {
            var c = new PixelCanvas(32, 20, 38);
            c.Rect(0, 0, 32, 18, Hex("#d9d2c5"));
            if (clean)
            {
                // a mural: sun, tree and kids
                c.Rect(0, 0, 32, 18, Hex("#a8dadc"));
                c.Circle(25, 13, 3, Hex("#ffc20e"));
                c.Rect(5, 2, 2, 8, Hex("#6b4226")); c.Circle(6, 11, 4, Hex("#3b9440"));
                c.Rect(14, 2, 3, 5, Hex("#e5157a")); c.Rect(19, 2, 3, 5, Hex("#ff7f11"));
                c.Circle(15.5f, 9, 1.5f, Hex("#c99572")); c.Circle(20.5f, 9, 1.5f, Hex("#c99572"));
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    int x = 1 + i * 8;
                    c.Rect(x, 3 + (i % 2), 7, 11, Hex("#ffd23f"));
                    c.Rect(x + 1, 9 + (i % 2), 5, 4, Hex("#6a2c91"));
                    c.Rect(x + 2, 5 + (i % 2), 3, 2, Hex("#6a2c91"));
                }
                c.Line(3, 15, 12, 6, Hex("#3a3a3a"));                   // graffiti scribble
                c.Line(18, 14, 29, 4, Hex("#e63946"));
            }
            c.Rect(0, 17, 32, 3, Hex("#b5603e"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Bunting => Cached("bunting", () =>
        {
            var c = new PixelCanvas(48, 10, 39);
            var cols = new[] { Hex("#e5157a"), Hex("#ffc20e"), Hex("#00a6a6"), Hex("#ff7f11"), Hex("#7b2ff7") };
            c.Line(0, 9, 47, 9, Hex("#555555"));
            for (int i = 0; i < 8; i++)
            {
                int x = i * 6;
                for (int y = 0; y < 6; y++)
                    for (int dx = y / 2; dx < 5 - y / 2; dx++) c.Set(x + dx, 8 - y, cols[i % cols.Length]);
            }
            return c.ToSprite(Bottom);
        });

        public static Sprite Diya(bool lit) => Cached("diya" + lit, () =>
        {
            var c = new PixelCanvas(10, 14, 40);
            c.Ellipse(5, 2, 5, 2, Hex("#b5603e"));
            c.Rect(1, 2, 8, 1, Hex("#d9822b"));
            if (lit)
            {
                c.Ellipse(5, 7, 2, 4, Hex("#ffb000"));
                c.Ellipse(5, 6, 1, 2, Hex("#fff3b0"));
            }
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Candle => Cached("candle", () =>
        {
            var c = new PixelCanvas(4, 10, 41);
            c.Rect(1, 0, 2, 6, Hex("#fff8e1"));
            c.Ellipse(2, 7.5f, 1.2f, 2, Hex("#ffb000"));
            return c.ToSprite(Bottom);
        });

        public static Sprite Placard(int variant) => Cached("placard" + variant, () =>
        {
            var c = new PixelCanvas(14, 22, 42 + variant);
            c.Rect(6, 0, 2, 14, Hex("#8b5a2b"));
            var cols = new[] { Hex("#fff8e1"), Hex("#ffc20e"), Hex("#a8dadc") };
            c.Rect(0, 12, 14, 10, cols[variant % 3]);
            for (int i = 0; i < 3; i++) c.Rect(2, 14 + i * 2, 10 - (i * 3) % 5, 1, i == 0 ? Hex("#e63946") : Hex("#333333"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Table => Cached("table", () =>
        {
            var c = new PixelCanvas(28, 16, 43);
            c.Rect(2, 0, 2, 8, Hex("#6b4226")); c.Rect(24, 0, 2, 8, Hex("#6b4226"));
            c.Rect(0, 7, 28, 6, Hex("#c97c3b"));
            c.Rect(3, 9, 9, 5, Hex("#fff8e1")); c.Rect(14, 9, 9, 5, Hex("#ffc20e"));
            c.Rect(24, 11, 3, 1, Hex("#e63946")); c.Rect(24, 12, 3, 1, Hex("#1d70b8"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Rickshaw(int frame) => Cached("rickshaw" + frame, () =>
        {
            var c = new PixelCanvas(36, 30, 44);
            // body (side view, facing right)
            c.Rect(3, 5, 28, 12, Hex("#2e8b57"));
            c.Rect(3, 17, 24, 8, Hex("#ffc20e"));              // canopy
            c.Rect(27, 11, 6, 6, Hex("#2e8b57"));               // nose
            c.Rect(20, 11, 6, 6, Hex("#9fd3ff"));               // windshield
            c.Rect(6, 8, 10, 7, Hex("#3a3a3a"));                // seat
            c.Circle(8, 4, 3.5f, Hex("#222222")); c.Circle(29, 4, 3.5f, Hex("#222222"));
            c.Set(8, 4 + (frame == 0 ? 1 : -1), Hex("#aaaaaa")); c.Set(29, 4 + (frame == 0 ? -1 : 1), Hex("#aaaaaa"));
            // loudspeaker cone on the roof
            c.Rect(12, 25, 2, 2, Hex("#555555"));
            c.Rect(10, 27, 8, 3, Hex("#d9d9d9"));
            c.Rect(18, 26, 3, 4, Hex("#f1f1f1"));
            // party banner
            c.Rect(4, 9, 2, 6, Hex("#e5157a"));
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 0.05f));
        });

        public static Sprite FerrisWheel(int frame) => Cached("ferris" + frame, () =>
        {
            var c = new PixelCanvas(48, 56, 45);
            c.Line(14, 0, 24, 28, Hex("#5a5f66")); c.Line(34, 0, 24, 28, Hex("#5a5f66"));
            c.Rect(10, 0, 28, 2, Hex("#5a5f66"));
            Vector2 center = new Vector2(24, 32);
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                c.Set(Mathf.RoundToInt(center.x + Mathf.Cos(a) * 20), Mathf.RoundToInt(center.y + Mathf.Sin(a) * 20), Hex("#e5157a"));
            }
            var cabin = new[] { Hex("#ffc20e"), Hex("#00a6a6"), Hex("#ff7f11"), Hex("#7b2ff7") };
            for (int i = 0; i < 8; i++)
            {
                float a = (i / 8f + frame * 0.03f) * Mathf.PI * 2f;
                int x = Mathf.RoundToInt(center.x + Mathf.Cos(a) * 20), y = Mathf.RoundToInt(center.y + Mathf.Sin(a) * 20);
                c.Line((int)center.x, (int)center.y, x, y, Hex("#c9c9c9"));
                c.Rect(x - 3, y - 4, 6, 5, cabin[i % cabin.Length]);
            }
            c.Circle(center.x, center.y, 2, Hex("#ffd23f"));
            c.Outline(Ink);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        public static Sprite Banner(int variant) => Cached("banner" + variant, () =>
        {
            var c = new PixelCanvas(40, 30, 46 + variant);
            c.Rect(1, 0, 2, 30, Hex("#6b4226")); c.Rect(37, 0, 2, 30, Hex("#6b4226"));
            Color col = variant == 0 ? Hex("#6a2c91") : variant == 1 ? Hex("#2b9bb3") : variant == 2 ? Hex("#00a6a6") : Hex("#1d70b8");
            c.Rect(3, 16, 34, 12, col);
            c.Rect(6, 20, 28, 4, Hex("#ffffff"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite VoteFlag => Cached("voteflag", () =>
        {
            var c = new PixelCanvas(10, 18, 47);
            c.Rect(1, 0, 1, 18, Hex("#5a5f66"));
            c.Rect(2, 10, 8, 7, Hex("#1d70b8"));
            c.Rect(4, 12, 4, 3, Hex("#ffffff"));
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        public static Sprite Bush => Cached("bush", () =>
        {
            var c = new PixelCanvas(18, 12, 48);
            c.Ellipse(9, 5, 8, 5, Hex("#3b9440"));
            c.Speckle(Hex("#6cc26f"), 8);
            c.Outline(Ink);
            return c.ToSprite(Bottom);
        });

        /// <summary>A soft contact shadow that grounds a building or tree (darkest in the middle, fading out).</summary>
        public static Sprite BaseShadow => Cached("baseshadow", () =>
        {
            var c = new PixelCanvas(64, 16, 52);
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 16; y++)
                {
                    float dx = Mathf.Abs(x + 0.5f - 32f) / 32f, dy = Mathf.Abs(y + 0.5f - 8f) / 8f;
                    float a = Mathf.Clamp01(1f - Mathf.Pow(dx, 6f)) * Mathf.Clamp01(1f - dy * dy) * 0.45f;
                    c.Set(x, y, new Color(0.05f, 0.03f, 0.1f, a));
                }
            var tex = c.ToTexture();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0.5f), 64);
        });

        /// <summary>Kerb edges drawn over road tiles where they meet footpaths. mask: 1 = up, 2 = down, 4 = left, 8 = right.</summary>
        public static Sprite Curb(int mask) => Cached("curb" + mask, () =>
        {
            var c = new PixelCanvas(16, 16, 53 + mask);
            Color stone = Hex("#c9c1b2"), edge = Hex("#8f887c"), shade = new Color(0, 0, 0, 0.28f);
            if ((mask & 1) != 0) { c.Rect(0, 14, 16, 2, stone); c.Rect(0, 13, 16, 1, edge); c.Rect(0, 11, 16, 2, shade); }
            if ((mask & 2) != 0) { c.Rect(0, 0, 16, 2, stone); c.Rect(0, 2, 16, 1, edge); }
            if ((mask & 4) != 0) { c.Rect(0, 0, 2, 16, stone); c.Rect(2, 0, 1, 16, edge); c.Rect(3, 0, 1, 16, shade); }
            if ((mask & 8) != 0) { c.Rect(14, 0, 2, 16, stone); c.Rect(13, 0, 1, 16, edge); }
            return c.ToSprite(Center);
        });

        public static Sprite WaterFrame(int frame) => Cached("waterframe" + frame, () =>
        {
            var c = new PixelCanvas(16, 16, 54);
            c.Fill(Hex("#3d8fd1"));
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float w = Mathf.Sin((x + frame * 2) * 0.8f + y * 0.6f) + Mathf.Sin((y - frame) * 0.9f);
                    if (w > 1.35f) c.Set(x, y, Hex("#9fd0f5"));
                    else if (w < -1.3f) c.Set(x, y, Hex("#2f78b8"));
                }
            return c.ToSprite(Center);
        });

        public static Sprite Petal => Cached("petal", () =>
        {
            var c = new PixelCanvas(3, 3, 55);
            c.Set(1, 0, Hex("#ff4d2e")); c.Set(0, 1, Hex("#ff4d2e")); c.Set(1, 1, Hex("#ff7a3d")); c.Set(2, 1, Hex("#ff4d2e")); c.Set(1, 2, Hex("#ff4d2e"));
            return c.ToSprite(Center);
        });

        public static Sprite Pothole => Cached("pothole", () =>
        {
            var c = new PixelCanvas(14, 8, 49);
            c.Ellipse(7, 4, 6, 3, Hex("#2c2f35"));
            c.Ellipse(7, 4, 4, 2, Hex("#5b7a99"));
            return c.ToSprite(Center);
        });
    }
}
