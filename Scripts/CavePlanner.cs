using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WorldGenerationEngineFinal;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections;
using System;

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

    private static readonly string caveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

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
        if (WorldBuilder.IsCanceled)
            yield break;

        var entrances = SpawnCaveEntrances();

        if (entrances.Count == 0)
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
            var size = pdi.boundingBoxSize;

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = prefabColor;
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

        SdFile.WriteAllBytes(filename, image);

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

    public static List<PrefabData> SpawnCaveEntrances()
    {
        var spawnedEntrances = new List<PrefabData>();
        var wildernessTiles = WildernessPlanner.GetUnusedWildernessTiles();
        var tileIndex = 0;
        var maxRolls = 500;

        if (wildernessTiles.Count == 0)
        {
            Log.Error("[Cave] no wilderness streetTile Available");
            return spawnedEntrances;
        }

        while (tileIndex < wildernessTiles.Count && --maxRolls > 0)
        {
            var tile = wildernessTiles[tileIndex];
            var prefab = SelectRandomWildernessEntrance();
            var succeed = SpawnWildernessPrefab(tile, prefab);

            if (succeed)
            {
                Log.Out($"[Cave] Entrance spawned: '{prefab.Name}'");
                spawnedEntrances.Add(prefab);
                tileIndex++;
            }
            else
            {
                Log.Warning($"[Cave] fail to spawn entrance '{prefab.Name}'");
            }
        }

        return spawnedEntrances;
    }

    public static bool SpawnWildernessPrefab(StreetTile tile, PrefabData wildernessPrefab)
    {
        GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldBuilder.Instance.Seed + 4096953);

        Vector2i worldPositionCenter = tile.WorldPositionCenter;
        Vector2i worldPosition = tile.WorldPosition;

        int maxTries = 6;

        while (maxTries-- < 0)
        {
            int rotation = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 4)) & 3;
            int sizeX = wildernessPrefab.size.x;
            int sizeZ = wildernessPrefab.size.z;

            if (rotation == 1 || rotation == 3)
            {
                sizeX = wildernessPrefab.size.z;
                sizeZ = wildernessPrefab.size.x;
            }

            Vector2i vector2i;
            if (sizeX > 150 || sizeZ > 150)
            {
                vector2i = worldPositionCenter - new Vector2i((sizeX - 150) / 2, (sizeZ - 150) / 2);
            }
            else
            {
                try
                {
                    vector2i = new Vector2i(gameRandom.RandomRange(worldPosition.x + 10, worldPosition.x + 150 - sizeX - 10), gameRandom.RandomRange(worldPosition.y + 10, worldPosition.y + 150 - sizeZ - 10));
                }
                catch
                {
                    vector2i = worldPositionCenter - new Vector2i(sizeX / 2, sizeZ / 2);
                }
            }
            int maxSize = (sizeX > sizeZ) ? sizeX : sizeZ;

            Rect rect = new Rect(vector2i.x, vector2i.y, maxSize, maxSize);

            new Rect(rect.min - new Vector2(maxSize, maxSize) / 2f, rect.size + new Vector2(maxSize, maxSize));

            Rect rect2 = new Rect(rect.min - new Vector2(maxSize, maxSize) / 2f, rect.size + new Vector2(maxSize, maxSize))
            {
                center = new Vector2(vector2i.x + sizeZ / 2, vector2i.y + sizeX / 2)
            };

            if (rect2.max.x >= WorldBuilder.Instance.WorldSize || rect2.min.x < 0f || rect2.max.y >= WorldBuilder.Instance.WorldSize || rect2.min.y < 0f)
            {
                continue;
            }

            BiomeType biome = WorldBuilder.Instance.GetBiome((int)rect.center.x, (int)rect.center.y);

            int medianHeight = Mathf.CeilToInt(WorldBuilder.Instance.GetHeight((int)rect.center.x, (int)rect.center.y));
            int positionX = vector2i.x;
            var list = new List<int>();

            while (true)
            {
                if (positionX < vector2i.x + sizeX)
                {
                    for (int positionZ = vector2i.y; positionZ < vector2i.y + sizeZ; positionZ++)
                    {
                        if (positionX >= WorldBuilder.Instance.WorldSize || positionX < 0 || positionZ >= WorldBuilder.Instance.WorldSize || positionZ < 0 || WorldBuilder.Instance.GetWater(positionX, positionZ) > 0 || biome != WorldBuilder.Instance.GetBiome(positionX, positionZ) || Mathf.Abs(Mathf.CeilToInt(WorldBuilder.Instance.GetHeight(positionX, positionZ)) - medianHeight) > 11)
                        {
                            Log.Out("[Cave] end_IL_03d4");
                            goto end_IL_03d4;
                        }
                        list.Add((int)WorldBuilder.Instance.GetHeight(positionX, positionZ));
                    }
                    positionX++;
                    continue;
                }

                medianHeight = tile.getMedianHeight(list);
                if (medianHeight + wildernessPrefab.yOffset < 2)
                {
                    break;
                }

                var vector3i = new Vector3i(tile.subHalfWorld(vector2i.x), tile.getHeightCeil(rect.center) + wildernessPrefab.yOffset + 1, tile.subHalfWorld(vector2i.y));
                var vector3i2 = new Vector3i(tile.subHalfWorld(vector2i.x), tile.getHeightCeil(rect.center), tile.subHalfWorld(vector2i.y));
                gameRandom.SetSeed(vector2i.x + vector2i.x * vector2i.y + vector2i.y);

                rotation = gameRandom.RandomRange(0, 4);
                rotation = (rotation + wildernessPrefab.RotationsToNorth) & 3;

                Vector2 vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y + sizeZ / 2);

                int prefabId = PrefabManager.PrefabInstanceId++;

                switch (rotation)
                {
                    case 0:
                        vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y);
                        break;
                    case 1:
                        vector = new Vector2(vector2i.x + sizeX, vector2i.y + sizeZ / 2);
                        break;
                    case 2:
                        vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y + sizeZ);
                        break;
                    case 3:
                        vector = new Vector2(vector2i.x, vector2i.y + sizeZ / 2);
                        break;
                }
                float maxSizeZX = 0f;
                if (wildernessPrefab.POIMarkers != null)
                {
                    List<Prefab.Marker> markers = wildernessPrefab.RotatePOIMarkers(_bLeft: true, rotation);
                    for (int i = markers.Count - 1; i >= 0; i--)
                    {
                        if (markers[i].MarkerType != Prefab.Marker.MarkerTypes.RoadExit)
                        {
                            markers.RemoveAt(i);
                        }
                    }
                    if (markers.Count > 0)
                    {
                        int index = gameRandom.RandomRange(0, markers.Count);
                        Vector3i start = markers[index].Start;
                        int sizeZX = ((markers[index].Size.x > markers[index].Size.z) ? markers[index].Size.x : markers[index].Size.z);
                        maxSizeZX = Mathf.Max(maxSizeZX, (float)sizeZX / 2f);
                        string groupName = markers[index].GroupName;
                        Vector2 vector2 = new Vector2((float)start.x + (float)markers[index].Size.x / 2f, (float)start.z + (float)markers[index].Size.z / 2f);
                        vector = new Vector2((float)vector2i.x + vector2.x, (float)vector2i.y + vector2.y);
                        Vector2 vector3 = vector;
                        bool isPrefabPath = false;
                        if (markers.Count > 1)
                        {
                            markers = wildernessPrefab.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
                            if (markers.Count > 0)
                            {
                                index = gameRandom.RandomRange(0, markers.Count);
                                vector3 = new Vector2((float)(vector2i.x + markers[index].Start.x) + (float)markers[index].Size.x / 2f, (float)(vector2i.y + markers[index].Start.z) + (float)markers[index].Size.z / 2f);
                            }
                            isPrefabPath = true;
                        }
                        WorldGenerationEngineFinal.Path path = new WorldGenerationEngineFinal.Path(_isCountryRoad: true, maxSizeZX);
                        path.FinalPathPoints.Add(new Vector2(vector.x, vector.y));
                        path.pathPoints3d.Add(new Vector3(vector.x, vector3i2.y, vector.y));
                        path.FinalPathPoints.Add(new Vector2(vector3.x, vector3.y));
                        path.pathPoints3d.Add(new Vector3(vector3.x, vector3i2.y, vector3.y));
                        path.IsPrefabPath = isPrefabPath;
                        path.StartPointID = prefabId;
                        path.EndPointID = prefabId;
                        WorldBuilder.Instance.wildernessPaths.Add(path);
                    }
                }
                tile.SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(vector2i.x, Mathf.CeilToInt(medianHeight + wildernessPrefab.yOffset + 1), vector2i.y), (byte)rotation);
                PrefabDataInstance pdi = new PrefabDataInstance(prefabId, new Vector3i(vector3i.x, medianHeight + wildernessPrefab.yOffset + 1, vector3i.z), (byte)rotation, wildernessPrefab);
                tile.AddPrefab(pdi);
                WorldBuilder.Instance.WildernessPrefabCount++;
                if (medianHeight != tile.getHeightCeil(rect.min.x, rect.min.y) || medianHeight != tile.getHeightCeil(rect.max.x, rect.min.y) || medianHeight != tile.getHeightCeil(rect.min.x, rect.max.y) || medianHeight != tile.getHeightCeil(rect.max.x, rect.max.y))
                {
                    tile.WildernessPOICenter = new Vector2i(rect.center);
                    tile.WildernessPOISize = Mathf.RoundToInt(Mathf.Max(rect.size.x, rect.size.y));
                    tile.WildernessPOIHeight = medianHeight;
                }
                if (maxSizeZX != 0f)
                {
                    WildernessPlanner.WildernessPathInfos.Add(new WorldBuilder.WildernessPathInfo(new Vector2i(vector), prefabId, maxSizeZX, WorldBuilder.Instance.GetBiome((int)vector.x, (int)vector.y)));
                }
                int num12 = Mathf.FloorToInt(rect.x / 10f) - 1;
                int num13 = Mathf.CeilToInt(rect.xMax / 10f) + 1;
                int num14 = Mathf.FloorToInt(rect.y / 10f) - 1;
                int num15 = Mathf.CeilToInt(rect.yMax / 10f) + 1;
                for (int j = num12; j < num13; j++)
                {
                    for (int k = num14; k < num15; k++)
                    {
                        if (j >= 0 && j < WorldBuilder.Instance.PathingGrid.GetLength(0) && k >= 0 && k < WorldBuilder.Instance.PathingGrid.GetLength(1))
                        {
                            if (j == num12 || j == num13 - 1 || k == num14 || k == num15 - 1)
                            {
                                PathingUtils.SetPathBlocked(j, k, 2);
                            }
                            else
                            {
                                PathingUtils.SetPathBlocked(j, k, isBlocked: true);
                            }
                        }
                    }
                }
                num12 = Mathf.FloorToInt(rect.x) - 1;
                num13 = Mathf.CeilToInt(rect.xMax) + 1;
                num14 = Mathf.FloorToInt(rect.y) - 1;
                num15 = Mathf.CeilToInt(rect.yMax) + 1;
                for (int l = num12; l < num13; l += 150)
                {
                    for (int n = num14; n < num15; n += 150)
                    {
                        StreetTile streetTileWorld = WorldBuilder.Instance.GetStreetTileWorld(l, n);
                        if (streetTileWorld != null)
                        {
                            streetTileWorld.Used = true;
                        }
                    }
                }
                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return true;

            end_IL_03d4:
                break;
            }
        }
        GameRandomManager.Instance.FreeGameRandom(gameRandom);
        return false;
    }

}
