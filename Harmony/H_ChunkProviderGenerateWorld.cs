using System.Collections;
using HarmonyLib;


[HarmonyPatch(typeof(ChunkProviderGenerateWorld), "Init")]
public static class ChunkProviderGenerateWorld_Init
{
    public static bool Prefix(ChunkProviderGenerateWorld __instance, World _world, ref IEnumerator __result)
    {
        __result = Init(__instance, _world);
        return false;
    }

    private static IEnumerator Init(ChunkProviderGenerateWorld __instance, World _world)
    {
        var worldLocation = PathAbstractions.WorldsSearchPaths.GetLocation(__instance.levelName);

        __instance.prefabDecorator = new DynamicPrefabDecorator();
        __instance.spawnPointManager = new SpawnPointManager();
        __instance.world = _world;

        yield return __instance.prefabDecorator.Load(worldLocation.FullPath);

        // PATCH START
        CaveGenerator.Init();
        // PATCH END

        if (!__instance.bClientMode)
        {
            __instance.spawnPointManager.Load(worldLocation.FullPath);
            __instance.threadInfo = ThreadManager.StartThread("GenerateChunks", null, __instance.GenerateChunksThread, null, System.Threading.ThreadPriority.Lowest, null, null, _useRealThread: true);
        }

        yield return null;
    }
}