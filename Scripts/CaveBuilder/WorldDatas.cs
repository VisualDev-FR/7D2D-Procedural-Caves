using System.IO;

public class WorldDatas
{
    public readonly PathAbstractions.AbstractedLocation location;

    public readonly GameUtils.WorldInfo worldInfo;

    public readonly string name;

    public readonly int size;

    public readonly int seed;

    public string dtmPath => Path.Combine(location.FullPath, "dtm.raw");

    public string prefabsPath => Path.Combine(location.FullPath, "prefabs.xml");

    public WorldDatas(string worldName)
    {
        this.name = worldName;
        this.location = PathAbstractions.WorldsSearchPaths.GetLocation(worldName);
        this.worldInfo = GameUtils.WorldInfo.LoadWorldInfo(location);
        this.size = worldInfo.WorldSize.x;
        this.seed = GetWorldSeed();
    }

    private int GetWorldSeed()
    {
        if (!worldInfo.DynamicProperties.Contains("Seed"))
        {
            return 0;
        }

        return worldInfo.DynamicProperties.GetInt("Seed");
    }

    public void Debug()
    {
        Logging.Debug($"name: {name}");
        Logging.Debug($"size: {size}");
        Logging.Debug($"seed: {seed}");
    }
}