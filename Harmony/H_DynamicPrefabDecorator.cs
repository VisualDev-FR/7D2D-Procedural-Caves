using HarmonyLib;


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
