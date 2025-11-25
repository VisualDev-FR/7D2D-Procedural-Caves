using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using WorldGenerationEngineFinal;



[HarmonyPatch(typeof(WorldBuilder), "generateBaseStamps")]
public static class H_WorldBuilder_GenerateData
{
    private static WorldBuilder worldBuilder;

    private static int WorldSize => worldBuilder.WorldSize;

    private static int BiomeSize => worldBuilder.BiomeSize;

    private static int WaterHeight => worldBuilder.WaterHeight;

    private static int Seed => worldBuilder.Seed;

    private static bool GenWaterBorderE => worldBuilder.GenWaterBorderE;

    private static bool GenWaterBorderN => worldBuilder.GenWaterBorderN;

    private static bool GenWaterBorderS => worldBuilder.GenWaterBorderS;

    private static bool GenWaterBorderW => worldBuilder.GenWaterBorderW;

    private static float[] terrainDest => worldBuilder.terrainDest;

    private static float[] waterDest => worldBuilder.waterDest;

    private static float[] terrainWaterDest => worldBuilder.terrainDest;

    private static Color32[] radDest => worldBuilder.radDest;

    private static Color32[] biomeDest => worldBuilder.biomeDest;

    private static Dictionary<BiomeType, Color32> biomeColors => worldBuilder.biomeColors;

    private static DataMap<BiomeType> biomeMap => worldBuilder.biomeMap;

    private static StampManager StampManager => worldBuilder.StampManager;

    private static StampGroup waterLayer => worldBuilder.waterLayer;

    public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
    {
        if (!CaveConfig.generateCaves)
        {
            return true;
        }

        worldBuilder = __instance;
        __result = generateBaseStamps((int)CaveConfig.terrainOffset);

        return false;
    }

