using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class CaveProjectDynamicPrefabDecorator
{
    public static void Postfix(DynamicPrefabDecorator __instance, Chunk _chunk)
    {
        // TODO: CaveDecorator.AddDecorationsToCave(_chunk);
    }
}
