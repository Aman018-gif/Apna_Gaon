using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Walkability + movement cost grid used by A*. Streets and footpaths are cheaper, so residents walk along them
    /// instead of cutting across the park.
    /// </summary>
    public class NavGrid
    {
        public readonly int Width, Height;
        readonly bool[,] walkable;
        readonly float[,] cost;

        public NavGrid(int width, int height)
        {
            Width = width;
            Height = height;
            walkable = new bool[width, height];
            cost = new float[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    walkable[x, y] = true;
                    cost[x, y] = 1f;
                }
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsWalkable(int x, int y) => InBounds(x, y) && walkable[x, y];
        public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);
        public float Cost(int x, int y) => cost[x, y];

        public void SetWalkable(int x, int y, bool value) { if (InBounds(x, y)) walkable[x, y] = value; }
        public void SetCost(int x, int y, float value) { if (InBounds(x, y)) cost[x, y] = Mathf.Max(0.1f, value); }

        public static NavGrid FromWorld(WorldMap map, float roadCost)
        {
            var g = new NavGrid(map.W, map.H);
            g.Refresh(map, roadCost);
            return g;
        }

        public void Refresh(WorldMap map, float roadCost)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    walkable[x, y] = map.IsWalkable(x, y);
                    var t = map.Tiles[x, y];
                    if (WorldMap.IsPaved(t)) cost[x, y] = roadCost;
                    else if (t == TileKind.Plaza || t == TileKind.Ground) cost[x, y] = 1f;
                    else cost[x, y] = 1.35f;   // grass and flower beds
                }
        }

        /// <summary>Nearest walkable cell to <paramref name="c"/> (spiral search), or c itself if none found.</summary>
        public Vector2Int NearestWalkable(Vector2Int c, int maxRadius = 6)
        {
            if (IsWalkable(c)) return c;
            for (int r = 1; r <= maxRadius; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (IsWalkable(n)) return n;
                    }
            return c;
        }
    }
}
