public static class CaveConfig
{
    public static bool generateWater = false;

    public static FastTags<TagGroup.Poi> tagCaveMarker = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagCaveWildernessEntrance = FastTags<TagGroup.Poi>.Parse("entrance, wilderness");

    public static FastTags<TagGroup.Poi> tagCaveUnderground = FastTags<TagGroup.Poi>.Parse("underground");

    public static FastTags<TagGroup.Poi> requiredCaveTags = FastTags<TagGroup.Poi>.CombineTags(tagCaveEntrance, tagCaveUnderground);

    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");
}