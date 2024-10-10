using System;
using WorldGenerationEngineFinal;

public static class CaveCache
{
    public static CaveEntrancesPlanner caveEntrancesPlanner;

    public static CavePlanner cavePlanner;

    public static CavePrefabManager cavePrefabManager;

    public static RawHeightMap heightMap;

    public static void Init(WorldBuilder worldBuilder)
    {
        cavePlanner = new CavePlanner(worldBuilder);
        cavePrefabManager = new CavePrefabManager(worldBuilder);
        caveEntrancesPlanner = new CaveEntrancesPlanner(cavePrefabManager);
        heightMap = new RawHeightMap(worldBuilder);
    }

    public static void Clear()
    {
        caveEntrancesPlanner = null;
        cavePlanner = null;
        cavePrefabManager = null;

        GC.Collect();
    }
}