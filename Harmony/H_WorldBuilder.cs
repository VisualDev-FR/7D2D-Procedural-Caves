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

    public static bool Prefix(ref IEnumerator __result)
    {
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

        StoreHeightMaps();
        PatchHeightMaps();

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

        worldBuilder.paths.Clear();
        worldBuilder.wildernessPaths.Clear();

        yield return worldBuilder.FinalizeWater();

        GC.Collect();

        Log.Out("RWG final in {0}:{1:00}, r={2:x}", worldBuilder.totalMS.Elapsed.Minutes, worldBuilder.totalMS.Elapsed.Seconds, Rand.Instance.PeekSample());
    }

    private static void PatchWaterHeight()
    {
        CaveUtils.SetField<WorldBuilder>(
            WorldBuilder.Instance,
            "WaterHeight",
            (int)CaveUtils.ClampHeight(WorldBuilder.Instance.WaterHeight)
        );
    }

    private static void StoreHeightMaps()
    {
        var arraySizes = worldBuilder.HeightMap.Length;

        HeightMap = new float[worldBuilder.HeightMap.Length];
        waterDest = new float[worldBuilder.waterDest.Length];
        terrainDest = new float[worldBuilder.terrainDest.Length];
        terrainWaterDest = new float[worldBuilder.terrainWaterDest.Length];

        Array.Copy(worldBuilder.HeightMap, HeightMap, arraySizes);
        Array.Copy(worldBuilder.waterDest, waterDest, arraySizes);
        Array.Copy(worldBuilder.terrainDest, terrainDest, arraySizes);
        Array.Copy(worldBuilder.terrainWaterDest, terrainWaterDest, arraySizes);
    }

    private static void PatchHeightMaps()
    {
        for (int i = 0; i < worldBuilder.HeightMap.Length; i++)
        {
            worldBuilder.HeightMap[i] = CaveUtils.ClampHeight(worldBuilder.HeightMap[i]);
            worldBuilder.waterDest[i] = CaveUtils.ClampHeight(worldBuilder.waterDest[i] * 255f) / 255f;
            worldBuilder.terrainDest[i] = CaveUtils.ClampHeight(worldBuilder.terrainDest[i] * 255f) / 255f;
            worldBuilder.terrainWaterDest[i] = CaveUtils.ClampHeight(worldBuilder.terrainWaterDest[i] * 255f) / 255f;
        }
    }

    private static void ResetHeightMaps()
    {
        var arraySizes = worldBuilder.HeightMap.Length;

        Array.Copy(HeightMap, worldBuilder.HeightMap, arraySizes);
        Array.Copy(terrainDest, worldBuilder.terrainDest, arraySizes);
        Array.Copy(terrainWaterDest, worldBuilder.terrainWaterDest, arraySizes);
        Array.Copy(waterDest, worldBuilder.waterDest, arraySizes);
    }

}


// [HarmonyPatch(typeof(WorldBuilder), "initStreetTiles")]
// public static class WorldBuilder_initStreetTiles
// {
//     public static void Postfix()
//     {

//         foreach (var st in WorldBuilder.Instance.StreetTileMap)
//         {
//             Log.Out($"[Cave] streetTile: '{st.PrefabName}', position: {st.WorldPosition}, height: {st.PositionHeight}");
//             // st.PositionHeight =
//             st.PositionHeight = CaveUtils.ClampHeight(st.PositionHeight);
//         }
//     }
// }

// [HarmonyPatch(typeof(POISmoother), "SmoothStreetTiles")]
// public static class WildernessPlanner_Plan
// {
//     public static bool Prefix()
//     {
//         var worldBuilder = WorldBuilder.Instance;

//         for (int i = 0; i < worldBuilder.HeightMap.Length; i++)
//         {
//             worldBuilder.HeightMap[i] = CaveUtils.ClampHeight(worldBuilder.HeightMap[i]);
//             worldBuilder.terrainDest[i] = CaveUtils.ClampHeight(worldBuilder.terrainDest[i] * 255f) / 255f;
//             worldBuilder.terrainWaterDest[i] = CaveUtils.ClampHeight(worldBuilder.terrainWaterDest[i] * 255f) / 255f;
//             worldBuilder.waterDest[i] = CaveUtils.ClampHeight(worldBuilder.waterDest[i] * 255f) / 255f;
//         }

//         return true;
//     }
// }
