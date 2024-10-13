using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public static class H_PrefabManager
{
    public static IEnumerator LoadPrefabs(PrefabManager PrefabManager, CavePrefabManager cavePrefabManager)
    {
        PrefabManager.ClearDisplayed();
        if (PrefabManager.prefabManagerData.AllPrefabDatas.Count != 0)
        {
            yield break;
        }
        MicroStopwatch ms = new MicroStopwatch(_bStart: true);
        List<PathAbstractions.AbstractedLocation> prefabs = PathAbstractions.PrefabsSearchPaths.GetAvailablePathsList(null, null, null, _ignoreDuplicateNames: true);

        // PATCH: add underground prefab filter + create tag to prevent the vanilla rwg from selecting wilderness cave entrances
        FastTags<TagGroup.Poi> filter = FastTags<TagGroup.Poi>.Parse("navonly,devonly,testonly,biomeonly,underground");
        FastTags<TagGroup.Poi> wildernessCaveEntrance = FastTags<TagGroup.Poi>.Parse("cave,entrance,wilderness");

        for (int i = 0; i < prefabs.Count; i++)
        {
            PathAbstractions.AbstractedLocation location = prefabs[i];

            int prefabCount = location.Folder.LastIndexOf("/Prefabs/");
            if (prefabCount >= 0 && location.Folder.Substring(prefabCount + 8, 5).EqualsCaseInsensitive("/test"))
                continue;

            PrefabData prefabData = PrefabData.LoadPrefabData(location);

            if (prefabData.size.x != prefabData.size.z)
            {
                Log.Out($"[Cave] not square prefab: '{prefabData.Name}'");
            }

            if (prefabData == null || prefabData.Tags.IsEmpty)
                Log.Warning("Could not load prefab data for " + location.Name);

            // PATCH START //

            cavePrefabManager.TryCacheCavePrefab(prefabData);

            if (!prefabData.Tags.Test_AnySet(filter) && !prefabData.Tags.Test_AllSet(wildernessCaveEntrance))
            {
                PrefabManager.prefabManagerData.AllPrefabDatas[location.Name.ToLower()] = prefabData;
            }

            // PATCH END //

            if (ms.ElapsedMilliseconds > 500)
            {
                yield return null;
                ms.ResetAndRestart();
            }
        }

        Log.Out($"LoadPrefabs {PrefabManager.prefabManagerData.AllPrefabDatas.Count} of {prefabs.Count} in {ms.ElapsedMilliseconds * 0.001f}");
    }

}

