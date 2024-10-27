using HarmonyLib;


[HarmonyPatch(typeof(Prefab), "GetBlock")]
public static class Prefab_GetBlock
{
    // NOTE: light patch allowing to replace air by caveAir in cavePrefabs
    public static void Postfix(int _x, int _y, int _z, Prefab __instance, ref BlockValue __result)
    {
        if (
            __result.isair
            && !GameManager.Instance.IsEditMode()
            && __instance.tags.Test_AnySet(CaveConfig.tagCave)
            && _y >= -__instance.yOffset
        )
        {
            __result = CaveBlocks.caveAir;
        }
    }
}
