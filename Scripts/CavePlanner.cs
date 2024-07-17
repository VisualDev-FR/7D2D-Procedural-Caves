using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

    public static int entrancesAdded = 0;

    public static int maxPlacementAttempts = 20;

    public static int cavePrefabTerrainMargin = 10;

    public static int cavePrefabBedRockMargin = 2;

    public static int radiationZoneMargin = 50;

    public static int overLapMargin = 30;

    public static Vector3i HalfWorldSize => new Vector3i(WorldBuilder.Instance.HalfWorldSize, 0, WorldBuilder.Instance.HalfWorldSize);

    public static List<PrefabData> entrancePrefabs = null;

    public static HashSet<Vector3i> caveMap = null;

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
                minHeight = Utils.FastMin(minHeight, (int)WorldBuilder.Instance.GetHeight(x, z));
            }
        }

        return minHeight;
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
        var otherSize = GetRotatedSize(other.boundingBoxSize, other.rotation);
        var otherPos = other.boundingBoxPosition;

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

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize);
            var canBePlacedUnderTerrain = minTerrainHeight >= (prefab.size.y + cavePrefabBedRockMargin + cavePrefabTerrainMargin);

            if (!canBePlacedUnderTerrain)
                continue;

            // Log.Out($"[Cave] {position} - {HalfWorldSize} = {position - HalfWorldSize}");

            position -= HalfWorldSize;

            if (OverLaps2D(position, rotatedSize, others))
                continue;

            position.y = rand.Next(cavePrefabBedRockMargin, minTerrainHeight - prefab.size.y - cavePrefabTerrainMargin);

            Log.Out($"[Cave] cave prefab {prefab.Name} added at {position}");

            return new PrefabDataInstance(others.Count + 1, position, (byte)rotation, prefab);
        }

        // Log.Warning($"[Cave] can't place prefab {prefab.Name} after {maxPlacementAttempts} attempts.");

        return null;
    }

    public static List<CavePrefab> PlaceCavePrefabs(int count)
    {
        var cavePrefabs = new List<CavePrefab>();
        var availablePrefabs = GetUndergroundPrefabs();
        var usedPrefabs = GetUsedCavePrefabs();

        for (int i = 0; i < count; i++)
        {
            var prefab = availablePrefabs[i % availablePrefabs.Count];
            var pdi = TrySpawnCavePrefab(prefab, usedPrefabs);

            if (pdi == null)
                continue;

            var cavePrefab = new CavePrefab(pdi);

            if (cavePrefab.nodes.Count == 0)
            {
                Log.Warning($"[Cave] no cave node found for {pdi.prefab.Name}");
                continue;
            }

            // Log.Out($"[Cave] {cavePrefab.nodes.Count} nodes added to {pdi.prefab.Name} ({pdi.boundingBoxPosition}) -> ({cavePrefab.nodes[0]})");

            PrefabManager.AddUsedPrefabWorld(-1, pdi);
            usedPrefabs.Add(pdi);
            cavePrefabs.Add(cavePrefab);

        }

        Log.Warning($"[Cave] {cavePrefabs.Count} placed prefabs.");

        return cavePrefabs;
    }

    public static IEnumerator GenerateCaveMap()
    {
        var timer = new Stopwatch();
        timer.Start();

        List<CavePrefab> cavePrefabs = PlaceCavePrefabs(100);

        GenerateGraph(cavePrefabs);

        // yield break;

        // HashSet<Vector3i> obstacles = CaveBuilder.CollectPrefabObstacles(cavePrefabs);
        // HashSet<Vector3i> prefabBoundNoise = new HashSet<Vector3i>(); // CaveBuilder.CollectPrefabNoise(cavePrefabs);

        List<Edge> edges = GraphSolver.Resolve(cavePrefabs);

        var wiredCaveMap = new HashSet<Vector3i>();
        int index = 0;

        foreach (var edge in edges)
        {
            Vector3i p1 = edge.node1;
            Vector3i p2 = edge.node2;

            string message = $"Cave tunneling: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count})";

            Log.Out($"Tunneling {p1} -> {p2} == {p1 + HalfWorldSize} -> {p2 + HalfWorldSize}");

            yield return WorldBuilder.Instance.SetMessage(message);

            HashSet<Vector3i> path = CaveTunneler.FindPath(p1 + HalfWorldSize, p2 + HalfWorldSize, cavePrefabs);

            wiredCaveMap.UnionWith(path);
        }

        // caveMap = CaveTunneler.ThickenCaveMap(wiredCaveMap.ToHashSet(), obstacles);

        // Console.WriteLine($"{caveMap.Count} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        yield return null;
    }

    public static void GenerateGraph(List<CavePrefab> prefabs)
    {
        int worldSize = WorldBuilder.Instance.WorldSize;
        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), worldSize * worldSize).ToArray();
        string filename = @"C:\tools\DEV\7D2D_Modding\7D2D-Procedural-caves\CaveBuilder\graph.png";

        foreach (var prefab in prefabs)
        {
            foreach (var point in prefab.Get2DEdges())
            {
                var p1 = point + HalfWorldSize;
                uint index = (uint)(p1.x + p1.z * worldSize);
                if (index >= worldSize * worldSize)
                {
                    Log.Error($"[cave] index1 out of bounds {p1}");
                    continue;
                }
                pixels[index] = new Color32(0, 255, 0, 255);
            }

            foreach (var point in prefab.nodes)
            {
                var p1 = point + HalfWorldSize;
                uint index = (uint)(p1.x + p1.z * worldSize);
                if (index >= worldSize * worldSize)
                {
                    Log.Error($"[cave] index2 out of bounds {p1}");
                    continue;
                }
                pixels[index] = new Color32(255, 0, 0, 255);
            }
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)worldSize, (uint)worldSize, (uint)worldSize * 4);
        SdFile.WriteAllBytes(filename, image);

    }

    public static void SaveCaveMap()
    {

    }

}
