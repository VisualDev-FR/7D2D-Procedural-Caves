using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WorldGenerationEngineFinal;

using Random = System.Random;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections;
using System;

using Path = System.IO.Path;


public static class CavePlanner
{
    private static Dictionary<string, PrefabData> allCavePrefabs = new Dictionary<string, PrefabData>();

    public static int AllPrefabsCount => allCavePrefabs.Count;

    private static List<string> entrancePrefabsNames = new List<string>();

    public static int EntrancePrefabCount => entrancePrefabsNames.Count;

    private static Random rand = CaveBuilder.rand;

    private static WorldBuilder WorldBuilder => WorldBuilder.Instance;

    private static int WorldSize => WorldBuilder.Instance.WorldSize;

    private static int Seed => WorldBuilder.Instance.Seed + WorldSize;

    private const int maxPlacementAttempts = 20;

    private static Vector3i HalfWorldSize => new Vector3i(WorldBuilder.HalfWorldSize, 0, WorldBuilder.HalfWorldSize);

    private static readonly string CaveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

    public static void Init()
    {
        entrancePrefabsNames = new List<string>();
        allCavePrefabs = new Dictionary<string, PrefabData>();
        CaveBuilder.rand = new Random(CaveBuilder.SEED);
    }

    public static PrefabData SelectRandomEntrance()
    {
        CaveUtils.Assert(entrancePrefabsNames.Count > 0, "Seems that no cave entrance was found.");

        string prefabName = entrancePrefabsNames[rand.Next(entrancePrefabsNames.Count)];

        return allCavePrefabs[prefabName];
    }

