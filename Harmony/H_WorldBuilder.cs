using System;
using System.Collections;
using HarmonyLib;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WorldBuilder), "GenerateData")]
public static class WorldBuilder_GenerateData
{
    private static readonly WorldBuilder worldBuilder = WorldBuilder.Instance;

    private static float[] HeightMap;

    private static float[] terrainDest;

    private static float[] terrainWaterDest;

    private static float[] waterDest;

    public static readonly float terrainOffset = 50;

    public static bool Prefix(ref IEnumerator __result)
    {
        Log.Out("Patch rand world generator!");
        __result = GenerateData();
        return false;
    }

    public static IEnumerator GenerateData()
    {
        CavePlanner.Init();
        PatchWaterHeight();

        yield return worldBuilder.Init();
        yield return worldBuilder.SetMessage(string.Format(Localization.Get("xuiWorldGenerationGenerating"), worldBuilder.WorldName), _logToConsole: true);
        yield return worldBuilder.generateTerrain();

        if (worldBuilder.IsCanceled)
            yield break;

        worldBuilder.initStreetTiles();

        if (worldBuilder.IsCanceled)
            yield break;

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return PrefabManager.LoadPrefabs();
            PrefabManager.ShufflePrefabData(worldBuilder.Seed);
            yield return null;
            PathingUtils.SetupPathingGrid();
        }
        else
        {
            PrefabManager.ClearDisplayed();
        }

        StoreHeightMaps();
        PatchHeightMaps();

        if (worldBuilder.Towns != 0)
        {
            yield return TownPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
        }

        ResetHeightMaps();

        yield return worldBuilder.GenerateTerrainLast();

        PatchHeightMaps();

        if (worldBuilder.IsCanceled)
            yield break;

        yield return POISmoother.SmoothStreetTiles();

        if (worldBuilder.IsCanceled)
            yield break;

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return HighwayPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return TownPlanner.SpawnPrefabs();

            if (worldBuilder.IsCanceled)
                yield break;
        }

        if (worldBuilder.Wilderness != 0)
        {
            yield return WildernessPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return worldBuilder.smoothWildernessTerrain();
            yield return WildernessPathPlanner.Plan(worldBuilder.Seed);
        }
        int num = 12 - worldBuilder.playerSpawns.Count;
        if (num > 0)
        {
            foreach (StreetTile item in WorldBuilder.CalcPlayerSpawnTiles())
            {
                if (worldBuilder.CreatePlayerSpawn(item.WorldPositionCenter, _isFallback: true) && --num <= 0)
                {
                    break;
                }
            }
        }

        GC.Collect();

        yield return worldBuilder.SetMessage("Draw Roads", _logToConsole: true);
        yield return worldBuilder.DrawRoads(worldBuilder.dest);

        if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
        {
            yield return worldBuilder.SetMessage("Smooth Road Terrain", _logToConsole: true);
            yield return WorldBuilder.smoothRoadTerrain(worldBuilder.dest, worldBuilder.HeightMap, worldBuilder.WorldSize);
        }

        yield return CavePlanner.GenerateCaveMap();

        worldBuilder.paths.Clear();
        worldBuilder.wildernessPaths.Clear();

        yield return worldBuilder.FinalizeWater();

        GC.Collect();

        Log.Out("RWG final in {0}:{1:00}, r={2:x}", worldBuilder.totalMS.Elapsed.Minutes, worldBuilder.totalMS.Elapsed.Seconds, Rand.Instance.PeekSample());
    }

    private static float ClampHeight(float height)
    {
        return terrainOffset + (255f - terrainOffset) * height / 255f;
    }

    private static void PatchWaterHeight()
    {
        CaveUtils.SetField<WorldBuilder>(
            WorldBuilder.Instance,
            "WaterHeight",
            (int)ClampHeight(WorldBuilder.Instance.WaterHeight)
        );
    }

    private static void StoreHeightMaps()
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

    private static void PatchHeightMaps()
    {
        for (int i = 0; i < worldBuilder.HeightMap.Length; i++)
        {
            worldBuilder.HeightMap[i] = ClampHeight(worldBuilder.HeightMap[i]);
        }
    }

    private static void ResetHeightMaps()
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
        CavePlanner.SaveCaveMap();
        return true;
    }
}
