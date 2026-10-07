using UnityEngine;

namespace MohallaHero
{
    /// <summary>Tiny software rasterizer used to draw the game's pixel art at runtime.</summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] px;
        readonly System.Random rng;

        public PixelCanvas(int w, int h, int seed = 0)
        {
            W = w; H = h;
            px = new Color32[w * h];
            rng = new System.Random(seed);
        }

        public float Rand() => (float)rng.NextDouble();
        public int Rand(int min, int max) => rng.Next(min, max);

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            if (c.a >= 0.999f) { px[y * W + x] = c; return; }
            Color dst = px[y * W + x];
            float a = c.a + dst.a * (1 - c.a);
            if (a <= 0f) return;
            Color o = (c * c.a + dst * dst.a * (1 - c.a)) / a;
            o.a = a;
            px[y * W + x] = o;
        }

        public Color Get(int x, int y) => (x < 0 || y < 0 || x >= W || y >= H) ? Color.clear : (Color)px[y * W + x];

        public void Fill(Color c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

        public void Rect(int x, int y, int w, int h, Color c)
        {
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    Set(i, j, c);
        }

        public void RectOutline(int x, int y, int w, int h, Color c)
        {
            for (int i = x; i < x + w; i++) { Set(i, y, c); Set(i, y + h - 1, c); }
            for (int j = y; j < y + h; j++) { Set(x, j, c); Set(x + w - 1, j, c); }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

        public void Line(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Randomly lighten/darken pixels of the given base colour for texture.</summary>
        public void Noise(Color baseColor, float amount, float chance = 1f)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (Rand() > chance) continue;
                    float f = 1f + (Rand() * 2f - 1f) * amount;
                    var c = baseColor * f;
                    c.a = 1f;
                    px[y * W + x] = c;
                }
        }

        /// <summary>Random texture dots, only on pixels that are already drawn (never in transparent space).</summary>
        public void Speckle(Color c, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = Rand(0, W), y = Rand(0, H);
                if (px[y * W + x].a > 0) Set(x, y, c);
            }
        }

        /// <summary>Adds a 1px dark outline around opaque pixels (classic pixel-art readability).</summary>
        public void Outline(Color c)
        {
            var copy = (Color32[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 0) continue;
                    bool edge = false;
                    for (int d = 0; d < 4 && !edge; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (nx >= 0 && ny >= 0 && nx < W && ny < H && copy[ny * W + nx].a > 128) edge = true;
                    }
                    if (edge) px[y * W + x] = c;
                }
        }

        public Sprite ToSprite(Vector2 pivot, float ppu = SpriteFactory.PPU)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, W, H), pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        public Texture2D ToTexture()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
