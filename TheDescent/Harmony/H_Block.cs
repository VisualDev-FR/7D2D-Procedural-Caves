using System;
using HarmonyLib;


[HarmonyPatch(typeof(Block), "GetLocalizedBlockName", new Type[] { })]
public static class Block_GetLocalizedBlockName
{
    public static bool Prefix(Block __instance, ref string __result)
    {
        if (__instance.localizedBlockName != null)
        {
            __result = __instance.localizedBlockName;
            return false;
        }
        else if (__instance.AutoShapeType != 0)
        {
            __result = __instance.blockMaterial.GetLocalizedMaterialName() + " - " + __instance.GetLocalizedAutoShapeShapeName();
        }
        else if (Localization.Exists(__instance.blockName))
        {
            __result = Localization.Get(__instance.blockName);
        }
        else
        {
            __result = GetLocalizedBlockName(__instance);
        }

        __instance.localizedBlockName = __result;

        return false;
    }

    private static string GetLocalizedBlockName(Block block)
    {
        var blockName = block.Properties.GetString("BlockName");

        if (blockName != string.Empty)
        {
            return Localization.Get(blockName);
        }

        return Localization.Get(block.blockName);
    }
}