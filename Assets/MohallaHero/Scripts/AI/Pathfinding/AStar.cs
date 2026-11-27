using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// 8-directional A* over a <see cref="NavGrid"/> with an octile heuristic and no corner cutting.
    /// Uses a binary min-heap open list and flat arrays for node data (no per-search allocations besides the result).
    /// </summary>
    public static class AStar
    {
        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        const float Sqrt2 = 1.41421356f;

        // Scratch buffers, resized per grid
        static float[] gScore;
        static int[] parent;
        static int[] visitStamp;
        static int[] closedStamp;
        static int stamp;
        static readonly MinHeap open = new MinHeap();

        public static int LastExpandedNodes { get; private set; }

        public static bool FindPath(NavGrid grid, Vector2Int start, Vector2Int goal, List<Vector2Int> result, int maxExpanded = 20000)
        {
            result.Clear();
            if (!grid.InBounds(start.x, start.y) || !grid.InBounds(goal.x, goal.y)) return false;
            if (!grid.IsWalkable(goal)) goal = grid.NearestWalkable(goal);
            if (!grid.IsWalkable(goal)) return false;
            if (start == goal) { result.Add(goal); return true; }

            int n = grid.Width * grid.Height;
            if (gScore == null || gScore.Length != n)
            {
                gScore = new float[n];
                parent = new int[n];
                visitStamp = new int[n];
                closedStamp = new int[n];
                stamp = 0;
            }
            stamp++;
            if (stamp == int.MaxValue)
            {
                System.Array.Clear(visitStamp, 0, n);
                System.Array.Clear(closedStamp, 0, n);
                stamp = 1;
            }

            int W = grid.Width;
            int startIdx = start.y * W + start.x;
            int goalIdx = goal.y * W + goal.x;

            open.Clear();
            gScore[startIdx] = 0f;
            parent[startIdx] = -1;
            visitStamp[startIdx] = stamp;
            open.Push(startIdx, Heuristic(start, goal));
            LastExpandedNodes = 0;

            while (open.Count > 0)
            {
                int current = open.Pop();
                if (closedStamp[current] == stamp) continue;
                closedStamp[current] = stamp;
                LastExpandedNodes++;

                if (current == goalIdx)
                {
                    Reconstruct(current, W, result);
                    return true;
                }
                if (LastExpandedNodes > maxExpanded) break;

                int cx = current % W, cy = current / W;
                for (int d = 0; d < Dirs.Length; d++)
                {
                    int nx = cx + Dirs[d].x, ny = cy + Dirs[d].y;
                    if (!grid.IsWalkable(nx, ny)) continue;
                    bool diagonal = d >= 4;
                    // no corner cutting: both orthogonal neighbours must be free
                    if (diagonal && (!grid.IsWalkable(cx + Dirs[d].x, cy) || !grid.IsWalkable(cx, cy + Dirs[d].y))) continue;

                    int ni = ny * W + nx;
                    if (closedStamp[ni] == stamp) continue;
                    float step = (diagonal ? Sqrt2 : 1f) * (grid.Cost(nx, ny) + grid.Cost(cx, cy)) * 0.5f;
                    float tentative = gScore[current] + step;
                    if (visitStamp[ni] != stamp || tentative < gScore[ni])
                    {
                        visitStamp[ni] = stamp;
                        gScore[ni] = tentative;
                        parent[ni] = current;
                        open.Push(ni, tentative + Heuristic(new Vector2Int(nx, ny), goal) * MinCost(grid));
                    }
                }
            }
            return false;
        }

        // Heuristic must not overestimate: scale by the cheapest tile cost on the grid (roads may cost < 1).
        static float cachedMinCost = -1f;
        static NavGrid cachedGrid;

        static float MinCost(NavGrid grid)
        {
            if (cachedGrid != grid || cachedMinCost < 0f) RecomputeMinCost(grid);
            return cachedMinCost;
        }

        public static void InvalidateCostCache() { cachedGrid = null; cachedMinCost = -1f; }

        static void RecomputeMinCost(NavGrid grid)
        {
            float min = float.MaxValue;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.Cost(x, y) < min) min = grid.Cost(x, y);
            cachedMinCost = min == float.MaxValue ? 1f : min;
            cachedGrid = grid;
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            int mn = Mathf.Min(dx, dy), mx = Mathf.Max(dx, dy);
            return mn * Sqrt2 + (mx - mn);
        }

        static void Reconstruct(int idx, int W, List<Vector2Int> result)
        {
            while (idx != -1)
            {
                result.Add(new Vector2Int(idx % W, idx / W));
                idx = parent[idx];
            }
            result.Reverse();
        }

        /// <summary>Binary min-heap of (index, priority). Duplicate entries are allowed and skipped via the closed set.</summary>
        class MinHeap
        {
            int[] items = new int[1024];
            float[] prios = new float[1024];
            public int Count { get; private set; }

            public void Clear() => Count = 0;

            public void Push(int item, float prio)
            {
                if (Count == items.Length)
                {
                    System.Array.Resize(ref items, Count * 2);
                    System.Array.Resize(ref prios, Count * 2);
                }
                int i = Count++;
                items[i] = item;
                prios[i] = prio;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (prios[p] <= prios[i]) break;
                    Swap(i, p);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = items[0];
                Count--;
                if (Count > 0)
                {
                    items[0] = items[Count];
                    prios[0] = prios[Count];
                    int i = 0;
                    while (true)
                    {
                        int l = 2 * i + 1, r = l + 1, s = i;
                        if (l < Count && prios[l] < prios[s]) s = l;
                        if (r < Count && prios[r] < prios[s]) s = r;
                        if (s == i) break;
                        Swap(i, s);
                        i = s;
                    }
                }
                return top;
            }

            void Swap(int a, int b)
            {
                int ti = items[a]; items[a] = items[b]; items[b] = ti;
                float tp = prios[a]; prios[a] = prios[b]; prios[b] = tp;
            }
        }
    }
}
