using System;
using WorldGenerationEngineFinal;

public static class CaveConfig
{
    private static ModConfig config = new ModConfig("TheDescent");

    public static Logging.Logger logger = Logging.CreateLogger("TheDescent", LoggingLevel.DEBUG);

    public static FastTags<TagGroup.Poi> tagCaveMarker = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagCaveWildernessEntrance = FastTags<TagGroup.Poi>.Parse("entrance, wilderness");

    public static FastTags<TagGroup.Poi> tagCaveUnderground = FastTags<TagGroup.Poi>.Parse("underground");

    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");

    public static FastTags<TagGroup.Poi> tagCaveAir = FastTags<TagGroup.Poi>.Parse("caveair");

    public static FastTags<TagGroup.Poi> requiredCaveTags = FastTags<TagGroup.Poi>.CombineTags(tagCaveEntrance, tagCaveUnderground);

    public static FastTags<TagGroup.Poi> tagRwgStreetTile = FastTags<TagGroup.Poi>.Parse("streettile, rwgonly");

    public static readonly int RegionSize = 512;

    public static readonly int RegionSizeOffset = (int)Math.Log(RegionSize, 2);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = 150;

    public static int bedRockMargin = 4;

    public static int terrainMargin = 2;

    public static int minTunnelRadius = 2;

    public static int maxTunnelRadius = 10;

    // the min deep (from terrain height) to spawn cave zombies
    public static int zombieSpawnMarginDeep = 5;

    public static int minSpawnTicksBeforeEnemySpawn = config.GetInt("minSpawnTicksBeforeEnemySpawn");

    public static bool enableCaveSpawn = config.GetBool("enableCaveSpawn");

    public static bool enableCaveBloodMoon = config.GetBool("enableCaveBloodMoon");

    public static int minSpawnDist = config.GetInt("minSpawnDist");

    public static int minSpawnDistBloodMoon = config.GetInt("minSpawnDistBloodMoon");

    // cave generation datas
    public static bool generateWater = false;

    public static bool generateCaves = false;

    public static float terrainOffset = 50;

    public static WorldBuilder.GenerationSelections caveNetworks;

    public static WorldBuilder.GenerationSelections caveEntrances;

    public static WorldBuilder.GenerationSelections caveWater;

}