using System;
using WorldGenerationEngineFinal;

public static class CaveConfig
{
    private static readonly ModConfig config = new ModConfig(version: 2, save: false);

    public static float moonLightScale = config.GetFloat("moonLightScale");

    public static readonly int RegionSize = 512;

    public static readonly int RegionSizeOffset = (int)Math.Log(RegionSize, 2);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = 150;

    public static int bedRockMargin = 4;

    public static int terrainMargin = 2;

    public static int minTunnelRadius = 2;

    public static int maxTunnelRadius = 10;

    public static int zombieSpawnMarginDeep = config.GetInt("zombieSpawnMarginDeep");

    public static int minSpawnTicksBeforeEnemySpawn = config.GetInt("minSpawnTicksBeforeEnemySpawn");

    public static bool enableCaveSpawn = config.GetBool("enableCaveSpawn");

    public static bool enableCaveBloodMoon = config.GetBool("enableCaveBloodMoon");

    public static int minSpawnDist = config.GetInt("minSpawnDist");

    public static int minSpawnDistBloodMoon = config.GetInt("minSpawnDistBloodMoon");

    public static float prefabScoreMultiplier = config.GetInt("prefabScoreMultiplier");

    // cave generation datas
    public static bool generateWater = false;

    public static bool generateCaves = false;

    public static float terrainOffset = 50;

    public static WorldBuilder.GenerationSelections caveNetworks;

    public static WorldBuilder.GenerationSelections caveEntrances;

    public static WorldBuilder.GenerationSelections caveWater;

}