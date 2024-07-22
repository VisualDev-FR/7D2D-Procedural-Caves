using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WorldGenerationEngineFinal;

using Random = System.Random;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections;


public static class CavePlanner
{
    public static Dictionary<string, PrefabData> AllCavePrefabs = new Dictionary<string, PrefabData>();

    public static FastTags<TagGroup.Poi> CaveEntranceTags = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> caveTags = FastTags<TagGroup.Poi>.Parse("cave");

    public static Random rand = CaveBuilder.rand;

    public static WorldBuilder WorldBuilder => WorldBuilder.Instance;

    public static int WorldSize => WorldBuilder.WorldSize;

    public static int Seed => WorldBuilder.Seed + WorldSize;

    public static int entrancesAdded = 0;

    public static int maxPlacementAttempts = 20;

    public static Vector3i HalfWorldSize => new Vector3i(WorldBuilder.HalfWorldSize, 0, WorldBuilder.HalfWorldSize);

    public static List<PrefabData> entrancePrefabs = null;

    public static HashSet<Vector3i> caveMap = null;

    public static PrefabCache GetUsedCavePrefabs()
    {
        var prefabs = new PrefabCache();

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(caveTags))
            {
                prefabs.AddPrefab(new CavePrefab(prefabs.Count + 1, pdi, HalfWorldSize));
            }
        }

        return prefabs;
    }

    public static List<PrefabData> GetUndergroundPrefabs()
    {
        var result =
            from prefab in AllCavePrefabs.Values
            where prefab.Tags.Test_AnySet(caveTags) && !prefab.Tags.Test_AnySet(CaveEntranceTags)
            select prefab;

        return result.ToList();
    }

    public static bool ContainsCaveNodes(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            if (marker.tags.Test_AnySet(CavePrefab.caveNodeTags))
            {
                return true;
            }
        }

        return false;
    }

    public static List<PrefabData> GetCaveEntrancePrefabs()
    {
        if (entrancePrefabs != null)
            return entrancePrefabs;

        var prefabDatas = new List<PrefabData>();

        foreach (PrefabData prefabData in AllCavePrefabs.Values)
        {
            if (!prefabData.Tags.Test_AnySet(CaveEntranceTags))
                continue;

            if (!ContainsCaveNodes(prefabData))
            {
                Log.Warning($"[Cave] skipping {prefabData.Name} because no cave node is specified.");
                continue;
            }

            prefabDatas.Add(prefabData);
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
        caveMap = new HashSet<Vector3i>();
        CaveBuilder.rand = new Random(CaveBuilder.SEED);
    }

    private static int GetMinTerrainHeight(Vector3i position, Vector3i size)
    {
        int minHeight = 257;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; z < position.z + size.z; z++)
            {
                minHeight = Utils.FastMin(minHeight, (int)WorldBuilder.GetHeight(x, z));
            }
        }

        return minHeight;
    }

    private static Vector3i GetRandomPositionFor(Vector3i size)
    {
        return new Vector3i(
            _x: rand.Next(RadiationSize, WorldSize - RadiationSize - size.x),
            _y: 0,
            _z: rand.Next(RadiationSize, WorldSize - RadiationSize - size.z)
        );
    }

    public static bool OverLaps2D(Vector3i position, Vector3i size, CavePrefab other)
    {
        var otherSize = GetRotatedSize(other.size, other.rotation);
        var otherPos = other.position;

        int overlapMargin = CaveBuilder.overLapMargin;

        if (position.x + size.x + overlapMargin < otherPos.x || otherPos.x + otherSize.x + overlapMargin < position.x)
            return false;

        if (position.z + size.z + overlapMargin < otherPos.z || otherPos.z + otherSize.z + overlapMargin < position.z)
            return false;

        return true;
    }

    public static bool OverLaps2D(Vector3i position, Vector3i size, List<CavePrefab> others)
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

    private static PrefabDataInstance TrySpawnCavePrefab(PrefabData prefabData, PrefabCache others)
    {
        int attempts = maxPlacementAttempts;

        while (attempts-- > 0)
        {
            int rotation = 0; // rand.Next(4);

            Vector3i rotatedSize = GetRotatedSize(prefabData.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize);

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize);
            var canBePlacedUnderTerrain = minTerrainHeight >= (prefabData.size.y + CaveBuilder.bedRockMargin + CaveBuilder.terrainMargin);

            if (!canBePlacedUnderTerrain)
                continue;

            if (OverLaps2D(position, rotatedSize, others.Prefabs))
                continue;

            position.y = rand.Next(CaveBuilder.bedRockMargin, minTerrainHeight - prefabData.size.y - CaveBuilder.terrainMargin);

            return new PrefabDataInstance(-1, position - HalfWorldSize, (byte)rotation, prefabData);
        }

        return null;
    }

    public static PrefabCache PlaceCavePrefabs(int count, PrefabCache caveEntrances)
    {
        var availablePrefabs = GetUndergroundPrefabs();
        var cavePrefabs = caveEntrances;

        for (int i = 0; i < count; i++)
        {
            var prefabData = availablePrefabs[i % availablePrefabs.Count];
            var pdi = TrySpawnCavePrefab(prefabData, cavePrefabs);

            if (pdi == null)
                continue;

            var cavePrefab = new CavePrefab(cavePrefabs.Count + 1, pdi, HalfWorldSize);

            PrefabManager.AddUsedPrefabWorld(-1, pdi);
            cavePrefabs.AddPrefab(cavePrefab);

            Log.Out($"[Cave] cave prefab {cavePrefab.Name} added at {cavePrefab.position}");
        }

        return cavePrefabs;
    }

    public static IEnumerator GenerateCaveMap()
    {
        Log.Out($"[Cave] worldsize = {WorldSize}");
        Log.Out($"[Cave] Seed = {Seed}");

        PrefabCache addedCaveEntrances = GetUsedCavePrefabs();

        Log.Out($"[Cave] {addedCaveEntrances.Count} Cave entrance added.");

        foreach (var prefab in addedCaveEntrances.Prefabs)
        {
            Log.Out($"[Cave] Cave entrance added at {prefab.position}: {prefab.Name}");
        }

        CaveBuilder.worldSize = WorldSize;
        CaveBuilder.SEED = Seed;
        CaveBuilder.rand = new Random(Seed);

        var timer = new Stopwatch();
        timer.Start();

        yield return WorldBuilder.SetMessage("Spawning cave prefabs...", _logToConsole: true);

        PrefabCache cavePrefabs = PlaceCavePrefabs(CaveBuilder.PREFAB_COUNT, addedCaveEntrances);

        yield return GenerateCavePreview(cavePrefabs, new HashSet<Vector3i>());

        Log.Out($"[Cave] {cavePrefabs.Count} cave prefabs added.");

        List<Edge> edges = Graph.Resolve(cavePrefabs.Prefabs);

        var wiredCaveMap = new HashSet<Vector3i>();
        int index = 0;

        foreach (var edge in edges)
        {
            Vector3i p1 = edge.node1.position;
            Vector3i p2 = edge.node2.position;

            string message = $"Cave tunneling: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count})";

            Log.Out($"Tunneling {p1} -> {p2}");

            yield return WorldBuilder.SetMessage(message);

            HashSet<Vector3i> path = CaveTunneler.FindPath(p1, p2, cavePrefabs);

            wiredCaveMap.UnionWith(path);
        }


        caveMap = CaveTunneler.ThickenCaveMap(wiredCaveMap);
        // caveMap = wiredCaveMap;

        yield return GenerateCavePreview(cavePrefabs, wiredCaveMap);

        Log.Out($"{caveMap.Count} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        yield return null;
    }

    public static IEnumerator GenerateCavePreview(PrefabCache prefabs, HashSet<Vector3i> caveMap)
    {
        string filename = @"C:\tools\DEV\7D2D_Modding\7D2D-Procedural-caves\CaveBuilder\graph.png";

        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), WorldSize * WorldSize).ToArray();

        yield return WorldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        foreach (var prefab in prefabs.Prefabs)
        {
            Log.Out($"{prefab.position} / {prefab.prefabDataInstance.boundingBoxPosition} / {prefab.size}");

            foreach (var point in prefab.Get2DEdges())
            {
                uint index = (uint)(point.x + point.z * WorldSize);
                pixels[index] = new Color32(0, 255, 0, 255);
            }

            foreach (var point in prefab.nodes)
            {
                uint index = (uint)(point.x + point.z * WorldSize);
                pixels[index] = new Color32(255, 0, 0, 255);
            }
        }

        foreach (var block in caveMap)
        {
            var p1 = block; // - HalfWorldSize;
            uint index = (uint)(p1.x + p1.z * WorldSize);
            pixels[index] = new Color32(255, 0, 0, 255);
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)WorldSize, (uint)WorldSize, (uint)WorldSize * 4);
        SdFile.WriteAllBytes(filename, image);

        yield return null;
    }

    public static void SaveCaveMap()
    {
        string filename = WorldBuilder.WorldPath + "cavemap.csv";

        Log.Out(filename);
        Log.Out($"caveMap size = {caveMap.Count}");

        CaveBuilder.SaveCaveMap(filename, caveMap);
    }
}
