using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WildernessPlanner), "Plan")]
public class WildernessPlanner_Plan
{
    public static WorldBuilder worldBuilder;

    public static WildernessPlanner WildernessPlanner;

    public static IEnumerator PlanPostfix(DynamicProperties thisWorldProperties, int worldSeed)
    {
        yield return null;
        int count = worldBuilder.GetCount("wilderness", worldBuilder.Wilderness);
        int tries = WildernessPlanner.maxWildernessSpawnTries;
        MicroStopwatch ms = new MicroStopwatch(_bStart: true);
        int wildernessPOIsLeft = count;
        if (wildernessPOIsLeft == 0)
        {
            wildernessPOIsLeft = 200;
            Log.Warning("No wilderness settings in rwgmixer for this world size, using default count of {0}", wildernessPOIsLeft);
        }
        int totalWildernessPOIs = wildernessPOIsLeft;
        WildernessPlanner.GetUnusedWildernessTiles();
        WildernessPlanner.WildernessPathInfos.Clear();
        int seed = worldSeed + 409651;
        GameRandom rnd = GameRandomManager.Instance.CreateGameRandom(seed);
        while (wildernessPOIsLeft > 0)
        {
            List<StreetTile> validWildernessTiles = WildernessPlanner.GetUnusedWildernessTiles();
            if (validWildernessTiles.Count == 0)
            {
                break;
            }
            if (tries <= 0)
            {
                wildernessPOIsLeft--;
                tries = WildernessPlanner.maxWildernessSpawnTries;
            }
            if (worldBuilder.IsMessageElapsed())
            {
                yield return worldBuilder.SetMessage($"Generating Wilderness POIs: {Mathf.FloorToInt(100f * (1f - (float)wildernessPOIsLeft / (float)totalWildernessPOIs))}%");
            }
            StreetTile streetTile = validWildernessTiles[WildernessPlanner.getLowBiasedRandom(rnd, 0, validWildernessTiles.Count)];
            if (!streetTile.Used && streetTile.SpawnPrefabs())
            {
                streetTile.Used = true;
                tries = 0;
            }
            else
            {
                tries--;
            }
        }

        // harmony patch is here
        CaveCache.Instance.caveEntrancesPlanner.SpawnCaveEntrances(rnd);

        GameRandomManager.Instance.FreeGameRandom(rnd);
        WildernessPlanner.WildernessPathInfos.Sort((WorldBuilder.WildernessPathInfo wp1, WorldBuilder.WildernessPathInfo wp2) => wp2.PathRadius.CompareTo(wp1.PathRadius));
        Log.Out($"WildernessPlanner Plan {worldBuilder.WildernessPrefabCount} prefabs spawned, in {(float)ms.ElapsedMilliseconds * 0.001f}, r={Rand.Instance.PeekSample():x}");
    }

    public static bool Prefix(WildernessPlanner __instance, DynamicProperties thisWorldProperties, int worldSeed, ref IEnumerator __result)
    {
        WildernessPlanner = __instance;
        worldBuilder = __instance.worldBuilder;
        __result = PlanPostfix(thisWorldProperties, worldSeed);
        return false;
    }
}
