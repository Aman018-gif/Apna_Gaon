using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    public enum BuildingStyle { Apartment, School, House, Office, Clinic, Shop, PartyOffice, TeaStall, CommunityHall, RationOffice, Stage, Booth }

    public struct BuildingDef
    {
        public string id;
        public string label;          // shown on the signboard
        public string mapLabel;       // short name for the map
        public BuildingStyle style;
        public RectInt rect;          // footprint in tiles (blocks movement)
        public NpcId owner;
        public bool hasOwner;
        public Vector2Int Door => WorldLayout.DoorOf(rect);
    }

    /// <summary>
    /// Hand-designed coordinates of Shanti Nagar (in tiles, y up). Map generation, NPC schedules, missions and visuals
    /// all read from here, so moving a building only needs one change. Run the EditMode tests after editing.
    /// </summary>
    public static class WorldLayout
    {
        public const int Width = 96, Height = 64;

        // ---- roads
        public static readonly RectInt MainRoad = new RectInt(1, 28, 94, 4);          // y 28..31, lane mark on y 29
        public static readonly RectInt LaneNorth = new RectInt(1, 46, 94, 2);         // "Gali No. 2"
        public static readonly RectInt LaneSouth = new RectInt(1, 12, 74, 2);         // "Gali No. 1" (stops at the Maidan)
        public static readonly int[] StreetX = { 20, 46, 72 };                         // vertical streets, 3 wide
        public static readonly int[] ZebraX = { 23, 49, 75 };                          // zebra crossings, 2 wide

        // ---- districts
        public static readonly RectInt Bazaar = new RectInt(23, 14, 23, 13);          // walled, gate at the top
        public static readonly RectInt Maidan = new RectInt(75, 1, 20, 26);           // walled, gate at the top
        public static readonly RectInt Park = new RectInt(50, 33, 21, 13);            // hedge with two openings
        public static readonly RectInt ParkPond = new RectInt(64, 40, 4, 3);
        public static readonly RectInt Playground = new RectInt(24, 33, 21, 4);

        public static readonly Dictionary<AreaId, Vector2Int[]> Gates = new Dictionary<AreaId, Vector2Int[]>
        {
            { AreaId.Bazaar, Cells(33, 26, 3, 1) },
            { AreaId.Maidan, Cells(83, 26, 3, 1) },
        };

        public static readonly Vector2Int[] ParkOpenings = { new Vector2Int(59, 33), new Vector2Int(60, 33), new Vector2Int(61, 33), new Vector2Int(59, 45), new Vector2Int(60, 45), new Vector2Int(61, 45) };

        // ---- buildings
        public static readonly List<BuildingDef> Buildings = new List<BuildingDef>();
        public static readonly List<int> VoterHouses = new List<int>();   // indices into Buildings

        public static BuildingDef Apartment => Building("apartment");
        public static BuildingDef WardOffice => Building("ward");
        public static Vector2Int PlayerDoor => Apartment.Door;
        public static Vector2 PlayerStart => CellFeet(PlayerDoor + new Vector2Int(0, -1));

        // ---- points of interest
        public static readonly Vector2Int[] Lights = { new Vector2Int(28, 37), new Vector2Int(38, 37), new Vector2Int(31, 37), new Vector2Int(55, 32), new Vector2Int(4, 36), new Vector2Int(86, 37) };
        public static readonly Vector2Int[] Taps = { new Vector2Int(25, 34), new Vector2Int(52, 35), new Vector2Int(15, 35), new Vector2Int(44, 19) };
        public static readonly Vector2Int[] Saplings = { new Vector2Int(54, 37), new Vector2Int(68, 36), new Vector2Int(66, 44) };
        public static readonly Vector2Int[] Garbage = { new Vector2Int(52, 44), new Vector2Int(43, 35), new Vector2Int(17, 26), new Vector2Int(70, 26), new Vector2Int(11, 48) };

        public static readonly Vector2Int ParkBoard = new Vector2Int(57, 34);           // "Renovation complete!" board (evidence photo)
        public static readonly Vector2Int RaddiPile = new Vector2Int(53, 24);           // behind Netaji's office (fallback receipt)
        public static readonly Vector2Int NoticeBoard = new Vector2Int(87, 35);
        public static readonly Vector2Int LuckyStall = new Vector2Int(33, 16);
        public static readonly Vector2Int RationQueue = new Vector2Int(41, 18);
        public static readonly Vector2Int PlacardTable = new Vector2Int(61, 2);
        public static readonly Vector2Int BullyCorner = new Vector2Int(53, 42);
        public static readonly Vector2Int ParkGather = new Vector2Int(60, 38);
        public static readonly Vector2Int[] MarchRoute = { new Vector2Int(35, 30), new Vector2Int(47, 46), new Vector2Int(73, 46), new Vector2Int(81, 35) };

        // ---- the Election Mela (inside the Maidan)
        public static readonly Vector2Int StallJugaad = new Vector2Int(78, 12);
        public static readonly Vector2Int StallVaada = new Vector2Int(84, 12);
        public static readonly Vector2Int StallMeera = new Vector2Int(90, 12);
        public static readonly Vector2Int StallChacha = new Vector2Int(78, 6);
        public static readonly Vector2Int MelaCenter = new Vector2Int(84, 16);
        public static readonly Vector2Int Ferris = new Vector2Int(90, 19);

        /// <summary>Decorative trees (each blocks its cell).</summary>
        public static readonly Vector2Int[] Trees =
        {
            new Vector2Int(51, 37), new Vector2Int(68, 34), new Vector2Int(51, 41), new Vector2Int(69, 41), new Vector2Int(56, 44), new Vector2Int(65, 37),
            new Vector2Int(2, 34), new Vector2Int(17, 34), new Vector2Int(19, 44), new Vector2Int(44, 44), new Vector2Int(24, 44),
            new Vector2Int(8, 55), new Vector2Int(18, 57), new Vector2Int(33, 58), new Vector2Int(44, 57), new Vector2Int(60, 58), new Vector2Int(78, 56), new Vector2Int(90, 58),
            new Vector2Int(2, 16), new Vector2Int(17, 16), new Vector2Int(50, 9), new Vector2Int(70, 9), new Vector2Int(93, 44), new Vector2Int(76, 44),
        };

        /// <summary>Election Mela stalls: (cell of the bottom-left corner, candidate or -1 for Chacha's stall).</summary>
        public static readonly (Vector2Int cell, int variant)[] MelaStalls =
        {
            (StallJugaad, 0), (StallVaada, 1), (StallMeera, 2), (StallChacha, 3), (new Vector2Int(88, 21), 4),
        };

        public static readonly Vector2Int[] RickshawLoop ={ new Vector2Int(3, 30), new Vector2Int(92, 30), new Vector2Int(92, 29), new Vector2Int(3, 29) };

        static WorldLayout()
        {
            Add("apartment", "Gulmohar Apartments", BuildingStyle.Apartment, new RectInt(3, 37, 12, 6), "Apartments");
            Add("school", "Shanti Nagar Public School", BuildingStyle.School, new RectInt(27, 38, 13, 5), "School");
            Add("ward", "Ward Office", BuildingStyle.Office, new RectInt(77, 38, 9, 5));
            Add("clinic", "Seva Clinic", BuildingStyle.Clinic, new RectInt(88, 38, 6, 4), "Clinic");
            Add("teastall", "Chacha ki Chai", BuildingStyle.TeaStall, new RectInt(6, 20, 6, 3), "Chai Stall");
            Add("kirana", "Lala Kirana Store", BuildingStyle.Shop, new RectInt(25, 20, 6, 4), "Lala Kirana");
            Add("ration", "Ration Office", BuildingStyle.RationOffice, new RectInt(38, 20, 6, 4), "Ration");
            Add("jugaad_office", "Jugaad Party Karyalay", BuildingStyle.PartyOffice, new RectInt(51, 19, 8, 4), "Netaji Office");
            Add("vaada_office", "Vaada Party Office", BuildingStyle.PartyOffice, new RectInt(62, 19, 8, 4), "Vaada Office");
            Add("hall", "Community Hall", BuildingStyle.CommunityHall, new RectInt(52, 4, 10, 5), "Comm. Hall");
            Add("stage", "Nukkad Natak Stage", BuildingStyle.Stage, new RectInt(76, 20, 6, 3), "Stage");
            Add("booth", "Polling Booth", BuildingStyle.Booth, new RectInt(86, 4, 6, 4), "Booth");

            // Houses: (x, y, owner or voter)
            House(3, 49, NpcId.Pintu); House(9, 49, null); House(14, 49, NpcId.Golu);
            House(24, 49, NpcId.Asha); House(30, 49, null); House(36, 49, null); House(41, 49, NpcId.Bunty);
            House(50, 49, null); House(56, 49, NpcId.Meera); House(62, 49, null); House(67, 49, NpcId.Fernandes);
            House(77, 49, NpcId.Jugaad); House(83, 49, null); House(89, 49, NpcId.Vaada);
            House(3, 6, NpcId.Raju); House(9, 6, null); House(14, 6, NpcId.Chacha);
            House(25, 6, NpcId.Lala); House(31, 6, null); House(37, 6, null);
            House(65, 6, null);
        }

        static void Add(string id, string label, BuildingStyle style, RectInt rect, string mapLabel = null)
            => Buildings.Add(new BuildingDef { id = id, label = label, style = style, rect = rect, mapLabel = mapLabel ?? label });

        static void House(int x, int y, NpcId? owner)
        {
            var b = new BuildingDef { id = "house" + Buildings.Count, style = BuildingStyle.House, rect = new RectInt(x, y, 4, 3) };
            if (owner.HasValue) { b.owner = owner.Value; b.hasOwner = true; b.label = NpcDatabase.FirstName(owner.Value) + "'s home"; }
            else { b.label = "House"; VoterHouses.Add(Buildings.Count); }
            Buildings.Add(b);
        }

        public static BuildingDef Building(string id)
        {
            foreach (var b in Buildings) if (b.id == id) return b;
            throw new KeyNotFoundException(id);
        }

        public static BuildingDef HomeOf(NpcId id)
        {
            if (id == NpcId.Sharma) return Apartment;
            foreach (var b in Buildings) if (b.hasOwner && b.owner == id) return b;
            throw new KeyNotFoundException("home of " + id);
        }

        public static Vector2 CellCenter(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);
        public static Vector2 CellFeet(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.2f);
        public static Vector2Int ToCell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        public static Vector2Int DoorOf(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y - 1);

        public static Vector2Int[] Cells(int x, int y, int w, int h)
        {
            var list = new List<Vector2Int>();
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    list.Add(new Vector2Int(x + i, y + j));
            return list.ToArray();
        }

        public static AreaId AreaAt(Vector2Int c)
        {
            if (Interior(Bazaar, c)) return AreaId.Bazaar;
            if (Interior(Maidan, c)) return AreaId.Maidan;
            return AreaId.Mohalla;
        }

        public static bool Interior(RectInt r, Vector2Int c) => c.x > r.xMin && c.x < r.xMax - 1 && c.y > r.yMin && c.y < r.yMax - 1;

        /// <summary>Friendly name of the place at a cell, for the HUD.</summary>
        public static string PlaceName(Vector2Int c)
        {
            var area = AreaAt(c);
            if (area != AreaId.Mohalla) return Format.Area(area);
            if (Park.Contains(c)) return "Gulmohar Park";
            if (MainRoad.Contains(c) || c.y == 27 || c.y == 32) return "Main Road";
            if (LaneNorth.Contains(c)) return "Gali No. 2";
            if (LaneSouth.Contains(c)) return "Gali No. 1";
            if (c.x >= 24 && c.x <= 44 && c.y >= 33 && c.y <= 45) return "School Area";
            if (c.x >= 76 && c.y >= 33 && c.y <= 45) return "Ward Office Chowk";
            if (c.x >= 49 && c.x <= 71 && c.y >= 14 && c.y <= 26) return "Party Office Road";
            return "Shanti Nagar";
        }

        /// <summary>Named places for the full map.</summary>
        public static IEnumerable<(string name, Vector2 cell)> Landmarks()
        {
            foreach (var b in Buildings)
                if (b.style != BuildingStyle.House) yield return (b.mapLabel, new Vector2(b.rect.x + b.rect.width / 2f, b.rect.y + b.rect.height / 2f));
            yield return ("Gulmohar Park", new Vector2(60.5f, 41.5f));
            yield return ("BAZAAR GALI", new Vector2(34.5f, 16f));
            yield return ("MELA MAIDAN", new Vector2(85f, 16f));
            yield return ("Main Road", new Vector2(10f, 30f));
        }
    }
}
