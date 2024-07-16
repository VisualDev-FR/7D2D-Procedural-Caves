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

    public static int maxPlacementAttempts = 20;

    public static int cavePrefabTerrainMargin = 10;

    public static int cavePrefabBedRockMargin = 2;

    public static int radiationZoneMargin = 50;

    public static int overLapMargin = 30;

    public static Vector3i HalfWorldSize => new Vector3i(WorldBuilder.Instance.HalfWorldSize, 0, WorldBuilder.Instance.HalfWorldSize);

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

    private static bool CanBePlacedUnderTerrain(Vector3i position, Vector3i size)
    {
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; z < position.z + size.z; z++)
            {
                int totalHeight = size.z + cavePrefabTerrainMargin + cavePrefabBedRockMargin;

                if (totalHeight >= WorldBuilder.Instance.GetHeight(x, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static Vector3i GetRandomPositionFor(Vector3i size)
    {
        int mapSize = WorldBuilder.Instance.WorldSize;
        int radiationSize = StreetTile.TileSize + radiationZoneMargin;

        return new Vector3i(
            _x: rand.Next(radiationSize, mapSize - radiationSize - size.x),
            _y: 0,
            _z: rand.Next(radiationSize, mapSize - radiationSize - size.z)
        );
    }

    public static bool OverLaps2D(Vector3i position, Vector3i size, PrefabDataInstance other)
    {
        var otherPos = other.boundingBoxPosition;
        var otherSize = other.boundingBoxSize;

        if (position.x + size.x + overLapMargin < otherPos.x || otherPos.x + otherSize.x + overLapMargin < position.x)
            return false;

        if (position.z + size.z + overLapMargin < otherPos.z || otherPos.z + otherSize.z + overLapMargin < position.z)
            return false;

        return true;
    }

    public static bool OverLaps2D(Vector3i position, Vector3i size, List<PrefabDataInstance> others)
    {
        foreach (var other in others)
        {
            if (OverLaps2D(position, size, other))
            {
                return true;
            }
        }

        return false;
    }

    public static Vector3i GetRotatedSize(Vector3i Size, int rotation)
    {
        if (rotation == 0 || rotation == 2)
            return new Vector3i(Size);

        return new Vector3i(Size.z, Size.y, Size.x);
    }

    private static PrefabDataInstance TrySpawnCavePrefab(PrefabData prefab, List<PrefabDataInstance> others)
    {
        int attempts = maxPlacementAttempts;

        while (attempts-- > 0)
        {
            int rotation = rand.Next(4);

            Vector3i rotatedSize = GetRotatedSize(prefab.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize);

            if (!CanBePlacedUnderTerrain(position, rotatedSize))
                continue;

            if (OverLaps2D(position, rotatedSize, others))
                continue;

            position -= HalfWorldSize;

            return new PrefabDataInstance(others.Count + 1, position, (byte)rotation, prefab);
        }

        Log.Warning($"[Cave] can't place prefab {prefab.Name} after {maxPlacementAttempts} attempts.");

        return null;
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
                usedPrefabs.Add(prefabDataInstance);
            }
        }

        Log.Warning($"[Cave] {placedPrefabs.Count} placed prefabs.");

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
