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


public static class CavePlanner
{
    private static CaveMap cavemap;

    private static readonly Dictionary<string, PrefabData> allCavePrefabs = new Dictionary<string, PrefabData>();

    private static List<string> wildernessEntranceNames = new List<string>();

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

        PrefabManager.Clear();
        PrefabManager.ClearDisplayed();
        PrefabManager.Cleanup();
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

    public static void TryCacheCavePrefab(PrefabData prefabData)
    {
        if (!prefabData.Tags.Test_AnySet(CaveConfig.tagCave) || !CavePrefabChecker.IsValid(prefabData))
        {
            return;
        }

        string prefabName = prefabData.Name.ToLower();
        string suffix = "";

        if (prefabData.Tags.Test_AllSet(CaveConfig.tagCaveWildernessEntrance))
        {
            suffix = "(wild entrance)";
            wildernessEntranceNames.Add(prefabName);
        }
        else if (prefabData.Tags.Test_AllSet(CaveConfig.tagCaveEntrance))
        {
            suffix = $"(town entrance)";
        }

        Log.Out($"[Cave] caching prefab '{prefabName}' {suffix}".TrimEnd());

        allCavePrefabs[prefabName] = prefabData;
    }

    public static List<PrefabDataInstance> GetPrefabsAbove(Vector3i position, Vector3i size)
    {
        var prefabs = PrefabManager.UsedPrefabsWorld.Where(pdi =>
            !pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveUnderground)
            && CaveUtils.OverLaps2D(position, size, pdi.boundingBoxPosition, pdi.boundingBoxSize)
        );

        return prefabs.ToList();
    }

    private static int GetMinTerrainHeight(Vector3i position, Vector3i size)
    {
        int minHeight = 1337;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; z < position.z + size.z; z++)
            {
                minHeight = CaveUtils.FastMin(minHeight, (int)WorldBuilder.GetHeight(x, z));
            }
        }

        var prefabsAbove = GetPrefabsAbove(position - HalfWorldSize, size);

        if (prefabsAbove.Count > 0)
        {
            foreach (var prefab in prefabsAbove)
            {
                minHeight = CaveUtils.FastMin(minHeight, prefab.boundingBoxPosition.y);
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

    public static void SpawnUnderGroundPrefabs(int count, ref PrefabCache cachedPrefabs)
    {
        var undergroundPrefabs = GetUndergroundPrefabs();

        for (int i = 0; i < count; i++)
        {
            var prefabData = undergroundPrefabs[i % undergroundPrefabs.Count];
            var pdi = TrySpawnCavePrefab(prefabData, cachedPrefabs);

            if (pdi == null)
                continue;

            var cavePrefab = new CavePrefab(cachedPrefabs.Count + 1, pdi, HalfWorldSize);

            cachedPrefabs.AddPrefab(cavePrefab);
            PrefabManager.AddUsedPrefabWorld(-1, pdi);

            Log.Out($"[Cave] cave prefab '{cavePrefab.Name}' added at {cavePrefab.position}");
        }

        Log.Out($"[Cave] {cachedPrefabs.Count} cave prefabs added.");
    }

    private static void AddSurfacePrefabs(PrefabCache cachedPrefabs)
    {
        var rwgTileClusters = new Dictionary<string, List<Rect3D>>();

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            bool isRwgTile = pdi.prefab.Tags.Test_AnySet(CaveConfig.tagRwgStreetTile);
            bool isUndergound = pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveUnderground);

            if (isRwgTile)
            {
                if (!rwgTileClusters.TryGetValue(pdi.prefab.Name, out var clusters))
                {
                    clusters = TTSReader.Clusterize(pdi.location.FullPath, pdi.prefab.yOffset);
                    rwgTileClusters[pdi.prefab.Name] = clusters;
                }

                foreach (var cluster in clusters)
                {
                    var position = pdi.boundingBoxPosition + HalfWorldSize;
                    var rectangle = cluster.Transform(position, pdi.rotation, pdi.prefab.size);
                    var cavePrefab = new CavePrefab(rectangle);
                    cachedPrefabs.AddPrefab(cavePrefab);
                }
            }
            else if (!isUndergound)
            {
                cachedPrefabs.AddPrefab(new CavePrefab(cachedPrefabs.Count + 1, pdi, HalfWorldSize));
            }
        }
    }

    public static IEnumerator GenerateCaveMap()
    {
        if (WorldBuilder.IsCanceled)
            yield break;

        PrefabCache cachedPrefabs = GetUsedCavePrefabs();

        foreach (var prefab in cachedPrefabs.Prefabs)
        {
            Log.Out($"[Cave] Cave entrance '{prefab.Name}' added at {prefab.position}");
        }

        var timer = new Stopwatch();
        timer.Start();

        yield return WorldBuilder.SetMessage("Spawning cave prefabs...", _logToConsole: true);

        SpawnUnderGroundPrefabs(CaveBuilder.PREFAB_COUNT, ref cachedPrefabs);

        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        AddSurfacePrefabs(cachedPrefabs);

        cavemap = new CaveMap();

        var localMinimas = new HashSet<CaveBlock>();
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
        cavemap.Save($"{WorldBuilder.WorldPath}/cavemap");
    }

}
