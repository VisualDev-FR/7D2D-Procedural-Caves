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
        Log.Out("start WorldBuilder_saveRawHeightmap postfix");
        CavePlanner.SaveCaveMap();

        return true;
    }
}
