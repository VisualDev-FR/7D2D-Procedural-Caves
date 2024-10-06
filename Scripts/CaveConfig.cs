using System;

public static class CaveConfig
{
    public static bool generateWater = false;

    public static FastTags<TagGroup.Poi> tagCaveMarker = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagCaveWildernessEntrance = FastTags<TagGroup.Poi>.Parse("entrance, wilderness");

    public static FastTags<TagGroup.Poi> tagCaveUnderground = FastTags<TagGroup.Poi>.Parse("underground");

    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");

    public static FastTags<TagGroup.Poi> requiredCaveTags = FastTags<TagGroup.Poi>.CombineTags(tagCaveEntrance, tagCaveUnderground);

    public static FastTags<TagGroup.Poi> tagRwgStreetTile = FastTags<TagGroup.Poi>.Parse("streettile, rwgonly");

    public static int SEED = 1634735684;

    public static int worldSize = 2048;

    public static Vector3i HalfWorldSize => new Vector3i(worldSize / 2, 0, worldSize / 2);

    public static int regionGridSize => worldSize / RegionSize;

    public static int chunkRegionGridSize => RegionSize / ChunkSize;

    public static int TargetPrefabCount => worldSize / 5;

    public static readonly int RegionSize = 512;

    public static readonly int ChunkSize = 16;

    public static readonly int MIN_PREFAB_SIZE = 8;

    public static readonly int MAX_PREFAB_SIZE = 100;

    public static readonly float POINT_WIDTH = 5;

    public static Random rand = new Random(SEED);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = 150;

    public static int bedRockMargin = 4;

    public static int terrainMargin = 2;

}