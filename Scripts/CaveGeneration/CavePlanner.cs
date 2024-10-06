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
using System.Threading;


public class CavePlanner
{
    private readonly CaveMap cavemap;

    private Graph caveGraph;

    private readonly Dictionary<string, PrefabData> allCavePrefabs = new Dictionary<string, PrefabData>();

    private readonly List<string> wildernessEntranceNames = new List<string>();

    private readonly HashSet<string> usedEntrances = new HashSet<string>();

    private readonly WorldBuilder worldBuilder;

    private PrefabManager PrefabManager => worldBuilder.PrefabManager;

    private int WorldSize => worldBuilder.WorldSize;

    private readonly Vector3i HalfWorldSize;

    public readonly string caveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

    public CavePlanner(WorldBuilder worldBuilder)
    {
        this.worldBuilder = worldBuilder;

        var seed = worldBuilder.Seed + worldBuilder.WorldSize;

        cavemap = new CaveMap();

        HalfWorldSize = new Vector3i(worldBuilder.HalfWorldSize, 0, worldBuilder.HalfWorldSize);

        CaveConfig.worldSize = WorldSize;
        CaveConfig.rand = new Random(seed);

        usedEntrances = new HashSet<string>();
        wildernessEntranceNames = new List<string>();

        worldBuilder.PrefabManager.Clear();
        worldBuilder.PrefabManager.ClearDisplayed();
        worldBuilder.PrefabManager.Cleanup();
    }

    public PrefabData SelectRandomWildernessEntrance()
    {
        CaveUtils.Assert(wildernessEntranceNames.Count > 0, "Seems that no cave entrance was found.");

        var unusedEntranceNames = wildernessEntranceNames.Where(prefabName => !usedEntrances.Contains(prefabName)).ToList();
        string entranceName;

        if (unusedEntranceNames.Count > 0)
        {
            entranceName = unusedEntranceNames[CaveConfig.rand.Next(unusedEntranceNames.Count)];
        }
        else
        {
            entranceName = wildernessEntranceNames[CaveConfig.rand.Next(wildernessEntranceNames.Count)];
        }

        // Log.Out($"[Cave] random selected entrance: '{entranceName}'");

        usedEntrances.Add(entranceName);

        return allCavePrefabs[entranceName];
    }

    private CavePrefabManager GetUsedCavePrefabs()
    {
        var prefabs = new CavePrefabManager(worldBuilder);

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
            {
                prefabs.AddPrefab(new CavePrefab(pdi.id, pdi, HalfWorldSize));
            }
        }

