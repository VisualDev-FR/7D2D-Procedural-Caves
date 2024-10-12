using System;
using WorldGenerationEngineFinal;

public class CaveCache
{
    public CaveEntrancesPlanner caveEntrancesPlanner;

    public CavePlanner cavePlanner;

    public CavePrefabManager cavePrefabManager;

    public RawHeightMap heightMap;

    public static CaveCache Instance;

    public CaveCache(WorldBuilder worldBuilder)
    {
        cavePlanner = new CavePlanner(worldBuilder);
        cavePrefabManager = new CavePrefabManager(worldBuilder);
        caveEntrancesPlanner = new CaveEntrancesPlanner(cavePrefabManager);
        heightMap = new RawHeightMap(worldBuilder);
    }

    public static void Init(WorldBuilder worldBuilder)
    {
        Instance = new CaveCache(worldBuilder);
    }

    public static void Clear()
    {
        Instance = null;

        GC.Collect();
    }
}