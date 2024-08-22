using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WorldGenerationEngineFinal;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

using Random = System.Random;
using Path = System.IO.Path;


public static class CavePlanner
{
    private static readonly Dictionary<string, PrefabData> allCavePrefabs = new Dictionary<string, PrefabData>();

    private static List<string> wildernessEntranceNames = new List<string>();

    private static List<PrefabDataInstance> surfacePrefabs = new List<PrefabDataInstance>();

    private static HashSet<string> usedEntrances = new HashSet<string>();

    public static int AllPrefabsCount => allCavePrefabs.Count;

    public static int EntrancePrefabCount => wildernessEntranceNames.Count;

    public static int TargetEntranceCount => WorldBuilder.Instance.WorldSize / 20;

    private static WorldBuilder WorldBuilder => WorldBuilder.Instance;

    private static int WorldSize => WorldBuilder.Instance.WorldSize;

    private static int Seed => WorldBuilder.Instance.Seed + WorldSize;

    private static Vector3i HalfWorldSize => new Vector3i(WorldBuilder.HalfWorldSize, 0, WorldBuilder.HalfWorldSize);

    private static readonly int maxPlacementAttempts = 20;

    public static readonly string caveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

    public static void Init()
    {
        CaveBuilder.worldSize = WorldSize;
        CaveBuilder.rand = new Random(Seed);

        usedEntrances = new HashSet<string>();
        wildernessEntranceNames = new List<string>();
        surfacePrefabs = new List<PrefabDataInstance>();

        PrefabManager.Clear();
        PrefabManager.ClearDisplayed();
        PrefabManager.Cleanup();
    }

