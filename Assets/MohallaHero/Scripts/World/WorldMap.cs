using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Pure data model of Shanti Nagar: tile kinds, static obstacles and locked gates.
    /// Generation is deterministic, so saves only store what changed (story variables and unlocked areas).
    /// </summary>
    public class WorldMap
    {
        public readonly int W = WorldLayout.Width, H = WorldLayout.Height;
        public readonly TileKind[,] Tiles;
        readonly bool[,] blocked;
        readonly bool[,] gateLocked;

        public WorldMap()
        {
            Tiles = new TileKind[W, H];
            blocked = new bool[W, H];
            gateLocked = new bool[W, H];
            Generate();
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

        public bool IsWalkable(int x, int y) => InBounds(x, y) && !blocked[x, y] && !gateLocked[x, y];
        public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);
        public bool IsStaticBlocked(int x, int y) => !InBounds(x, y) || blocked[x, y];

        public void SetGateLocked(AreaId area, bool locked)
        {
            if (!WorldLayout.Gates.TryGetValue(area, out var cells)) return;
            foreach (var c in cells) gateLocked[c.x, c.y] = locked;
        }

        public static bool TileBlocks(TileKind k) => k == TileKind.Water || k == TileKind.Wall || k == TileKind.Hedge;

        /// <summary>Paved tiles are cheaper for NPC pathfinding, so people use streets and footpaths.</summary>
        public static bool IsPaved(TileKind k) =>
            k == TileKind.Asphalt || k == TileKind.LaneMark || k == TileKind.Zebra || k == TileKind.Sidewalk || k == TileKind.ParkPath;

        // ------------------------------------------------------------------ generation

        void Set(int x, int y, TileKind k) { if (InBounds(x, y)) Tiles[x, y] = k; }

        void Fill(RectInt r, TileKind k)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    Set(x, y, k);
        }

        void Border(RectInt r, TileKind k)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    if (x == r.xMin || x == r.xMax - 1 || y == r.yMin || y == r.yMax - 1) Set(x, y, k);
        }

        void Generate()
        {
            Fill(new RectInt(0, 0, W, H), TileKind.Plaza);

            // Roads
            var main = WorldLayout.MainRoad;
            Fill(main, TileKind.Asphalt);
            Fill(new RectInt(main.x, main.y + 1, main.width, 1), TileKind.LaneMark);
            Fill(new RectInt(main.x, main.y - 1, main.width, 1), TileKind.Sidewalk);
            Fill(new RectInt(main.x, main.yMax, main.width, 1), TileKind.Sidewalk);
            Fill(WorldLayout.LaneNorth, TileKind.Asphalt);
            Fill(WorldLayout.LaneSouth, TileKind.Asphalt);
            foreach (int x in WorldLayout.StreetX)
                for (int y = 1; y < H - 1; y++)
                    for (int i = 0; i < 3; i++)
                        if (Tiles[x + i, y] != TileKind.LaneMark) Set(x + i, y, TileKind.Asphalt);
            foreach (int x in WorldLayout.ZebraX) Fill(new RectInt(x, main.y, 2, main.height), TileKind.Zebra);

            // Districts
            Fill(WorldLayout.Playground, TileKind.Ground);

            var park = WorldLayout.Park;
            Fill(park, TileKind.Grass);
            Fill(new RectInt(park.x + 1, park.y + 6, park.width - 2, 1), TileKind.ParkPath);
            Fill(new RectInt(park.x + 10, park.y + 1, 1, park.height - 2), TileKind.ParkPath);
            var flowers = new System.Random(7);
            for (int i = 0; i < 40; i++)
            {
                int x = flowers.Next(park.xMin + 1, park.xMax - 1), y = flowers.Next(park.yMin + 1, park.yMax - 1);
                if (Tiles[x, y] == TileKind.Grass) Tiles[x, y] = TileKind.Flowers;
            }
            Fill(WorldLayout.ParkPond, TileKind.Water);
            Border(park, TileKind.Hedge);
            foreach (var c in WorldLayout.ParkOpenings) Set(c.x, c.y, TileKind.ParkPath);

            // Upper band gardens (a little green between the houses)
            for (int x = 2; x < 94; x++)
                for (int y = 53; y < 62; y++)
                    if (!IsStreetX(x) && (x * 7 + y * 3) % 11 == 0) Set(x, y, TileKind.Grass);

            Fill(WorldLayout.Maidan, TileKind.Ground);
            Border(WorldLayout.Maidan, TileKind.Wall);
            Border(WorldLayout.Bazaar, TileKind.Wall);

            // Outer boundary wall
            Border(new RectInt(0, 0, W, H), TileKind.Wall);

            // Gates: walkable ground behind a removable barricade
            foreach (var kv in WorldLayout.Gates)
                foreach (var c in kv.Value)
                {
                    Tiles[c.x, c.y] = kv.Key == AreaId.Maidan ? TileKind.Ground : TileKind.Sidewalk;
                    gateLocked[c.x, c.y] = true;
                }

            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    blocked[x, y] = TileBlocks(Tiles[x, y]);

            // Buildings, trees and fixed props block their cells
            foreach (var b in WorldLayout.Buildings) Block(b.rect);
            foreach (var t in WorldLayout.Trees) Block(new RectInt(t.x, t.y, 1, 1));
            foreach (var (cell, _) in WorldLayout.MelaStalls) Block(new RectInt(cell.x, cell.y, 3, 1));
            Block(new RectInt(WorldLayout.Ferris.x, WorldLayout.Ferris.y, 3, 2));
        }

        static bool IsStreetX(int x)
        {
            foreach (int s in WorldLayout.StreetX) if (x >= s && x < s + 3) return true;
            return false;
        }

        void Block(RectInt r)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    if (InBounds(x, y)) blocked[x, y] = true;
        }
    }
}
