using HarmonyLib;


[HarmonyPatch(typeof(ChunkProviderGenerateWorld), "Init")]
public static class ChunkProviderGenerateWorld_Init
{
    public static bool Prefix(World _world)
    {
        CaveGenerator.Init();
        return true;
    }
}