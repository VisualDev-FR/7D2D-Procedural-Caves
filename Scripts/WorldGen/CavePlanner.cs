using System;
using System.Collections.Generic;
using System.Linq;
using WorldGenerationEngineFinal;


public static class CavePlanner
{
    public static Dictionary<string, PrefabData> AllCavePrefabs = new Dictionary<string, PrefabData>();

    public static FastTags<TagGroup.Poi> CaveEntranceTags = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> caveTags = FastTags<TagGroup.Poi>.Parse("cave");

    public static Random rand = CaveBuilder.rand;

    public static int entrancesAdded = 0;

    public static List<PrefabData> entrancePrefabs = null;

    public static List<PrefabDataInstance> GetUsedCavePrefab()
    {
        var result =
            from PrefabDataInstance pdi in PrefabManager.UsedPrefabsWorld
            where pdi.prefab.Tags.Test_AnySet(caveTags)
            select pdi;

        return result.ToList();
    }

    public static List<PrefabData> GetCaveEntrancePrefabs()
    {
        if (entrancePrefabs != null)
            return entrancePrefabs;

        var prefabDatas = new List<PrefabData>();

        foreach (var prefabData in AllCavePrefabs.Values)
        {
            if (prefabData.Tags.Test_AnySet(CaveEntranceTags))
            {
                prefabDatas.Add(prefabData);
            }
        }

        if (prefabDatas.Count == 0)
            Log.Error($"No cave entrance found in installed prefabs.");

        entrancePrefabs = prefabDatas;

        return prefabDatas;
    }

    public static List<PrefabDataInstance> GetAddedCaveEntrance()
    {
        var prefabs = new List<PrefabDataInstance>();

        foreach (var prefab in PrefabManager.UsedPrefabsWorld)
        {
            if (prefab.prefab.Tags.Test_AnySet(caveTags))
                prefabs.Add(prefab);
        }

        return prefabs;
    }

    public static void Cleanup()
    {
        entrancesAdded = 0;
        entrancePrefabs = null;
        CaveBuilder.rand = new Random(CaveBuilder.SEED);
    }

    public static void PlaceCavePOIs()
    {
    }

    public static void GenerateCaveMap()
    {

    }

    public static void SaveCaveMap()
    {

    }

}
