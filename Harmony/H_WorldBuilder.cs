using System.Collections;
using HarmonyLib;
using UnityEngine;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WorldBuilder), "GenerateFromUI")]
public static class WorldBuilder_GenerateFromUI
{
    public static WorldBuilder worldBuilder;

    public static IEnumerator GenerateFromUI()
    {
        worldBuilder.IsCanceled = false;
        worldBuilder.IsFinished = false;
        worldBuilder.totalMS = new MicroStopwatch(_bStart: true);

        yield return worldBuilder.SetMessage("Starting");
        yield return new WaitForSeconds(0.1f);
        yield return worldBuilder.GenerateData();
    }

    public static IEnumerator GenerateFromUIPostFix()
    {
        CavePlanner.Init();

        yield return GenerateFromUI();
        yield return CavePlanner.GenerateCaveMap();

        yield return null;
    }

    public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
    {
        worldBuilder = __instance;
        __result = GenerateFromUIPostFix();
        return false;
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

// [HarmonyPatch(typeof(WorldBuilder), "initStreetTiles")]
// public static class WorldBuilder_initStreetTiles
// {
//     private static readonly int minTerrainHeight = 50;

//     public static bool Prefix(WorldBuilder __instance)
//     {
//         for (int i = 0; i < __instance.HeightMap.Length; i++)
//         {
//             __instance.HeightMap[i] = minTerrainHeight + (255 - minTerrainHeight) * (__instance.HeightMap[i] / 255);
//             __instance.terrainDest[i] = (float)(minTerrainHeight + (255 - minTerrainHeight) * (__instance.terrainDest[i] / 255)) / 255;
//             __instance.terrainWaterDest[i] = (float)(minTerrainHeight + (255 - minTerrainHeight) * (__instance.terrainWaterDest[i] / 255)) / 255;
//             __instance.waterDest[i] = (float)(minTerrainHeight + (255 - minTerrainHeight) * (__instance.waterDest[i] / 255)) / 255;
//         }

//         return true;
//     }
// }
