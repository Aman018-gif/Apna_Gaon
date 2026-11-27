using UnityEngine;

namespace MohallaHero
{
    /// <summary>Paints Shanti Nagar into a 1-pixel-per-tile texture for the minimap and the full map.</summary>
    public static class MapTexture
    {
        static Color32[] pixels;

        public static Texture2D Create(WorldMap map) =>
            new Texture2D(map.W, map.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        public static void Paint(Texture2D tex, WorldMap map)
        {
            if (pixels == null || pixels.Length != map.W * map.H) pixels = new Color32[map.W * map.H];
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                    pixels[y * map.W + x] = !map.IsWalkable(x, y) && !WorldMap.TileBlocks(map.Tiles[x, y])
                        ? (Color32)new Color(0.86f, 0.42f, 0.36f)   // buildings, trees, barricades
                        : (Color32)TileColor(map.Tiles[x, y]);
            foreach (var b in WorldLayout.Buildings)
            {
                Color c = BuildingColor(b.style);
                for (int x = b.rect.xMin; x < b.rect.xMax; x++)
                    for (int y = b.rect.yMin; y < b.rect.yMax; y++)
                        if (map.InBounds(x, y)) pixels[y * map.W + x] = c;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }

        public static Color TileColor(TileKind k)
        {
            switch (k)
            {
                case TileKind.Asphalt: case TileKind.LaneMark: return new Color(0.33f, 0.34f, 0.38f);
                case TileKind.Zebra: return new Color(0.75f, 0.75f, 0.72f);
                case TileKind.Sidewalk: return new Color(0.7f, 0.67f, 0.62f);
                case TileKind.Plaza: return new Color(0.84f, 0.79f, 0.71f);
                case TileKind.Grass: case TileKind.Flowers: return new Color(0.38f, 0.68f, 0.31f);
                case TileKind.ParkPath: return new Color(0.79f, 0.55f, 0.38f);
                case TileKind.Ground: return new Color(0.85f, 0.71f, 0.5f);
                case TileKind.Water: return new Color(0.24f, 0.56f, 0.82f);
                case TileKind.Wall: return new Color(0.6f, 0.34f, 0.24f);
                case TileKind.Hedge: return new Color(0.18f, 0.49f, 0.2f);
            }
            return Color.magenta;
        }

        static Color BuildingColor(BuildingStyle s)
        {
            switch (s)
            {
                case BuildingStyle.House: return new Color(0.98f, 0.62f, 0.5f);
                case BuildingStyle.Apartment: return new Color(1f, 0.76f, 0.05f);
                case BuildingStyle.School: return new Color(1f, 0.85f, 0.3f);
                case BuildingStyle.PartyOffice: return new Color(0.6f, 0.35f, 0.75f);
                case BuildingStyle.Office: case BuildingStyle.RationOffice: return new Color(0.35f, 0.5f, 0.8f);
                case BuildingStyle.Clinic: return new Color(0.3f, 0.8f, 0.8f);
                case BuildingStyle.Yoga: return new Color(0.4f, 0.8f, 0.55f);
                default: return new Color(0.95f, 0.45f, 0.55f);
            }
        }
    }
}