    public static IEnumerator generateBaseStamps(int terrainHeight)
    {
        worldBuilder.WaterHeight = terrainHeight - 5;

        StampManager.GetStamp("ground");

        for (int i = 0; i < terrainDest.Length; i++)
        {
            terrainDest[i] = terrainHeight / 256f;
            terrainWaterDest[i] = terrainHeight / 256f;
        }

        // -----------------------------------
        // ALL BELOW THIS LINE IS VANILLA CODE
        // -----------------------------------
        Vector2 sizeMinMax = new Vector2(1.5f, 3.5f);
        worldBuilder.thisWorldProperties.ParseVec("border.scale", ref sizeMinMax);
        int borderStep = 512;
        worldBuilder.thisWorldProperties.ParseInt("border_step_distance", ref borderStep);

        Task terrainBorderTask = new Task(() =>
        {
            MicroStopwatch microStopwatch3 = new MicroStopwatch(_bStart: true);
            new MicroStopwatch(_bStart: false);
            new MicroStopwatch(_bStart: false);
            Rand rand2 = new Rand(Seed + 1);
            int num7 = borderStep;
            int num8 = num7 / 2;
            for (int num9 = 0; num9 < WorldSize + num7; num9 += num7)
            {
                if (worldBuilder.IsCanceled)
                {
                    break;
                }
                if (!GenWaterBorderE || !GenWaterBorderW || !GenWaterBorderN || !GenWaterBorderS)
                {
                    for (int num10 = 0; num10 < 4; num10++)
                    {
                        TranslationData translationData = null;
                        if (num10 == 0 && !GenWaterBorderS)
                        {
                            translationData = new TranslationData(num9 + rand2.Range(0, num8), rand2.Range(0, num8), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num10 == 1 && !GenWaterBorderN)
                        {
                            translationData = new TranslationData(num9 + rand2.Range(0, num8), WorldSize - rand2.Range(0, num8), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num10 == 2 && !GenWaterBorderW)
                        {
                            translationData = new TranslationData(rand2.Range(0, num8), num9 + rand2.Range(0, num8), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num10 == 3 && !GenWaterBorderE)
                        {
                            translationData = new TranslationData(WorldSize - rand2.Range(0, num8), num9 + rand2.Range(0, num8), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        if (translationData != null)
                        {
                            string text = biomeMap.data[Mathf.Clamp(translationData.x / 1024, 0, WorldSize / 1024 - 1), Mathf.Clamp(translationData.y / 1024, 0, WorldSize / 1024 - 1)].ToString();
                            if (StampManager.TryGetStamp(text + "_land_border", out var _output, rand2) || StampManager.TryGetStamp("land_border", out _output, rand2))
                            {
                                StampManager.DrawStamp(terrainDest, terrainWaterDest, new Stamp(worldBuilder, _output, translationData));
                            }
                        }
                    }
                }
                if (GenWaterBorderE || GenWaterBorderW || GenWaterBorderN || GenWaterBorderS)
                {
                    for (int num11 = 0; num11 < 4; num11++)
                    {
                        TranslationData translationData2 = null;
                        if (num11 == 0 && GenWaterBorderS)
                        {
                            translationData2 = new TranslationData(num9, rand2.Range(0, num8 / 2), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num11 == 1 && GenWaterBorderN)
                        {
                            translationData2 = new TranslationData(num9, WorldSize - rand2.Range(0, num8 / 2), rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num11 == 2 && GenWaterBorderW)
                        {
                            translationData2 = new TranslationData(rand2.Range(0, num8 / 2), num9, rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        else if (num11 == 3 && GenWaterBorderE)
                        {
                            translationData2 = new TranslationData(WorldSize - rand2.Range(0, num8 / 2), num9, rand2.Range(sizeMinMax.x, sizeMinMax.y), rand2.Angle());
                        }
                        if (translationData2 != null)
                        {
                            string text2 = biomeMap.data[Mathf.Clamp(translationData2.x / 1024, 0, WorldSize / 1024 - 1), Mathf.Clamp(translationData2.y / 1024, 0, WorldSize / 1024 - 1)].ToString();
                            if (StampManager.TryGetStamp(text2 + "_water_border", out var _output2, rand2) || StampManager.TryGetStamp("water_border", out _output2, rand2))
                            {
                                StampManager.DrawStamp(terrainDest, terrainWaterDest, new Stamp(worldBuilder, _output2, translationData2));
                                Stamp stamp2 = new Stamp(worldBuilder, _output2, translationData2, _isCustomColor: true, new Color32(0, 0, (byte)WaterHeight, 0), 0.1f, _isWater: true);
                                waterLayer.Stamps.Add(stamp2);
                                StampManager.DrawWaterStamp(stamp2, waterDest, WorldSize);
                            }
                        }
                    }
                }
            }
            rand2.Free();
            Log.Out("generateBaseStamps terrainBorderThread in {0}", (float)microStopwatch3.ElapsedMilliseconds * 0.001f);
        });
        terrainBorderTask.Start();
        Task radTask = new Task(() =>
        {
            MicroStopwatch microStopwatch2 = new MicroStopwatch(_bStart: true);
            Color color2 = new Color(1f, 0f, 0f, 0f);
            int num6 = WorldSize - 1;
            for (int n = 0; n < WorldSize; n++)
            {
                radDest[n] = color2;
                radDest[n + num6 * WorldSize] = color2;
                radDest[n * WorldSize] = color2;
                radDest[n * WorldSize + num6] = color2;
            }
            Log.Out("generateBaseStamps radThread in {0}", (float)microStopwatch2.ElapsedMilliseconds * 0.001f);
        });
        radTask.Start();
        Task[] biomeTasks = new Task[1]
        {
            new Task( () =>
            {
                MicroStopwatch microStopwatch = new MicroStopwatch(_bStart: true);
                Rand rand = new Rand(Seed + 3);
                Color32 color = biomeColors[BiomeType.forest];
                for (int k = 0; k < biomeDest.Length; k++)
                {
                    biomeDest[k] = color;
                }
                RawStamp stamp = StampManager.GetStamp("filler_biome", rand);
                if (stamp != null)
                {
                    int num = WorldSize / 256;
                    int num2 = 32 / 2;
                    float num3 = 32f / (float)stamp.width * 1.5f;
                    for (int l = 0; l < num; l++)
                    {
                        int num4 = l * 256 / 8;
                        for (int m = 0; m < num; m++)
                        {
                            int num5 = m * 256 / 8;
                            BiomeType biomeType = biomeMap.data[m, l];
                            if (biomeType != BiomeType.none)
                            {
                                float scale = num3 + rand.Range(0f, 0.2f);
                                float angle = rand.Range(0, 4) * 90 + rand.Range(-20, 20);
                                StampManager.DrawBiomeStamp(biomeDest, stamp.alphaPixels, num5 + num2, num4 + num2, BiomeSize, BiomeSize, stamp.width, stamp.height, scale, biomeColors[biomeType], 0.1f, angle);
                            }
                        }
                    }
                }
                rand.Free();
                Log.Out("generateBaseStamps biomeThreads in {0}", (float)microStopwatch.ElapsedMilliseconds * 0.001f);
            })
        };
        Task[] array = biomeTasks;
        for (int j = 0; j < array.Length; j++)
        {
            array[j].Start();
        }
        bool isAnyAlive = true;
        while (isAnyAlive || !terrainBorderTask.IsCompleted || !radTask.IsCompleted)
        {
            isAnyAlive = false;
            array = biomeTasks;
            foreach (Task task in array)
            {
                isAnyAlive |= !task.IsCompleted;
            }
            if (!terrainBorderTask.IsCompleted && isAnyAlive)
            {
                yield return worldBuilder.SetMessage(Localization.Get("xuiRwgCreatingTerrainAndBiomeStamps"));
            }
            else if (!terrainBorderTask.IsCompleted && !isAnyAlive)
            {
                yield return worldBuilder.SetMessage(Localization.Get("xuiRwgCreatingTerrainStamps"));
            }
            else
            {
                yield return worldBuilder.SetMessage(Localization.Get("xuiRwgCreatingBiomeStamps"));
            }
        }
    }

}


[HarmonyPatch(typeof(WorldBuilder), "GenerateData")]
public static class WorldBuilder_GenerateData
{
    private static WorldBuilder worldBuilder;

    private static CaveBuilder caveBuilder;

    public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
    {
        if (!CaveConfig.generateCaves)
        {
            return true;
        }

        worldBuilder = __instance;

        CaveUtils.Assert(worldBuilder != null, "null world builder");

        Logging.Info("Patch rand world generator!");
        __result = GenerateData();
        return false;
    }

    public static IEnumerator GenerateData()
    {
        yield return worldBuilder.Init();
        yield return worldBuilder.SetMessage(string.Format(Localization.Get("xuiWorldGenerationGenerating"), worldBuilder.WorldName), _logToConsole: true);
        yield return worldBuilder.GenerateTerrain();

        if (worldBuilder.IsCanceled)
            yield break;

        worldBuilder.InitStreetTiles();

        caveBuilder = new CaveBuilder(worldBuilder);

        if (worldBuilder.IsCanceled)
            yield break;

        bool hasPOIs = worldBuilder.Towns != 0 || worldBuilder.Wilderness != WorldBuilder.GenerationSelections.None;
        if (hasPOIs)
        {
            yield return H_PrefabManager.LoadPrefabs(worldBuilder.PrefabManager, caveBuilder.cavePrefabManager);
            worldBuilder.PrefabManager.ShufflePrefabData(worldBuilder.Seed);
            yield return null;
            worldBuilder.PathingUtils.SetupPathingGrid();
        }
        else
        {
            worldBuilder.PrefabManager.ClearDisplayed();
        }

        if (worldBuilder.Towns != 0)
        {
            yield return worldBuilder.TownPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
        }

        yield return worldBuilder.GenerateTerrainLast();

        if (worldBuilder.IsCanceled)
            yield break;

        yield return worldBuilder.POISmoother.SmoothStreetTiles();

        if (worldBuilder.IsCanceled)
            yield break;

        if (worldBuilder.Wilderness != 0)
        {
            yield return worldBuilder.WildernessPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return worldBuilder.SmoothWildernessTerrain();

            if (worldBuilder.IsCanceled)
            {
                yield break;
            }
        }
        if (hasPOIs)
        {
            worldBuilder.CalcTownshipsHeightMask();
            yield return worldBuilder.HighwayPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            yield return worldBuilder.TownPlanner.SpawnPrefabs();
            if (worldBuilder.IsCanceled)
            {
                yield break;
            }
        }

        if (worldBuilder.Wilderness != 0)
        {
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

        yield return GCUtils.UnloadAndCollectCo();
        yield return worldBuilder.SetMessage(Localization.Get("xuiRwgDrawRoads"), _logToConsole: true);
        yield return worldBuilder.DrawRoads(worldBuilder.roadDest);

        if (hasPOIs)
        {
            yield return worldBuilder.SetMessage(Localization.Get("xuiRwgSmoothRoadTerrain"), _logToConsole: true);
            worldBuilder.CalcWindernessPOIsHeightMask(worldBuilder.roadDest);
            yield return worldBuilder.SmoothRoadTerrain(worldBuilder.roadDest, worldBuilder.HeightMap, worldBuilder.WorldSize, worldBuilder.Townships);
        }

        yield return caveBuilder.GenerateCaveMap();

        foreach (Path highwayPath in worldBuilder.highwayPaths)
        {
            highwayPath.Cleanup();
        }

        foreach (Path wildernessPath in worldBuilder.wildernessPaths)
        {
            wildernessPath.Cleanup();
        }

        worldBuilder.highwayPaths.Clear();
        worldBuilder.wildernessPaths.Clear();

        yield return worldBuilder.FinalizeWater();
        yield return worldBuilder.SerializeData();
        yield return GCUtils.UnloadAndCollectCo();

        Logging.Info("RWG final in {0}:{1:00}, r={2:x}", worldBuilder.totalMS.Elapsed.Minutes, worldBuilder.totalMS.Elapsed.Seconds, Rand.Instance.PeekSample());

        yield break;
    }

    public static void SaveCaveMap()
    {
        caveBuilder.SaveCaveMap(worldBuilder);
    }

    public static void Cleanup()
    {
        caveBuilder?.Cleanup();
        caveBuilder = null;
    }
}


[HarmonyPatch(typeof(WorldBuilder), "serializeRawHeightmap")]
public static class WorldBuilder_serializeRawHeightmap
{
    public static bool Prefix()
    {
        if (CaveConfig.generateCaves)
        {
            WorldBuilder_GenerateData.SaveCaveMap();
            WorldBuilder_GenerateData.Cleanup();
        }

        return true;
    }
}