    private static List<PrefabDataInstance> GetSurfacePrefabs()
    {
        var result = PrefabManager.UsedPrefabsWorld.Where(pdi =>
            pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance) ||
            !pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave)
        );

        return result.ToList();
    }

    private static HashSet<string> GetAddedPrefabNames()
    {
        var result =
                from prefab in PrefabManager.UsedPrefabsWorld
                where prefab.prefab.Tags.Test_AnySet(CaveConfig.tagCave)
                select prefab.prefab.Name;

        return result.ToHashSet();
    }

    public static PrefabData SelectRandomWildernessEntrance()
    {
        CaveUtils.Assert(wildernessEntranceNames.Count > 0, "Seems that no cave entrance was found.");

        var unusedEntranceNames = wildernessEntranceNames.Where(prefabName => !usedEntrances.Contains(prefabName)).ToList();
        string entranceName;

        if (unusedEntranceNames.Count > 0)
        {
            entranceName = unusedEntranceNames[CaveBuilder.rand.Next(unusedEntranceNames.Count)];
        }
        else
        {
            entranceName = wildernessEntranceNames[CaveBuilder.rand.Next(wildernessEntranceNames.Count)];
        }

        // Log.Out($"[Cave] random selected entrance: '{entranceName}'");

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
                prefabs.AddPrefab(new CavePrefab(pdi.id, pdi, HalfWorldSize));
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

    private static bool IsWildernessEntrance(PrefabData prefabData)
    {
        return prefabData.Tags.Test_AllSet(CaveConfig.tagCaveWildernessEntrance);
    }

    private static string SkippingBecause(string prefabName, string reason)
    {
        return $"[Cave] skipping '{prefabName}' because {reason}.";
    }

    public static void TryCacheCavePrefab(PrefabData prefabData)
    {
        if (!prefabData.Tags.Test_AnySet(CaveConfig.tagCave))
        {
            return;
        }

        if (!prefabData.Tags.Test_AnySet(CaveConfig.requiredCaveTags))
        {
            Log.Warning(SkippingBecause(prefabData.Name, $"missing cave type tag: {prefabData.Tags}"));
            return;
        }

        if (!ContainsCaveMarkers(prefabData))
        {
            Log.Warning(SkippingBecause(prefabData.Name, "no cave marker was found."));
            return;
        }

        if (!PrefabMarkersAreValid(prefabData))
        {
            Log.Warning(SkippingBecause(prefabData.Name, "at least one marker is invalid."));
            return;
        }

        string prefabName = prefabData.Name.ToLower();
        string suffix = "";

        if (IsWildernessEntrance(prefabData))
        {
            suffix = "(entrance)";
            wildernessEntranceNames.Add(prefabName);
        }

        Log.Out($"[Cave] caching prefab '{prefabName}' {suffix}".TrimEnd());

        allCavePrefabs[prefabName] = prefabData;
    }

    public static List<PrefabDataInstance> GetPrefabsAbove(Vector3i position, Vector3i size)
    {
        var prefabs = surfacePrefabs.Where(pdi =>
        {
            return CaveUtils.OverLaps2D(position, size, pdi.boundingBoxPosition, pdi.boundingBoxSize);
        });

        return prefabs.ToList();
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

        var prefabsAbove = GetPrefabsAbove(position - HalfWorldSize, size);

        if (prefabsAbove.Count > 0)
        {
            foreach (var prefab in prefabsAbove)
            {
                minHeight = Utils.FastMin(minHeight, prefab.boundingBoxPosition.y);
            }
        }

        return minHeight;
    }

    private static Vector3i GetRandomPositionFor(Vector3i size)
    {
        var offset = CaveBuilder.radiationSize + CaveBuilder.radiationZoneMargin;

        return new Vector3i(
            _x: CaveBuilder.rand.Next(offset, WorldSize - offset - size.x),
            _y: 0,
            _z: CaveBuilder.rand.Next(offset, WorldSize - offset - size.z)
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
            int rotation = CaveBuilder.rand.Next(4);

            Vector3i rotatedSize = CaveUtils.GetRotatedSize(prefabData.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize);

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize);
            var canBePlacedUnderTerrain = minTerrainHeight > (CaveBuilder.bedRockMargin + prefabData.size.y + CaveBuilder.terrainMargin);

            if (!canBePlacedUnderTerrain)
                continue;

            if (OverLaps2D(position, rotatedSize, others.Prefabs))
                continue;

            position.y = CaveBuilder.rand.Next(CaveBuilder.bedRockMargin, minTerrainHeight - prefabData.size.y - CaveBuilder.terrainMargin);

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
        var _cavemap = new CaveMap();
        yield return GenerateCavePreview(_cavemap);
        yield break;

        if (WorldBuilder.IsCanceled)
            yield break;

        surfacePrefabs = GetSurfacePrefabs();

        PrefabCache addedCaveEntrances = GetUsedCavePrefabs();

        foreach (var prefab in addedCaveEntrances.Prefabs)
        {
            Log.Out($"[Cave] Cave entrance '{prefab.Name}' added at {prefab.position}");
        }

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

            if (WorldBuilder.IsCanceled)
                yield break;

            var start = edge.node1;
            var target = edge.node2;

            var tunneler = new CaveTunneler();
            var tunnel = tunneler.GenerateTunnel(edge, cachedPrefabs, cavemap);

            localMinimas.UnionWith(tunneler.localMinimas);
            cavemap.UnionWith(tunnel);
        }

        yield return cavemap.SetWaterCoroutine(localMinimas, cachedPrefabs);

        if (WorldBuilder.IsCanceled)
            yield break;

        yield return WorldBuilder.SetMessage("Saving cavemap...");

        yield return GenerateCavePreview(cavemap);

        cavemap.Save($"{caveTempDir}/cavemap");

        yield return WorldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Log.Out($"{cavemap.Count:N0} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        yield return null;
    }

    public static IEnumerator GenerateCavePreview(CaveMap caveMap)
    {
        Color32 regularPrefabColor = new Color32(255, 255, 255, 32);
        Color32 cavePrefabsColor = new Color32(0, 255, 0, 128);
        Color32 caveEntrancesColor = new Color32(255, 255, 0, 255);
        Color32 caveTunnelColor = new Color32(255, 0, 0, 64);

        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), WorldSize * WorldSize).ToArray();

        foreach (PrefabDataInstance pdi in PrefabManager.UsedPrefabsWorld)
        {
            var prefabColor = regularPrefabColor;

            if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance))
            {
                prefabColor = caveEntrancesColor;
            }
            else if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
            {
                prefabColor = cavePrefabsColor;
            }

            var position = pdi.boundingBoxPosition + HalfWorldSize;
            var size = new Vector3i(pdi.boundingBoxSize);

            if (pdi.rotation == 1 || pdi.rotation == 3)
            {
                size = new Vector3i(pdi.boundingBoxSize.z, pdi.boundingBoxSize.y, pdi.boundingBoxSize.x);
            }

            // Log.Out($"[Cave] pdi.boundingBoxPosition: {position}");

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = prefabColor;
            }
        }

        var usedTiles = (
            from StreetTile st in WorldBuilder.Instance.StreetTileMap
            where st.Used
            select st
            ).ToList();

        foreach (var st in usedTiles)
        {
            var position = new Vector3i(st.WorldPosition.x, 0, st.WorldPosition.y);
            var size = new Vector3i(150, 0, 150);

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = regularPrefabColor;
            }
        }

        foreach (CaveBlock caveblock in caveMap)
        {
            var position = caveblock;
            int index = position.x + position.z * WorldSize;
            try
            {
                caveTunnelColor.a = (byte)position.y;
                pixels[index] = caveTunnelColor;
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error($"[Cave] IndexOutOfRangeException: index={index}, position={caveblock}, worldSize={WorldSize}");
            }
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)WorldSize, (uint)WorldSize, (uint)WorldSize * 4);
        var filename = $"{caveTempDir}/cavemap.png";

        if (!Directory.Exists(caveTempDir))
            Directory.CreateDirectory(caveTempDir);

        File.WriteAllBytes(filename, image);

        yield return null;
    }

    public static void SaveCaveMap()
    {
        string source = $"{caveTempDir}/cavemap";
        string destination = $"{WorldBuilder.WorldPath}/cavemap";

        if (!Directory.Exists(source))
            return;

        if (Directory.Exists(destination))
            Directory.Delete(destination);

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
