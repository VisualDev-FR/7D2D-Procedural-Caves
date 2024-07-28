#pragma warning disable CS0162, CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305, IDE0035, IDE1006


using System.Collections.Generic;
using Random = System.Random;


public static class CaveBuilder
{
    public static int SEED = 1634735684;

    public static int worldSize = 2048;

    public static int RegionSize = 512;

    public static int regionGridSize = worldSize / RegionSize;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;

    public static float POINT_WIDTH = 5;

    public static int PREFAB_COUNT = worldSize / 5;

    public static Random rand = new Random(SEED);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = StreetTile.TileSize;

    public static int bedRockMargin = 2;

    public static int terrainMargin = 5;

    public static CaveNoise pathingNoise = CaveNoise.defaultNoise;

    public static bool TryPlacePrefab(ref CavePrefab prefab, List<CavePrefab> others)
    {
        int maxTries = 10;

        while (maxTries-- > 0)
        {
            prefab.SetRandomPosition(rand, worldSize);

            if (!prefab.OverLaps2D(others))
            {
                return true;
            }
        }

        return false;
    }

    public static PrefabCache GetRandomPrefabs(int count)
    {
        Log.Out("Start POIs placement...");

        var prefabCache = new PrefabCache();

        for (int i = 0; i < count; i++)
        {
            var prefab = new CavePrefab(prefabCache.Count + 1, rand);

            if (TryPlacePrefab(ref prefab, prefabCache.Prefabs))
                prefabCache.AddPrefab(prefab);
        }

        Log.Out($"{prefabCache.Count} / {PREFAB_COUNT} prefabs added");

        return prefabCache;
    }

}
