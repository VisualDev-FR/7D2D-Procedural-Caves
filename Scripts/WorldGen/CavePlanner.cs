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

    public static List<PrefabDataInstance> GetUsedCavePrefabs()
    {
        var result =
            from PrefabDataInstance pdi in PrefabManager.UsedPrefabsWorld
            where pdi.prefab.Tags.Test_AnySet(caveTags)
            select pdi;

        return result.ToList();
    }

    public static List<PrefabData> GetUndergroundPrefabs()
    {
        var result =
            from prefab in AllCavePrefabs.Values
            where prefab.Tags.Test_AnySet(caveTags) && !prefab.Tags.Test_AnySet(CaveEntranceTags)
            select prefab;

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

    public static void Cleanup()
    {
        entrancesAdded = 0;
        entrancePrefabs = null;
        CaveBuilder.rand = new Random(CaveBuilder.SEED);
    }

    private static PrefabDataInstance TrySpawnCavePrefab(PrefabData prefab, List<PrefabDataInstance> others)
    {
        throw new NotImplementedException();
    }

    public static List<PrefabDataInstance> PlaceCavePOIs(int count)
    {
        var placedPrefabs = new List<PrefabDataInstance>();
        var availablePrefabs = GetUndergroundPrefabs();
        var usedPrefabs = GetUsedCavePrefabs();

        for (int i = 0; i < count; i++)
        {
            var prefab = availablePrefabs[i % availablePrefabs.Count];
            var prefabDataInstance = TrySpawnCavePrefab(prefab, usedPrefabs);

            if (prefabDataInstance != null)
            {
                PrefabManager.AddUsedPrefabWorld(-1, prefabDataInstance);
                placedPrefabs.Add(prefabDataInstance);
            }
        }

        return placedPrefabs;
    }

    public static void GenerateCaveMap()
    {
        List<PrefabDataInstance> cavePrefabs = PlaceCavePOIs(100);
    }

    public static void SaveCaveMap()
    {

    }

}
