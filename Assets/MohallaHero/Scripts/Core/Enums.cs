namespace MohallaHero
{
    public enum Gender { Male, Female }

    /// <summary>The four karma stats. Every story choice can move them up or down.</summary>
    public enum KarmaStat { Civic, Green, Courage, Honesty }

    /// <summary>
    /// The neighbourhood's people. All are fictional (spec: no real politicians).
    /// Ids are also the lowercase names used in the .ink stories, e.g. <c>trust("raju")</c>.
    /// </summary>
    public enum NpcId { Pintu, Chacha, Jugaad, Vaada, Asha, Lala, Sharma, Raju, Golu, Bunty, Fernandes, Meera }

    /// <summary>Mohalla is always open; Bazaar and Maidan are behind barricades until the story opens them.</summary>
    public enum AreaId { Mohalla, Bazaar, Maidan }

    public enum DayPhase { Morning, Afternoon, Evening, Night }

    public enum TileKind
    {
        Asphalt, LaneMark, Zebra, Sidewalk, Grass, ParkPath, Ground, Plaza, Water, Wall, Hedge, Flowers
    }

    public enum Candidate { Jugaad, Vaada, Meera, Nota }

    public enum Ending { None, MohallaHero, JagrukNagrik, AadhaSach, JugaadRaj }

    public enum MiniGame { None, WasteSorting, FakeNews, Slogan }
}