        return prefabs;
    }

    public List<PrefabData> GetUndergroundPrefabs()
    {
        var result =
            from prefab in allCavePrefabs.Values
            where prefab.Tags.Test_AnySet(CaveConfig.tagCave) && !prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance)
            select prefab;

        return result.ToList();
    }

    public void TryCacheCavePrefab(PrefabData prefabData)
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

    public List<PrefabDataInstance> GetPrefabsAbove(Vector3i position, Vector3i size)
    {
        var prefabs = PrefabManager.UsedPrefabsWorld.Where(pdi =>
            !pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveUnderground)
            && CaveUtils.OverLaps2D(position, size, pdi.boundingBoxPosition, pdi.boundingBoxSize)
        );

        return prefabs.ToList();
    }

    private int GetMinTerrainHeight(Vector3i position, Vector3i size)
    {
        int minHeight = 1337;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; z < position.z + size.z; z++)
            {
                minHeight = CaveUtils.FastMin(minHeight, (int)worldBuilder.GetHeight(x, z));
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

    private Vector3i GetRandomPositionFor(Vector3i size)
    {
        var offset = CaveConfig.radiationSize + CaveConfig.radiationZoneMargin;

        return new Vector3i(
            _x: CaveConfig.rand.Next(offset, WorldSize - offset - size.x),
            _y: 0,
            _z: CaveConfig.rand.Next(offset, WorldSize - offset - size.z)
        );
    }

    public bool OverLaps2D(Vector3i position, Vector3i size, CavePrefab other, int overlapMargin)
    {
        var otherSize = CaveUtils.GetRotatedSize(other.Size, other.rotation);
        var otherPos = other.position;

        if (position.x + size.x + overlapMargin < otherPos.x || otherPos.x + otherSize.x + overlapMargin < position.x)
            return false;

        if (position.z + size.z + overlapMargin < otherPos.z || otherPos.z + otherSize.z + overlapMargin < position.z)
            return false;

        return true;
    }

    public bool OverLaps2D(Vector3i position, Vector3i size, List<CavePrefab> others, int overlapMargin)
    {
        foreach (var prefab in others)
        {
            if (OverLaps2D(position, size, prefab, overlapMargin))
            {
                return true;
            }
        }

        return false;
    }

    private PrefabDataInstance TrySpawnCavePrefab(PrefabData prefabData, CavePrefabManager others)
    {
        int maxPlacementAttempts = 20;

        while (maxPlacementAttempts-- > 0)
        {
            int rotation = CaveConfig.rand.Next(4);

            Vector3i rotatedSize = CaveUtils.GetRotatedSize(prefabData.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize);

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize);
            var canBePlacedUnderTerrain = minTerrainHeight > (CaveConfig.bedRockMargin + prefabData.size.y + CaveConfig.terrainMargin);

            if (!canBePlacedUnderTerrain)
                continue;

            if (OverLaps2D(position, rotatedSize, others.Prefabs, CaveConfig.overLapMargin))
                continue;

            position.y = CaveConfig.rand.Next(CaveConfig.bedRockMargin, minTerrainHeight - prefabData.size.y - CaveConfig.terrainMargin);

            return new PrefabDataInstance(PrefabManager.PrefabInstanceId++, position - HalfWorldSize, (byte)rotation, prefabData);
        }

        return null;
    }

    public IEnumerator SpawnUnderGroundPrefabs(int count, CavePrefabManager cachedPrefabs)
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
        yield return null;
    }

    private void AddSurfacePrefabs(CavePrefabManager cachedPrefabs)
    {
        var rwgTileClusters = new Dictionary<string, List<BoundingBox>>();

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

    private IEnumerator SpawnCaveRooms(int count, CavePrefabManager cachedPrefabs)
    {
        for (int i = 0; i < count; i++)
        {
            int maxTries = 5;

            for (int j = 0; j < maxTries; j++)
            {
                Vector3i size = new Vector3i(
                    CaveConfig.rand.Next(20, 50),
                    CaveConfig.rand.Next(15, 30),
                    CaveConfig.rand.Next(30, 100)
                );

                Vector3i position = GetRandomPositionFor(size);

                var minTerrainHeight = GetMinTerrainHeight(position, size);
                var canBePlacedUnderTerrain = minTerrainHeight > (CaveConfig.bedRockMargin + size.y + CaveConfig.terrainMargin);

                if (!canBePlacedUnderTerrain)
                    continue;

                if (OverLaps2D(position, size, cachedPrefabs.Prefabs, 30))
                    continue;

                position.y = CaveConfig.rand.Next(CaveConfig.bedRockMargin, minTerrainHeight - size.y - CaveConfig.terrainMargin);

                var prefab = new CavePrefab(cachedPrefabs.Count + 1)
                {
                    Size = size,
                    position = position,
                };

                prefab.UpdateMarkers(CaveConfig.rand);
                var room = new CaveRoom(prefab, CaveConfig.rand.Next());

                cavemap.AddRoom(room);
                cachedPrefabs.AddPrefab(prefab);

                Log.Out($"Room added at '{position - CaveConfig.HalfWorldSize}', size: '{size}'");
                break;
            }
        }
        yield return null;
    }

    public IEnumerator GenerateCaveMap()
    {
        if (worldBuilder.IsCanceled)
            yield break;

        CavePrefabManager prefabManager = GetUsedCavePrefabs();

        foreach (var prefab in prefabManager.Prefabs)
        {
            Log.Out($"[Cave] Cave entrance '{prefab.Name}' added at {prefab.position}");
        }

        var timer = new Stopwatch();
        timer.Start();

        yield return worldBuilder.SetMessage("Spawning cave prefabs...", _logToConsole: true);

        yield return SpawnUnderGroundPrefabs(CaveConfig.TargetPrefabCount, prefabManager);
        yield return SpawnCaveRooms(1000, prefabManager);

        caveGraph = new Graph(prefabManager.Prefabs);

        foreach (var edge in caveGraph.Edges)
        {
            Log.Out($"[Cave] [{edge.node1.position}], [{edge.node2.position}]");
        }

        AddSurfacePrefabs(prefabManager);

        var threads = new List<Thread>();
        var subLists = CaveUtils.SplitList(caveGraph.Edges.ToList(), 6);
        var localMinimas = new HashSet<CaveBlock>();
        var lockObject = new object();
        int index = 0;

        foreach (var edgeList in subLists)
        {
            var thread = new Thread(() =>
            {
                foreach (var edge in edgeList)
                {
                    string message = $"Cave tunneling: {100f * index++ / caveGraph.Edges.Count:F0}% ({index} / {caveGraph.Edges.Count})";

                    if (worldBuilder.IsCanceled)
                        return;

                    var start = edge.node1;
                    var target = edge.node2;

                    var tunnel = new CaveTunnel(worldBuilder, edge, prefabManager);

                    lock (lockObject)
                    {
                        localMinimas.UnionWith(tunnel.localMinimas);
                        cavemap.AddTunnel(tunnel);
                    }
                }
            })
            {
                Priority = System.Threading.ThreadPriority.Highest
            };

            thread.Start();
            threads.Add(thread);
        }

        while (true)
        {
            bool isThreadAlive = false;
            foreach (var th in threads)
            {
                if (th.IsAlive)
                {
                    isThreadAlive = true;
                    break;
                }
            }

            if (isThreadAlive)
            {
                yield return worldBuilder.SetMessage($"Cave tunneling {100f * cavemap.TunnelsCount / caveGraph.Edges.Count:F0}%");
            }
            else
            {
                break;
            }
        }

        yield return cavemap.SetWaterCoroutine(worldBuilder, localMinimas, prefabManager);

        if (worldBuilder.IsCanceled)
            yield break;

        yield return worldBuilder.SetMessage("Saving cavemap...");

        yield return GenerateCavePreview(cavemap);

        yield return worldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Log.Out($"{cavemap.Count:N0} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        yield return null;
    }

    public IEnumerator GenerateCavePreview(CaveMap caveMap)
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
            from StreetTile st in worldBuilder.StreetTileMap
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

    public void SaveCaveMap()
    {
        cavemap.Save($"{worldBuilder.WorldPath}/cavemap");
        caveGraph.Save($"{worldBuilder.WorldPath}/cavegraph.txt");
    }

}
