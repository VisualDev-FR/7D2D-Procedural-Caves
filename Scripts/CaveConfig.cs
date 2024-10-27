using UnityEngine;

public static class CaveConfig
{
    public static FastTags<TagGroup.Poi> tagCaveMarker = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagCaveWildernessEntrance = FastTags<TagGroup.Poi>.Parse("entrance, wilderness");

    public static FastTags<TagGroup.Poi> tagCaveUnderground = FastTags<TagGroup.Poi>.Parse("underground");

    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");

    public static FastTags<TagGroup.Poi> requiredCaveTags = FastTags<TagGroup.Poi>.CombineTags(tagCaveEntrance, tagCaveUnderground);

    public static FastTags<TagGroup.Poi> tagRwgStreetTile = FastTags<TagGroup.Poi>.Parse("streettile, rwgonly");

    public static readonly int RegionSize = 512;

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = 150;

    public static int bedRockMargin = 4;

    public static int terrainMargin = 2;

    // the min deep (from terrain height) to spawn cave zombies
    public static int zombieSpawnMarginDeep = 5;

    public static bool generateWater = false;

    public static float terrainOffset = 50;

    public static bool enableCaveBloodMoon = true;

    public static int minSpawnDist = 15;

    public class CaveLightConfig
    {
        public static Vector2 ambientInsideEquatorScale = new Vector2(0, 0);

        public static Vector2 ambientInsideGroundScale = new Vector2(0, 0);

        public static Vector2 ambientInsideSkyScale = new Vector2(0, 0);
    }
}