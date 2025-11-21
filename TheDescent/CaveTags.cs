public class CaveTags
{
    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");

    public static FastTags<TagGroup.Poi> tagCaveMarker = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagWildernessEntrance = FastTags<TagGroup.Poi>.Parse("wilderness,entrance");

    public static FastTags<TagGroup.Poi> tagUnderground = FastTags<TagGroup.Poi>.Parse("underground");

    public static FastTags<TagGroup.Poi> tagCaveUnderground = FastTags<TagGroup.Poi>.Parse("cave,underground");

    public static FastTags<TagGroup.Poi> requiredCaveTags = FastTags<TagGroup.Poi>.CombineTags(tagCaveEntrance, tagUnderground);

    public static FastTags<TagGroup.Poi> tagCaveTrader = FastTags<TagGroup.Poi>.Parse("cave,underground,trader");
}