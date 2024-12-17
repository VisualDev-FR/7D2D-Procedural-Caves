using System;
using HarmonyLib;


[HarmonyPatch(typeof(EAIManager), "GetType")]
public static class EAIManager_GetType
{
    public static bool Prefix(string _className, ref Type __result)
    {
        if (_className.StartsWith("EatBlock"))
        {
            __result = typeof(EAIEatBlock);
            return false;
        }

        return true;
    }
}