    private static PrefabCache GetUsedCavePrefabs()
    {
        var prefabs = new PrefabCache();

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CavePrefab.tagCave))
            {
                prefabs.AddPrefab(new CavePrefab(prefabs.Count + 1, pdi, HalfWorldSize));
            }
        }

        return prefabs;
    }

    public static List<PrefabData> GetUndergroundPrefabs()
    {
        var result =
            from prefab in allCavePrefabs.Values
            where prefab.Tags.Test_AnySet(CavePrefab.tagCave) && !prefab.Tags.Test_AnySet(CavePrefab.tagCaveEntrance)
            select prefab;

        return result.ToList();
    }

    public static bool ContainsCaveMarkers(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            if (marker.tags.Test_AnySet(CavePrefab.tagCaveMarker))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PrefabMarkersAreValid(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            bool isOnBound_x = marker.start.x == -1 || marker.start.x == prefab.size.x;
            bool isOnBound_z = marker.start.z == -1 || marker.start.z == prefab.size.z;

            if (!isOnBound_x && !isOnBound_z)
            {
                return false;
            }

            // TODO: check 3D Intersection between prefab and markers
        }

        return true;
    }

    private static bool IsCaveEntrance(PrefabData prefabData)
    {
        return prefabData.Tags.Test_AnySet(CavePrefab.tagCaveEntrance);
    }

    public static void TryCacheCavePrefab(PrefabData prefabData)
    {
        if (!ContainsCaveMarkers(prefabData))
        {
            Log.Warning($"[Cave] skipping {prefabData.Name} because no cave marker was found.");
            return;
        }

        if (!PrefabMarkersAreValid(prefabData))
        {
            Log.Warning($"[Cave] skipping {prefabData.Name} because at least one marker is invalid.");
            return;
        }

        string prefabName = prefabData.Name.ToLower();

        if (IsCaveEntrance(prefabData))
        {
            Log.Out($"[Cave] cache prefab {prefabName} as a cave entrance.");
            entrancePrefabsNames.Add(prefabName);
        }

        Log.Out($"[Cave] caching prefab '{prefabName}'");

        allCavePrefabs[prefabName] = prefabData;
    }

    private static int GetMinTerrainHeight(Vector3i position, Vector3i size)
    {
        int minHeight = 1337;

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
        var offset = CaveBuilder.radiationSize + CaveBuilder.radiationZoneMargin;

        return new Vector3i(
            _x: rand.Next(offset, WorldSize - offset - size.x),
            _y: 0,
            _z: rand.Next(offset, WorldSize - offset - size.z)
        );
    }

    public static bool OverLaps2D(Vector3i position, Vector3i size, CavePrefab other)
    {
        var otherSize = CaveUtils.GetRotatedSize(other.Size, other.rotation);
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

    private static PrefabDataInstance TrySpawnCavePrefab(PrefabData prefabData, PrefabCache others)
    {
        int attempts = maxPlacementAttempts;

        while (attempts-- > 0)
        {
            int rotation = 0; // rand.Next(4);

            Vector3i rotatedSize = CaveUtils.GetRotatedSize(prefabData.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize);

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize);
            var canBePlacedUnderTerrain = minTerrainHeight > (prefabData.size.y + CaveBuilder.bedRockMargin + CaveBuilder.terrainMargin);

            if (!canBePlacedUnderTerrain)
                continue;

            if (OverLaps2D(position, rotatedSize, others.Prefabs))
                continue;

            position.y = rand.Next(CaveBuilder.bedRockMargin, minTerrainHeight - prefabData.size.y - CaveBuilder.terrainMargin);

            return new PrefabDataInstance(-1, position - HalfWorldSize, (byte)rotation, prefabData);
        }

        return null;
    }

    public static PrefabCache PlaceCavePrefabs(int count, PrefabCache prefabCache)
    {
        var availablePrefabs = GetUndergroundPrefabs();
        var cavePrefabs = prefabCache;

        for (int i = 0; i < count; i++)
        {
            var prefabData = availablePrefabs[i % availablePrefabs.Count];
            var pdi = TrySpawnCavePrefab(prefabData, cavePrefabs);

            if (pdi == null)
                continue;

            var cavePrefab = new CavePrefab(cavePrefabs.Count + 1, pdi, HalfWorldSize);

            cavePrefabs.AddPrefab(cavePrefab);
            PrefabManager.AddUsedPrefabWorld(-1, pdi);

            Log.Out($"[Cave] cave prefab {cavePrefab.Name} added at {cavePrefab.position}");
        }

        Log.Out($"[Cave] {cavePrefabs.Count} cave prefabs added.");

        return cavePrefabs;
    }

    public static IEnumerator GenerateCaveMap()
    {
        Log.Out($"[Cave] worldsize = {WorldSize}");
        Log.Out($"[Cave] Seed = {Seed}");

        PrefabCache addedCaveEntrances = GetUsedCavePrefabs();

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

        PrefabCache cachedPrefabs = PlaceCavePrefabs(CaveBuilder.PREFAB_COUNT, addedCaveEntrances);

        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        int caveBlockCount = 0;
        int index = 0;

        using (var multistream = new MultiStream($"{CaveTempDir}/cavemap", create: true))
        {
            foreach (var edge in edges)
            {
                GraphNode p1 = edge.node1;
                GraphNode p2 = edge.node2;

                string message = $"Cave tunneling: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count})";

                yield return WorldBuilder.SetMessage(message);

                var tunnel = CaveTunneler.GenerateTunnel(p1, p2, cachedPrefabs);

                foreach (CaveBlock caveBlock in tunnel)
                {
                    var position = caveBlock.position;

                    int region_x = position.x / CaveBuilder.RegionSize;
                    int region_z = position.z / CaveBuilder.RegionSize;
                    int regionID = region_x + region_z * CaveBuilder.regionGridSize;

                    var writer = multistream.GetWriter($"region_{regionID}.bin");
                    caveBlock.ToBinaryStream(writer);
                    caveBlockCount++;
                }
            }
        }

        yield return WorldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Log.Out($"{caveBlockCount} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        yield return null;
    }

    public static IEnumerator GenerateCavePreview(List<CavePrefab> prefabs, HashSet<CaveBlock> caveMap)
    {
        string filename = @"C:\tools\DEV\7D2D_Modding\7D2D-Procedural-caves\CaveBuilder\graph.png";

        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), WorldSize * WorldSize).ToArray();

        foreach (var prefab in prefabs)
        {
            foreach (var point in prefab.Get2DEdges())
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = new Color32(0, 255, 0, 255);
            }
        }

        foreach (var blockPos in caveMap)
        {
            var position = blockPos;
            int index = position.x + position.z * WorldSize;
            try
            {
                pixels[index] = new Color32(255, 0, 0, 255);
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error($"[Cave] IndexOutOfRangeException: index={index}, position={blockPos}, worldSize={WorldSize}");
            }
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)WorldSize, (uint)WorldSize, (uint)WorldSize * 4);
        SdFile.WriteAllBytes(filename, image);

        yield return null;
    }

    public static void SaveCaveMap()
    {
        string source = $"{CaveTempDir}/cavemap";
        string destination = $"{WorldBuilder.WorldPath}/cavemap";

        if (!Directory.Exists(source))
            return;

        if (!Directory.Exists(destination))
            Directory.CreateDirectory(destination);

        try
        {
            foreach (string filePath in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(destination, fileName);

                File.Move(filePath, destPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"An error occured: {ex.Message}");
        }
    }
}
