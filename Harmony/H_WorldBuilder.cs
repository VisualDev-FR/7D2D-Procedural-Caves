using System;
using System.Collections;
using HarmonyLib;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WorldBuilder), "GenerateData")]
public static class WorldBuilder_GenerateData
{
    private static WorldBuilder worldBuilder;

    public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
    {
        worldBuilder = __instance;

        CaveUtils.Assert(worldBuilder != null, "null world builder");

        Log.Out("Patch rand world generator!");
        __result = GenerateData();
        return false;
    }

    public static IEnumerator GenerateData()
    {
        PatchWaterHeight();

        yield return worldBuilder.Init();
        yield return worldBuilder.SetMessage(string.Format(Localization.Get("xuiWorldGenerationGenerating"), worldBuilder.WorldName), _logToConsole: true);
        yield return worldBuilder.generateTerrain();

        if (worldBuilder.IsCanceled)
            yield break;

        worldBuilder.initStreetTiles();

        CaveCache.Init(worldBuilder);

        if (worldBuilder.IsCanceled)
            yield break;

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return H_PrefabManager.LoadPrefabs(worldBuilder.PrefabManager, CaveCache.Instance.cavePrefabManager);
            worldBuilder.PrefabManager.ShufflePrefabData(worldBuilder.Seed);
            yield return null;
            worldBuilder.PathingUtils.SetupPathingGrid();
        }
        else
        {
            worldBuilder.PrefabManager.ClearDisplayed();
        }

        StoreHeightMaps(out float[] HeightMap, out float[] terrainDest, out float[] terrainWaterDest, out float[] waterDest);
        PatchHeightMaps();

        if (worldBuilder.Towns != 0)
        {
            yield return worldBuilder.TownPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
        }

        ResetHeightMaps(HeightMap, terrainDest, terrainWaterDest, waterDest);

        yield return worldBuilder.GenerateTerrainLast();

        PatchHeightMaps();

        if (worldBuilder.IsCanceled)
            yield break;

        yield return worldBuilder.POISmoother.SmoothStreetTiles();

        if (worldBuilder.IsCanceled)
            yield break;

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return worldBuilder.HighwayPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return worldBuilder.TownPlanner.SpawnPrefabs();

            if (worldBuilder.IsCanceled)
                yield break;
        }

        if (worldBuilder.Wilderness != 0)
        {
            yield return worldBuilder.WildernessPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return worldBuilder.smoothWildernessTerrain();
            yield return worldBuilder.WildernessPathPlanner.Plan(worldBuilder.Seed);
        }
        int num = 12 - worldBuilder.playerSpawns.Count;
        if (num > 0)
        {
            foreach (StreetTile item in worldBuilder.CalcPlayerSpawnTiles())
            {
                if (worldBuilder.CreatePlayerSpawn(item.WorldPositionCenter, _isFallback: true) && --num <= 0)
                {
                    break;
                }
            }
        }

        GCUtils.Collect();

        yield return worldBuilder.SetMessage("Draw Roads", _logToConsole: true);
        yield return worldBuilder.DrawRoads(worldBuilder.dest);

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return worldBuilder.SetMessage("Smooth Road Terrain", _logToConsole: true);
            yield return worldBuilder.smoothRoadTerrain(worldBuilder.dest, worldBuilder.HeightMap, worldBuilder.WorldSize);
        }

        yield return CaveCache.Instance.cavePlanner.GenerateCaveMap(CaveCache.Instance.cavePrefabManager, CaveCache.Instance.heightMap);

        worldBuilder.paths.Clear();
        worldBuilder.wildernessPaths.Clear();

        yield return worldBuilder.FinalizeWater();

        GCUtils.Collect();

        Log.Out("RWG final in {0}:{1:00}, r={2:x}", worldBuilder.totalMS.Elapsed.Minutes, worldBuilder.totalMS.Elapsed.Seconds, Rand.Instance.PeekSample());
    }

    private static float ClampHeight(float height)
    {
        return CaveConfig.terrainOffset + (255f - CaveConfig.terrainOffset) * height / 255f;
    }

    private static void PatchWaterHeight()
    {
        CaveUtils.SetField<WorldBuilder>(
            worldBuilder,
            "WaterHeight",
            (int)ClampHeight(worldBuilder.WaterHeight)
        );
    }

    private static void PatchHeightMaps()
    {
        for (int i = 0; i < worldBuilder.HeightMap.Length; i++)
        {
            worldBuilder.HeightMap[i] = ClampHeight(worldBuilder.HeightMap[i]);
        }
    }

    private static void StoreHeightMaps(out float[] HeightMap, out float[] terrainDest, out float[] terrainWaterDest, out float[] waterDest)
    {
        var arraySizes = worldBuilder.HeightMap.Length;

        HeightMap = new float[worldBuilder.HeightMap.Length];

        // TODO: Try to remove those lines
        waterDest = new float[worldBuilder.waterDest.Length];
        terrainDest = new float[worldBuilder.terrainDest.Length];
        terrainWaterDest = new float[worldBuilder.terrainWaterDest.Length];

        Array.Copy(worldBuilder.HeightMap, HeightMap, arraySizes);

        // TODO: Try to remove those lines
        Array.Copy(worldBuilder.waterDest, waterDest, arraySizes);
        Array.Copy(worldBuilder.terrainDest, terrainDest, arraySizes);
        Array.Copy(worldBuilder.terrainWaterDest, terrainWaterDest, arraySizes);
    }

    private static void ResetHeightMaps(float[] HeightMap, float[] terrainDest, float[] terrainWaterDest, float[] waterDest)
    {
        var arraySizes = worldBuilder.HeightMap.Length;

        Array.Copy(HeightMap, worldBuilder.HeightMap, arraySizes);

        // TODO: Try to remove those lines
        Array.Copy(terrainDest, worldBuilder.terrainDest, arraySizes);
        Array.Copy(terrainWaterDest, worldBuilder.terrainWaterDest, arraySizes);
        Array.Copy(waterDest, worldBuilder.waterDest, arraySizes);
    }

}


[HarmonyPatch(typeof(WorldBuilder), "saveRawHeightmap")]
public static class WorldBuilder_saveRawHeightmap
{
    public static bool Prefix()
    {
        CaveCache.Instance.cavePlanner.SaveCaveMap();
        CaveCache.Clear();
        return true;
    }
}
