using WorldGenerationEngineFinal;

public static class CaveCache
{
    public static CaveEntrancesPlanner caveEntrancesPlanner;

    public static CavePlanner cavePlanner;

    public static void Init(WorldBuilder worldBuilder)
    {
        caveEntrancesPlanner = new CaveEntrancesPlanner(worldBuilder);
        cavePlanner = new CavePlanner(worldBuilder);
    }
}