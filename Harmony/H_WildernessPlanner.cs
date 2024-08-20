using HarmonyLib;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WildernessPlanner), "Plan")]
public class WildernessPlanner_Plan
{
    public static void Postfix(DynamicProperties thisWorldProperties, int worldSeed)
    {
        CaveEntrancesPlanner.SpawnCaveEntrances();
    }
}
