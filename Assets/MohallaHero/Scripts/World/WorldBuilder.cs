using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MohallaHero
{
    /// <summary>
    /// Builds the scene for a session from the <see cref="WorldMap"/>: ground and collision tilemaps, buildings with
    /// signboards, street furniture, story objects and the Election Mela decorations.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public Tilemap Ground { get; private set; }
        public Tilemap Detail { get; private set; }
        public Tilemap Blockers { get; private set; }

        public readonly Dictionary<string, SpriteRenderer> BuildingSprites = new Dictionary<string, SpriteRenderer>();
        public readonly List<StoryObject> Objects = new List<StoryObject>();
        public readonly List<SpriteRenderer> StreetLights = new List<SpriteRenderer>();
        public readonly List<GlowLight> LightGlows = new List<GlowLight>();
        public readonly List<SpriteRenderer> PosterWalls = new List<SpriteRenderer>();
        public readonly List<GameObject> Bunting = new List<GameObject>();
        public readonly List<GameObject> Potholes = new List<GameObject>();
        public readonly List<SpriteRenderer> Swings = new List<SpriteRenderer>();
        public Transform MelaRoot { get; private set; }
        public Transform MarchRoot { get; private set; }
        public SpriteRenderer Ferris { get; private set; }
        public Transform Props { get; private set; }

        WorldMap map;
        readonly List<Vector3Int> waterCells = new List<Vector3Int>();
        Tile waterTile;
        float waterTimer;
        int waterFrame;

        public void Build(WorldMap map, AreaManager areas)
        {
            this.map = map;
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(transform, false);
            gridGo.AddComponent<Grid>();

            Ground = CreateTilemap(gridGo.transform, "Ground", -100);
            Detail = CreateTilemap(gridGo.transform, "Detail", -95);
            Blockers = CreateTilemap(gridGo.transform, "Blockers", -99);
            Blockers.GetComponent<TilemapRenderer>().enabled = false;
            Blockers.gameObject.AddComponent<TilemapCollider2D>();

            Props = new GameObject("Props").transform;
            Props.SetParent(transform, false);

            PaintGround();
            PaintCurbs();
            PaintBlockers();
            BuildBuildings();
            BuildStreet();
            BuildStoryObjects();
            BuildGates(areas);
            BuildMela();
            MarchRoot = new GameObject("March").transform;
            MarchRoot.SetParent(transform, false);
        }

        static Tilemap CreateTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            return tm;
        }

        static Tile MakeTile(Sprite s, Tile.ColliderType collider = Tile.ColliderType.None)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = collider;
            return t;
        }

        void PaintGround()
        {
            var cache = new Dictionary<(TileKind, int), Tile>();
            var tiles = new TileBase[map.W * map.H];
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                {
                    var k = map.Tiles[x, y];
                    int variant = (x * 73856093 ^ y * 19349663) & 3;
                    if (k == TileKind.Water)
                    {
                        if (waterTile == null) waterTile = MakeTile(SpriteFactory.WaterFrame(0));
                        tiles[y * map.W + x] = waterTile;
                        waterCells.Add(new Vector3Int(x, y, 0));
                        continue;
                    }
                    if (k != TileKind.Asphalt && k != TileKind.Grass && k != TileKind.Plaza) variant %= 2;
                    if (k == TileKind.Asphalt && variant == 3 && (x + y) % 5 != 0) variant = 2;   // potholes are rare
                    if (!cache.TryGetValue((k, variant), out var tile))
                        cache[(k, variant)] = tile = MakeTile(SpriteFactory.Ground(k, variant));
                    tiles[y * map.W + x] = tile;
                }
            Ground.SetTilesBlock(new BoundsInt(0, 0, 0, map.W, map.H, 1), tiles);
        }

        static bool IsRoad(TileKind k) => k == TileKind.Asphalt || k == TileKind.LaneMark || k == TileKind.Zebra;

        /// <summary>Raised kerbs where a road meets a footpath, courtyard or garden (a detail layer above the ground).</summary>
        void PaintCurbs()
        {
            var cache = new Dictionary<int, Tile>();
            for (int y = 1; y < map.H - 1; y++)
                for (int x = 1; x < map.W - 1; x++)
                {
                    if (!IsRoad(map.Tiles[x, y])) continue;
                    int mask = 0;
                    if (!IsRoad(map.Tiles[x, y + 1]) && map.Tiles[x, y + 1] != TileKind.Wall) mask |= 1;
                    if (!IsRoad(map.Tiles[x, y - 1]) && map.Tiles[x, y - 1] != TileKind.Wall) mask |= 2;
                    if (!IsRoad(map.Tiles[x - 1, y]) && map.Tiles[x - 1, y] != TileKind.Wall) mask |= 4;
                    if (!IsRoad(map.Tiles[x + 1, y]) && map.Tiles[x + 1, y] != TileKind.Wall) mask |= 8;
                    if (mask == 0) continue;
                    if (!cache.TryGetValue(mask, out var tile)) cache[mask] = tile = MakeTile(SpriteFactory.Curb(mask));
                    Detail.SetTile(new Vector3Int(x, y, 0), tile);
                }
        }

        void Update()
        {
            if (waterTile == null || waterCells.Count == 0) return;
            waterTimer += Time.deltaTime;
            if (waterTimer < 0.45f) return;
            waterTimer = 0f;
            waterFrame = (waterFrame + 1) % 4;
            waterTile.sprite = SpriteFactory.WaterFrame(waterFrame);
            foreach (var c in waterCells) Ground.RefreshTile(c);
        }

        void PaintBlockers()
        {
            var block = MakeTile(SpriteFactory.White, Tile.ColliderType.Grid);
            var tiles = new TileBase[map.W * map.H];
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                    if (!map.IsWalkable(x, y)) tiles[y * map.W + x] = block;
            Blockers.SetTilesBlock(new BoundsInt(0, 0, 0, map.W, map.H, 1), tiles);
        }

        // ------------------------------------------------------------------ buildings

        void BuildBuildings()
        {
            var root = new GameObject("Buildings").transform;
            root.SetParent(transform, false);
            foreach (var b in WorldLayout.Buildings)
            {
                var sr = WorldObjects.CreateSprite("Bld_" + b.id, SpriteFactory.Building(b, true), new Vector2(b.rect.x, b.rect.y), root);
                BuildingSprites[b.id] = sr;
                var shadow = WorldObjects.CreateSprite("Shadow", SpriteFactory.BaseShadow, new Vector2(b.rect.x + b.rect.width / 2f, b.rect.y + 0.05f), sr.transform, -1);
                shadow.transform.localScale = new Vector3(b.rect.width + 0.8f, 1.3f, 1f);
                if (b.style == BuildingStyle.Stage || b.style == BuildingStyle.Booth) continue;   // shown with the Mela
                if (b.style != BuildingStyle.House)
                {
                    float signY = b.rect.y + SpriteFactory.ExtraHeight(b.style) + 1f - 0.55f;
                    WorldObjects.Label(sr.transform, b.label, new Vector2(b.rect.x + b.rect.width / 2f, signY), Color.white, 0.075f, 34);
                }
                else if (b.hasOwner)
                    WorldObjects.Label(sr.transform, NpcDatabase.FirstName(b.owner), new Vector2(b.rect.x + b.rect.width / 2f, b.rect.y + 2.6f), Color.white, 0.055f, 34);
            }
            // The Stage and the Booth belong to the Mela
        }

        // ------------------------------------------------------------------ street furniture

        void BuildStreet()
        {
            // street lights along the main road and the lanes
            for (int x = 4; x < 94; x += 9)
            {
                AddStreetLight(new Vector2(x + 0.5f, 32.3f));
                AddStreetLight(new Vector2(x + 4.5f, 27.1f));
            }
            for (int x = 6; x < 90; x += 14) AddStreetLight(new Vector2(x + 0.5f, 45.3f));

            foreach (var t in WorldLayout.Trees)
            {
                var tree = WorldObjects.CreateSprite("Tree", SpriteFactory.Tree((t.x + t.y) % 2), WorldLayout.CellFeet(t), Props);
                var shadow = WorldObjects.CreateSprite("Shadow", SpriteFactory.BaseShadow, (Vector2)tree.transform.position + new Vector2(0.15f, 0.05f), tree.transform, -1);
                shadow.transform.localScale = new Vector3(2.4f, 1.6f, 1f);
            }

            // park furniture
            var park = WorldLayout.Park;
            WorldObjects.CreateSprite("Bench", SpriteFactory.Bench, new Vector2(56.5f, 38.6f), Props);
            WorldObjects.CreateSprite("Bench", SpriteFactory.Bench, new Vector2(64.5f, 38.6f), Props);
            Swings.Add(WorldObjects.CreateSprite("Swing", SpriteFactory.BrokenSwing, new Vector2(56f, 40.2f), Props));
            Swings.Add(WorldObjects.CreateSprite("Swing", SpriteFactory.BrokenSwing, new Vector2(63f, 43.2f), Props));
            WorldObjects.CreateSprite("Dustbins", SpriteFactory.Dustbins, new Vector2(61.8f, 34.2f), Props);
            WorldObjects.Label(Props, "Gulmohar Park", new Vector2(60.5f, 33.6f + 0.1f), new Color(1f, 0.95f, 0.6f), 0.07f);

            // poster walls (graffiti now, murals later)
            foreach (var p in new[] { new Vector2(53f, 45.3f), new Vector2(67f, 45.3f), new Vector2(26f, 26.3f), new Vector2(42f, 26.3f) })
                PosterWalls.Add(WorldObjects.CreateSprite("PosterWall", SpriteFactory.PosterWall(false), p, Props));

            // bunting over the streets (appears as the mohalla's spirit rises)
            foreach (var p in new[] { new Vector2(10, 33.8f), new Vector2(34, 33.8f), new Vector2(84, 33.8f), new Vector2(30, 48.8f), new Vector2(60, 48.8f), new Vector2(84, 48.8f) })
            {
                var sr = WorldObjects.CreateSprite("Bunting", SpriteFactory.Bunting, p, Props, 32);
                Bunting.Add(sr.gameObject);
            }

            // potholes that disappear as the rating rises
            foreach (var p in new[] { new Vector2(12.5f, 29.5f), new Vector2(40.5f, 30.6f), new Vector2(66.5f, 29.4f), new Vector2(86.5f, 30.5f), new Vector2(47.5f, 20.5f), new Vector2(21.5f, 52.5f) })
            {
                var sr = WorldObjects.CreateSprite("Pothole", SpriteFactory.Pothole, p, Props, -80);
                Potholes.Add(sr.gameObject);
            }

            // rickshaw stand and odds and ends
            WorldObjects.CreateSprite("Rickshaw", SpriteFactory.Rickshaw(0), new Vector2(17.2f, 24.2f), Props);
            WorldObjects.CreateSprite("Bench", SpriteFactory.Bench, new Vector2(13.2f, 19.2f), Props);
            WorldObjects.CreateSprite("Dustbins", SpriteFactory.Dustbins, new Vector2(18.6f, 34.2f), Props);
            WorldObjects.CreateSprite("Dustbins", SpriteFactory.Dustbins, new Vector2(42.6f, 18.2f), Props);
            for (int i = 0; i < 4; i++)
                WorldObjects.CreateSprite("Bush", SpriteFactory.Bush, new Vector2(78.5f + i * 4.5f, 33.4f), Props);
            WorldObjects.CreateSprite("Table", SpriteFactory.Table, WorldLayout.CellFeet(WorldLayout.PlacardTable) + new Vector2(0, 0.6f), Props);
            WorldObjects.Label(Props, "Bazaar Gali", new Vector2(34.5f, 27.0f + 0.45f), new Color(1f, 0.85f, 0.3f), 0.07f);
            WorldObjects.Label(Props, "Mela Maidan", new Vector2(84.5f, 27.0f + 0.45f), new Color(1f, 0.85f, 0.3f), 0.07f);
            WorldObjects.Label(Props, "MAIN ROAD", new Vector2(8f, 30.0f), new Color(1, 1, 1, 0.35f), 0.09f, -70, false);
        }

        void AddStreetLight(Vector2 p)
        {
            var sr = WorldObjects.CreateSprite("StreetLight", SpriteFactory.StreetLight(true), p, Props);
            StreetLights.Add(sr);
            var glow = WorldObjects.AddGlow(sr.transform, new Vector2(0.4f, 1.95f), new Color(1f, 0.9f, 0.55f, 0.45f), 3.2f);
            LightGlows.Add(glow);
        }

        // ------------------------------------------------------------------ story objects

        StoryObject Add(StoryObject o) { Objects.Add(o); return o; }

        void BuildStoryObjects()
        {
            var root = new GameObject("StoryObjects").transform;
            root.SetParent(transform, false);

            for (int i = 0; i < WorldLayout.Lights.Length; i++)
            {
                var o = Add(StoryObject.Create(root, "light_" + i, "obj_light", i % 2 == 0 ? "Bulb jal raha hai (switch off?)" : "Khaali kamre mein pankha chal raha hai",
                    WorldLayout.CellFeet(WorldLayout.Lights[i]), SpriteFactory.WastedLight(true, i), SpriteFactory.WastedLight(false, i)));
                var glow = WorldObjects.AddGlow(o.transform, new Vector2(0, 1.1f), new Color(1f, 0.95f, 0.5f, 0.55f), 1.6f);
                o.OnRefresh = obj => glow.On = !obj.Resolved;
            }
            for (int i = 0; i < WorldLayout.Taps.Length; i++)
            {
                var o = Add(StoryObject.Create(root, "tap_" + i, "obj_tap", "Nal tapak raha hai", WorldLayout.CellFeet(WorldLayout.Taps[i]),
                    SpriteFactory.Tap(true), SpriteFactory.Tap(false)));
                o.gameObject.AddComponent<DripEffect>();
            }
            for (int i = 0; i < WorldLayout.Saplings.Length; i++)
                Add(StoryObject.Create(root, "sapling_" + i, "obj_sapling", "Paudha lagao", WorldLayout.CellFeet(WorldLayout.Saplings[i]),
                    SpriteFactory.SaplingSpot(false), SpriteFactory.SaplingSpot(true)));
            for (int i = 0; i < WorldLayout.Garbage.Length; i++)
            {
                var o = Add(StoryObject.Create(root, "garbage_" + i, "obj_garbage", "Kachre ka dher", WorldLayout.CellFeet(WorldLayout.Garbage[i]),
                    SpriteFactory.GarbagePile(i)));
                o.HideWhenResolved = true;
                o.InteractRadius = 1.5f;
                o.gameObject.AddComponent<FlyEffect>();
            }

            var home = Add(StoryObject.Create(root, "home", "obj_home", "Ghar (aaram karo / save)", WorldLayout.CellFeet(WorldLayout.PlayerDoor), null));
            home.StayUsableWhenResolved = true;

            Add(StoryObject.Create(root, "notice", "obj_notice", "Ward Notice Board", WorldLayout.CellFeet(WorldLayout.NoticeBoard), SpriteFactory.NoticeBoard)).StayUsableWhenResolved = true;
            var board = Add(StoryObject.Create(root, "park_board", "park_board", "\"Renovation Complete\" board", WorldLayout.CellFeet(WorldLayout.ParkBoard), SpriteFactory.RenovationBoard));
            board.InteractRadius = 1.6f;
            Add(StoryObject.Create(root, "raddi", "raddi_pile", "Raddi ka dher", WorldLayout.CellFeet(WorldLayout.RaddiPile), SpriteFactory.RaddiPile));

            // Bazaar
            var lucky = Add(StoryObject.Create(root, "lucky", "lucky_stall", "\"Lucky Offer\" stall", WorldLayout.CellFeet(WorldLayout.LuckyStall) + new Vector2(-1.5f, -0.2f), SpriteFactory.Stall(4)));
            lucky.InteractOffset = new Vector2(1.5f, -0.2f);
            lucky.InteractRadius = 1.8f;
            WorldObjects.Label(lucky.transform, "LUCKY DRAW!", (Vector2)lucky.transform.position + new Vector2(1.5f, 2.7f), new Color(1f, 0.9f, 0.2f), 0.07f, 40);
            Add(StoryObject.Create(root, "ration", "ration_queue", "Ration Office queue", WorldLayout.CellFeet(WorldLayout.RationQueue), null));
            for (int i = 0; i < 4; i++)   // the queue itself
                WorldObjects.CreateSprite("Queue", SpriteFactory.Character(SpriteFactory.Hex("#c99572"), i % 2 == 0 ? SpriteFactory.Hex("#9e9e9e") : SpriteFactory.Hex("#e9c46a"),
                    SpriteFactory.Hex("#3d3d4f"), SpriteFactory.Hex("#222222"), Color.gray, i == 2, Outfit.Plain, 1, 0, "queue" + i),
                    WorldLayout.CellFeet(WorldLayout.RationQueue) + new Vector2(-1.2f - i * 0.9f, -0.3f), root);

            // Chapter 5
            var table = Add(StoryObject.Create(root, "placards", "placard_table", "Placard table", WorldLayout.CellFeet(WorldLayout.PlacardTable), null));
            table.StayUsableWhenResolved = true;
            table.InteractRadius = 1.6f;

            // Chapter 6: doors of voters' houses
            for (int i = 0; i < WorldLayout.VoterHouses.Count; i++)
            {
                var b = WorldLayout.Buildings[WorldLayout.VoterHouses[i]];
                var door = Add(StoryObject.Create(root, "door_" + i, "voter_door", "Darwaza khatkhatao (voter)", WorldLayout.CellFeet(b.Door), null));
                var flag = WorldObjects.CreateSprite("VoteFlag", SpriteFactory.VoteFlag, WorldLayout.CellFeet(b.Door) + new Vector2(1.3f, 0.6f), door.transform);
                door.OnRefresh = obj =>
                {
                    var gm = GameManager.I;
                    flag.enabled = gm != null && gm.Story.Is("m6_started") && !obj.Resolved;
                };
            }
        }

        // ------------------------------------------------------------------ gates

        void BuildGates(AreaManager areas)
        {
            var root = new GameObject("Gates").transform;
            root.SetParent(transform, false);
            var hints = new Dictionary<AreaId, string>
            {
                { AreaId.Bazaar, "\"Road repair in progress\". The Bazaar Gali opens in Chapter 3 (Imandar Bazaar)." },
                { AreaId.Maidan, "\"Mela ki taiyari chal rahi hai\". The Mela Maidan opens for the Election Mela (Chapter 6)." },
            };
            foreach (var kv in WorldLayout.Gates)
            {
                foreach (var c in kv.Value)
                {
                    var sr = WorldObjects.CreateSprite("Barricade_" + kv.Key, SpriteFactory.Barricade, WorldLayout.CellFeet(c) + new Vector2(0, -0.15f), root);
                    areas.RegisterBarrier(kv.Key, sr.gameObject);
                }
                var first = kv.Value[0];
                var sign = WorldObjects.Create<LockedSign>("Sign_" + kv.Key, WorldLayout.CellFeet(first + new Vector2Int(-1, 1)), root);
                sign.Area = kv.Key;
                sign.Hint = hints[kv.Key];
                var board = WorldObjects.CreateSprite("Board", SpriteFactory.NoticeBoard, sign.transform.position, sign.transform);
                board.transform.localScale = Vector3.one * 0.75f;
                areas.RegisterBarrier(kv.Key, sign.gameObject);
            }
        }

        // ------------------------------------------------------------------ the Election Mela

        void BuildMela()
        {
            MelaRoot = new GameObject("Mela").transform;
            MelaRoot.SetParent(transform, false);
            BuildingSprites["stage"].transform.SetParent(MelaRoot, true);
            BuildingSprites["booth"].transform.SetParent(MelaRoot, true);

            foreach (var (cell, variant) in WorldLayout.MelaStalls)
                WorldObjects.CreateSprite("Stall", SpriteFactory.Stall(variant), new Vector2(cell.x, cell.y), MelaRoot);
            string[] stallNames = { "ROCKET", "SEE-SAW", "LANTERN", "CHACHA KHABAR", "SNACKS" };
            for (int i = 0; i < WorldLayout.MelaStalls.Length; i++)
            {
                var (cell, _) = WorldLayout.MelaStalls[i];
                WorldObjects.Label(MelaRoot, stallNames[i], new Vector2(cell.x + 1.5f, cell.y + 2.0f), Color.white, 0.06f, 40);
            }
            Ferris = WorldObjects.CreateSprite("Ferris", SpriteFactory.FerrisWheel(0), new Vector2(WorldLayout.Ferris.x + 1.5f, WorldLayout.Ferris.y), MelaRoot);
            Ferris.gameObject.AddComponent<FerrisSpin>();
            for (int i = 0; i < 4; i++)
                WorldObjects.CreateSprite("Banner", SpriteFactory.Banner(i), new Vector2(77.5f + i * 4.5f, 24.2f), MelaRoot);
            for (int x = 77; x < 94; x += 3)
                WorldObjects.CreateSprite("Bunting", SpriteFactory.Bunting, new Vector2(x + 1.5f, 15.3f), MelaRoot, 32);

            var stage = Add(StoryObject.Create(MelaRoot, "stage", "nukkad_stage", "Nukkad natak", WorldLayout.CellFeet(WorldLayout.Building("stage").Door), null));
            stage.StayUsableWhenResolved = true;
            stage.InteractRadius = 2.2f;
            var booth = Add(StoryObject.Create(MelaRoot, "booth", "booth", "Polling booth", WorldLayout.CellFeet(WorldLayout.Building("booth").Door), null));
            booth.StayUsableWhenResolved = true;
            booth.InteractRadius = 1.8f;
            WorldObjects.Label(MelaRoot, "POLLING BOOTH", new Vector2(89f, 7.6f), Color.white, 0.06f, 40);
            WorldObjects.Label(MelaRoot, "NUKKAD NATAK", new Vector2(79f, 23.6f), new Color(1f, 0.9f, 0.3f), 0.06f, 40);
        }
    }

    /// <summary>Gate sign explaining when a locked area opens.</summary>
    public class LockedSign : Interactable
    {
        public AreaId Area;
        public string Hint;
        public override float Radius => 1.8f;
        public override bool CanInteract => !GameManager.I.Areas.IsUnlocked(Area);
        public override string Prompt => $"Band hai: {Format.Area(Area)}";
        public override void Interact(PlayerController player) => GameManager.I.UI.Notify(Hint);
    }

    /// <summary>Water drops falling from a leaking tap (stops once the tap is fixed).</summary>
    public class DripEffect : MonoBehaviour
    {
        SpriteRenderer drop;
        StoryObject obj;
        float t;

        void Start()
        {
            obj = GetComponent<StoryObject>();
            drop = WorldObjects.CreateSprite("Drop", SpriteFactory.Drop, transform.position, transform, 1);
        }

        void Update()
        {
            bool on = obj != null && !obj.Resolved;
            drop.enabled = on;
            if (!on) return;
            t = (t + Time.deltaTime * 1.6f) % 1f;
            drop.transform.localPosition = new Vector3(0.18f, 0.45f - t * 0.45f, 0);
            drop.color = new Color(1, 1, 1, 1f - t * 0.6f);
        }
    }

    /// <summary>Flies buzzing over a garbage heap.</summary>
    public class FlyEffect : MonoBehaviour
    {
        readonly SpriteRenderer[] flies = new SpriteRenderer[3];
        float t;

        void Start()
        {
            for (int i = 0; i < flies.Length; i++) flies[i] = WorldObjects.CreateSprite("Fly", SpriteFactory.Fly, transform.position, transform, 1);
        }

        void Update()
        {
            t += Time.deltaTime;
            for (int i = 0; i < flies.Length; i++)
            {
                float a = t * (2.5f + i) + i * 2f;
                flies[i].transform.localPosition = new Vector3(Mathf.Cos(a) * 0.5f, 0.8f + Mathf.Sin(a * 1.7f) * 0.25f, 0);
            }
        }
    }

    public class FerrisSpin : MonoBehaviour
    {
        SpriteRenderer sr;
        float t;
        int frame;

        void Start() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            t += Time.deltaTime;
            if (t < 0.25f) return;
            t = 0f;
            frame = (frame + 1) % 33;
            sr.sprite = SpriteFactory.FerrisWheel(frame);
        }
    }
}
