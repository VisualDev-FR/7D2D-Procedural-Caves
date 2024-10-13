using WorldGenerationEngineFinal;

public class CaveCache
{
    public CaveEntrancesPlanner caveEntrancesPlanner;

    public CavePlanner cavePlanner;

    public CavePrefabManager cavePrefabManager;

    public RawHeightMap heightMap;

    public CaveCache(WorldBuilder worldBuilder)
    {
        cavePlanner = new CavePlanner(worldBuilder);
        cavePrefabManager = new CavePrefabManager(worldBuilder);
        caveEntrancesPlanner = new CaveEntrancesPlanner(cavePrefabManager);
        heightMap = new RawHeightMap(worldBuilder);
    }
}