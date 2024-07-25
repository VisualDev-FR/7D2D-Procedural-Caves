using HarmonyLib;


[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class DynamicPrefabDecorator_DecorateChunk
{
    public static void Postfix(DynamicPrefabDecorator __instance, Chunk _chunk)
    {
        // TODO: CaveDecorator.AddDecorationsToCave(_chunk);
    }
}
