using HarmonyLib;


[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class DynamicPrefabDecorator_DecorateChunk
{
    // run cave generation after prefabs spawn, to allow caves digging into rwg streetTiles
    public static void Postfix(DynamicPrefabDecorator __instance, Chunk _chunk)
    {
        if (CaveGenerator.isEnabled)
        {
            CaveGenerator.GenerateCave(_chunk);
        }
    }
}
