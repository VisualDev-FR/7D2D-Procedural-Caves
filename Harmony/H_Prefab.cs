using HarmonyLib;


[HarmonyPatch(typeof(Prefab), "GetBlock")]
public static class Prefab_GetBlock
{
    public static void Postfix(int _x, int _y, int _z, Prefab __instance, ref BlockValue __result)
    {
        if (__result.isair && !GameManager.Instance.IsEditMode() && __instance.tags.Test_AnySet(CaveConfig.tagCave))
        {
            __result = CaveGenerator.caveAir;
        }
    }
}
