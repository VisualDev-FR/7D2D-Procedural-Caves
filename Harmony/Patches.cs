using HarmonyLib;
using UnityEngine;
using System;

namespace Harmony
{
    [HarmonyPatch(typeof(HeightMapUtils), "ConvertDTMToHeightData", new Type[] { typeof(string) })]
    public class HeightMapUtils_ConvertDTMToHeightData
    {
        public static bool Prefix(string levelName)
        {
            Log.Out("[ProceduralCaves] Patching raw reader.");
            Log.Out(StackTraceUtility.ExtractStackTrace());
            return true;
        }
    }

}