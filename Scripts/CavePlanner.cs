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

    private static HashSet<string> usedEntrances = new HashSet<string>();

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
        CaveBuilder.rand = new Random(CaveBuilder.SEED);
        entrancePrefabsNames = new List<string>();
        allCavePrefabs = new Dictionary<string, PrefabData>();
        usedEntrances = new HashSet<string>();
    }

    private static HashSet<string> GetAddedPrefabNames()
    {
        var result =
                from prefab in PrefabManager.UsedPrefabsWorld
                where prefab.prefab.Tags.Test_AnySet(CaveConfig.tagCave)
                select prefab.prefab.Name;

        return result.ToHashSet();
    }

    public static PrefabData SelectRandomEntrance()
    {
        CaveUtils.Assert(entrancePrefabsNames.Count > 0, "Seems that no cave entrance was found.");

        var unusedEntranceNames = entrancePrefabsNames.Where(prefabName => !usedEntrances.Contains(prefabName)).ToList();
        string entranceName;

        if (unusedEntranceNames.Count > 0)
        {
            entranceName = unusedEntranceNames[rand.Next(unusedEntranceNames.Count)];
        }
        else
        {
            entranceName = entrancePrefabsNames[rand.Next(entrancePrefabsNames.Count)];
        }

        Log.Out($"[Cave] random selected entrance: '{entranceName}'");

        usedEntrances.Add(entranceName);

        return allCavePrefabs[entranceName];
    }

    private static PrefabCache GetUsedCavePrefabs()
    {
        var prefabs = new PrefabCache();

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
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
            where prefab.Tags.Test_AnySet(CaveConfig.tagCave) && !prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance)
            select prefab;

        return result.ToList();
    }

    public static bool ContainsCaveMarkers(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            if (marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
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
        return prefabData.Tags.Test_AnySet(CaveConfig.tagCaveEntrance);
    }

    public static void TryCacheCavePrefab(PrefabData prefabData)
    {
        if (!ContainsCaveMarkers(prefabData))
        {
            Log.Warning($"[Cave] skipping '{prefabData.Name} 'because no cave marker was found.");
            return;
        }

        if (!PrefabMarkersAreValid(prefabData))
        {
            Log.Warning($"[Cave] skipping '{prefabData.Name}' because at least one marker is invalid.");
            return;
        }

        string prefabName = prefabData.Name.ToLower();
        string suffix = "";

        if (IsCaveEntrance(prefabData))
        {
            suffix = "(entrance)";
            entrancePrefabsNames.Add(prefabName);
        }

        Log.Out($"[Cave] caching prefab '{prefabName}' {suffix}".TrimEnd());

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

            Log.Out($"[Cave] cave prefab '{cavePrefab.Name}' added at {cavePrefab.position}");
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
            Log.Out($"[Cave] Cave entrance '{prefab.Name}' added at {prefab.position}");
        }

        CaveBuilder.worldSize = WorldSize;
        CaveBuilder.SEED = Seed;
        CaveBuilder.rand = new Random(Seed);

        var timer = new Stopwatch();
        timer.Start();

        yield return WorldBuilder.SetMessage("Spawning cave prefabs...", _logToConsole: true);

        PrefabCache cachedPrefabs = PlaceCavePrefabs(CaveBuilder.PREFAB_COUNT, addedCaveEntrances);

        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        var localMinimas = new HashSet<CaveBlock>();
        var cavemap = new CaveMap();
        int index = 0;

        foreach (var edge in edges)
        {
            string message = $"Cave tunneling: {100f * index++ / edges.Count:F0}% ({index} / {edges.Count})";

            yield return WorldBuilder.SetMessage(message);

            var start = edge.node1;
            var target = edge.node2;

            var tunneler = new CaveTunneler();
            var tunnel = tunneler.GenerateTunnel(edge, cachedPrefabs, cavemap);

            localMinimas.UnionWith(tunneler.localMinimas);
            cavemap.UnionWith(tunnel);
        }

        if (CaveConfig.generateWater)
            yield return cavemap.SetWaterCoroutine(localMinimas, cachedPrefabs);

        yield return WorldBuilder.SetMessage("Saving cavemap...");

        cavemap.Save($"{CaveTempDir}/cavemap");

        yield return WorldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Log.Out($"{cavemap.Count:N0} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

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
