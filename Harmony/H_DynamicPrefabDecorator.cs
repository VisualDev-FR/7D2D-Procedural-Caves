using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class DynamicPrefabDecorator_DecorateChunk
{
    // run cave generation after prefabs spawn, to allow caves digging into prefabs
    public static void Postfix(Chunk _chunk)
    {
        if (CaveGenerator.isEnabled && !GameUtils.IsPlaytesting())
        {
            CaveGenerator.GenerateCaveChunk(_chunk);
        }
    }
}


[HarmonyPatch(typeof(DynamicPrefabDecorator), "GetClosestPOIToWorldPos")]
public class DynamicPrefabDecorator_GetClosestPOIToWorldPos
{
    // prevents traders to offer quest to go at underground trader
    public static bool Prefix(FastTags<TagGroup.Global> questTag, Vector2 worldPos, List<Vector2> excludeList = null, int maxSearchDist = -1, bool ignoreCurrentPOI = false, BiomeFilterTypes biomeFilterType = BiomeFilterTypes.SameBiome, string biomeFilter = "")
    {
        var logger = Logging.CreateLogger("H_DynamicPrefabDecorator.GetClosestPOIToWorldPos");

        logger.Debug(questTag.ToString());

        return true;
    }
}
