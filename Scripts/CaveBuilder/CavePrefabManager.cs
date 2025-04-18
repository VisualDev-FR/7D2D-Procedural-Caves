using System;
using System.Linq;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public class CavePrefabManager
{
    private static readonly HashSet<CavePrefab> emptyPrefabsHashset = new HashSet<CavePrefab>();

    public readonly Dictionary<Vector2s, HashSet<CavePrefab>> groupedPrefabs = new Dictionary<Vector2s, HashSet<CavePrefab>>();

    public readonly Dictionary<string, List<Vector3i>> prefabPlacements = new Dictionary<string, List<Vector3i>>();

    public readonly List<CavePrefab> Prefabs = new List<CavePrefab>();

    public WorldBuilder worldBuilder;

    public PrefabManager PrefabManager => worldBuilder.PrefabManager;

    public int worldSize;

    public int PrefabCount => Prefabs.Count;

    private readonly List<string> wildernessEntranceNames = new List<string>();

    private readonly HashSet<string> usedEntrances = new HashSet<string>();

    private readonly Dictionary<string, PrefabData> allCavePrefabs = new Dictionary<string, PrefabData>();

    private readonly Dictionary<int, CaveRoom> caveRooms = new Dictionary<int, CaveRoom>();

    public IEnumerable<CaveRoom> CaveRooms => Prefabs
        .Where(prefab => prefab.isRoom)
        .Select(prefab => caveRooms[prefab.id]);

    public CavePrefabManager(int worldSize)
    {
        this.worldSize = worldSize;
    }

    public CavePrefabManager(WorldBuilder worldBuilder)
    {
        this.worldBuilder = worldBuilder;
        worldSize = worldBuilder.WorldSize;
    }

    public void Cleanup()
    {
        wildernessEntranceNames.Clear();
        usedEntrances.Clear();
        allCavePrefabs.Clear();
        caveRooms.Clear();
    }

    public static int GetChunkHash(int x, int z)
    {
        return CaveUtils.GetChunkHash(x, z);
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        if (prefab?.PrefabName != null)
        {
            if (!prefabPlacements.ContainsKey(prefab.PrefabName))
            {
                prefabPlacements[prefab.PrefabName] = new List<Vector3i>();
            }

            prefabPlacements[prefab.PrefabName].Add(prefab.GetCenter());
        }

        var overlapingChunks = prefab.GetOverlappingChunks().ToList();

        // Logging.Info($"AddPrefab '{prefab.PrefabName}' at {prefab.position - CaveUtils.HalfWorldSize(worldSize)}, overlappingChunks: {overlapingChunks.Count()}");

        if (overlapingChunks.Count == 0)
        {
            Logging.Warning("No overlapping chunk");
        }

        var neighbor = new Vector2s(0, 0);

        foreach (var chunkPos in overlapingChunks)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    neighbor.x = (short)(chunkPos.x + dx);
                    neighbor.z = (short)(chunkPos.z + dz);

                    if (!groupedPrefabs.ContainsKey(neighbor))
                    {
                        groupedPrefabs[neighbor] = new HashSet<CavePrefab>();
                    }

                    groupedPrefabs[neighbor].Add(prefab);
                }
            }
        }
    }

    public void AddNaturalEntrance(Vector3i position)
    {
        var start = new Vector3i(position.x - 1, position.y, position.z - 1);
        var size = new Vector3i(3, 255, 3);
        var nodeRadius = 3;

        var prefab = new CavePrefab()
        {
            id = PrefabCount,
            position = start,
            Size = size,
            isNaturalEntrance = true,
            PrefabName = $"NaturalEntrance_{PrefabCount}"
        };

        prefab.AddNodes(
            new List<GraphNode>()
            {
                new GraphNode(position + Vector3i.right)   { NodeRadius = nodeRadius, direction = Direction.South },
                new GraphNode(position + Vector3i.left)    { NodeRadius = nodeRadius, direction = Direction.North },
                new GraphNode(position + Vector3i.back)    { NodeRadius = nodeRadius, direction = Direction.West },
                new GraphNode(position + Vector3i.forward) { NodeRadius = nodeRadius, direction = Direction.East },
            }
        );

        Prefabs.Add(prefab);
    }

    public bool IsNearSamePrefab(CavePrefab prefab, int minDist)
    {
        // TODO: hanlde surface prefabs which have null pdi
        if (prefab.prefabDataInstance == null)
        {
            return false;
        }

        if (!prefabPlacements.TryGetValue(prefab.PrefabName, out var positions))
        {
            return false;
        }

        var center = prefab.GetCenter();
        var sqrMinDist = minDist * minDist;

        foreach (var other in Prefabs)
        {
            if (FastMath.SqrEuclidianDist(center, other.GetCenter()) < sqrMinDist)
            {
                return true;
            }
        }

        return false;
    }

    public HashSet<CavePrefab> GetNearestPrefabsFrom(Vector3i worldPos)
    {
        var chunkPos = new Vector2s(worldPos.x >> 4, worldPos.z >> 4); // -> (x / 16, z / 16)

        if (groupedPrefabs.TryGetValue(chunkPos, out var closePrefabs))
        {
            return closePrefabs;
        }

        return emptyPrefabsHashset;
    }

    public float MinSqrDistanceToPrefab(Vector3i position)
    {
        var minSqrDist = float.MaxValue;

        foreach (CavePrefab prefab in GetNearestPrefabsFrom(position))
        {
            Vector3i start = prefab.position;
            Vector3i end = start + prefab.Size; // TODO: store end point in prefab to avoid allocating new Vectors here or elsewhere

            int sqrDistance = CaveUtils.SqrDistanceToRectangle3D(position, start, end);

            if (sqrDistance < minSqrDist)
            {
                minSqrDist = sqrDistance;
            }
        }

        return minSqrDist;
    }

    public bool IntersectWithPrefab(Vector3i position)
    {
        foreach (CavePrefab prefab in GetNearestPrefabsFrom(position))
        {
            if (prefab.Intersect3D(position))
            {
                return true;
            }
        }

        return false;
    }

    public bool IntersectMarker(Vector3i position)
    {
        foreach (CavePrefab prefab in GetNearestPrefabsFrom(position))
        {
            if (prefab.IntersectMarker(position.x, position.y, position.z))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPlacePrefab(Random rand, ref CavePrefab prefab, RawHeightMap heightMap)
    {
        int minDist = prefab.prefabDataInstance == null ? int.MaxValue : prefab.prefabDataInstance.prefab.DuplicateRepeatDistance;
        int overLapMargin = CaveConfig.overLapMargin;
        int maxTries = 10;

        while (maxTries-- > 0)
        {
            prefab.SetRandomPosition(heightMap, rand, worldSize);

            if (!prefab.OverLaps2D(Prefabs, overLapMargin) && !IsNearSamePrefab(prefab, minDist))
            {
                return true;
            }
        }

        return false;
    }

    public void AddRandomPrefabs(Random rand, RawHeightMap heightMap, int targetCount, List<PrefabData> prefabs)
    {
        Logging.Info("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var pdi = new PrefabDataInstance(PrefabCount + 1, Vector3i.zero, (byte)rand.Next(4), prefabs[i % prefabs.Count]);
            var prefab = new CavePrefab(pdi.id, pdi, Vector3i.zero);

            if (TryPlacePrefab(rand, ref prefab, heightMap))
            {
                AddPrefab(prefab);
            }
        }

        Logging.Info($"{PrefabCount} / {targetCount} prefabs added");
    }

    public void AddRandomPrefabs(Random rand, RawHeightMap heightMap, int targetCount, int minMarkers = 4, int maxMarkers = 4)
    {
        Logging.Info("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var markerCount = rand.Next(minMarkers, maxMarkers);
            var prefab = new CavePrefab(PrefabCount + 1, Vector3i.zero, rand, markerCount);

            if (TryPlacePrefab(rand, ref prefab, heightMap))
            {
                AddPrefab(prefab);
            }
        }

        Logging.Info($"{PrefabCount} / {targetCount} prefabs added");
    }

    public void SetupBoundaryPrefabs(Random rand, int tileSize)
    {
        var tileGridSize = worldSize / tileSize;
        var uBound = 1;

        for (int tileX = 1; tileX < tileGridSize - uBound + 1; tileX++)
        {
            for (int tileZ = 1; tileZ < tileGridSize - uBound + 1; tileZ++)
            {
                bool isBoundary = tileX == 1 || tileX == tileGridSize - uBound || tileZ == 1 || tileZ == tileGridSize - uBound;

                if (!isBoundary)
                    continue;

                var prefab = new CavePrefab(Prefabs.Count)
                {
                    isBoundaryPrefab = true,
                    isRoom = true,
                    position = new Vector3i(tileX * tileSize, 0, tileZ * tileSize),
                    Size = new Vector3i(
                        rand.Next(20, tileSize - 10),
                        rand.Next(20, 30),
                        rand.Next(20, tileSize - 10))
                };

                prefab.UpdateMarkers(rand);

                if (tileX == 1)
                {
                    prefab.RemoveMarker(Direction.North);
                }
                else if (tileX == tileGridSize - uBound)
                {
                    prefab.RemoveMarker(Direction.South);
                    prefab.position.x = tileSize * (tileGridSize - uBound + 1) - prefab.Size.x;
                }

                if (tileZ == 1)
                {
                    prefab.RemoveMarker(Direction.West);
                }
                else if (tileZ == tileGridSize - uBound)
                {
                    prefab.RemoveMarker(Direction.East);
                    prefab.position.z = tileSize * (tileGridSize - uBound + 1) - prefab.Size.z;
                }

                foreach (var node in prefab.nodes)
                {
                    node.position = prefab.position + node.marker.start;
                }

                AddPrefab(prefab);
            }
        }
    }

    public List<PrefabData> GetUndergroundPrefabs()
    {
        return allCavePrefabs.Values
            .Where(p => p.Tags.Test_AnySet(CaveTags.tagUnderground))
            .ToList();
    }

    public PrefabData SelectRandomWildernessEntrance(GameRandom rand)
    {
        CaveUtils.Assert(wildernessEntranceNames.Count > 0, "Seems that no cave entrance was found.");

        var unusedEntranceNames = wildernessEntranceNames.Where(prefabName => !usedEntrances.Contains(prefabName)).ToList();
        string entranceName;

        if (unusedEntranceNames.Count > 0)
        {
            entranceName = unusedEntranceNames[rand.Next(unusedEntranceNames.Count)];
        }
        else
        {
            entranceName = wildernessEntranceNames[rand.Next(wildernessEntranceNames.Count)];
        }

        // Logging.Info($"random selected entrance: '{entranceName}'");

        usedEntrances.Add(entranceName);

        return allCavePrefabs[entranceName];
    }

    public List<PrefabDataInstance> GetPrefabsAbove(Vector3i position, Vector3i size)
    {
        var prefabs = PrefabManager.UsedPrefabsWorld.Where(pdi =>
            !pdi.prefab.Tags.Test_AnySet(CaveTags.tagUnderground)
            && CaveUtils.OverLaps2D(position, size, pdi.boundingBoxPosition, pdi.boundingBoxSize)
        );

        return prefabs.ToList();
    }

    private int GetMinTerrainHeight(Vector3i position, Vector3i size, RawHeightMap heightMap)
    {
        int minHeight = 1337;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; z < position.z + size.z; z++)
            {
                minHeight = Utils.FastMin(minHeight, (int)heightMap.GetHeight(x, z));
            }
        }

        var prefabsAbove = GetPrefabsAbove(position - CaveUtils.HalfWorldSize(heightMap.worldSize), size);

        if (prefabsAbove.Count > 0)
        {
            foreach (var prefab in prefabsAbove)
            {
                minHeight = Utils.FastMin(minHeight, prefab.boundingBoxPosition.y);
            }
        }

        return minHeight;
    }

    private Vector3i GetRandomPositionFor(Vector3i size, Random rand)
    {
        var offset = CaveConfig.radiationSize + CaveConfig.radiationZoneMargin;

        return new Vector3i(
            _x: rand.Next(offset, worldSize - offset - size.x),
            _y: 0,
            _z: rand.Next(offset, worldSize - offset - size.z)
        );
    }

    private bool OverLaps2D(Vector3i position, Vector3i size, CavePrefab other, int overlapMargin)
    {
        var otherSize = CaveUtils.GetRotatedSize(other.Size, other.rotation);
        var otherPos = other.position;

        if (position.x + size.x + overlapMargin < otherPos.x || otherPos.x + otherSize.x + overlapMargin < position.x)
            return false;

        if (position.z + size.z + overlapMargin < otherPos.z || otherPos.z + otherSize.z + overlapMargin < position.z)
            return false;

        return true;
    }

    private bool OverLaps2D(Vector3i position, Vector3i size, int overlapMargin)
    {
        foreach (var prefab in Prefabs)
        {
            if (OverLaps2D(position, size, prefab, overlapMargin))
            {
                return true;
            }
        }

        return false;
    }

    private PrefabDataInstance TrySpawnCavePrefab(PrefabData prefabData, Random rand, RawHeightMap heightMap)
    {
        int maxPlacementAttempts = 20;

        while (maxPlacementAttempts-- > 0)
        {
            int rotation = rand.Next(4);

            Vector3i rotatedSize = CaveUtils.GetRotatedSize(prefabData.size, rotation);
            Vector3i position = GetRandomPositionFor(rotatedSize, rand);

            var minTerrainHeight = GetMinTerrainHeight(position, rotatedSize, heightMap);
            var canBePlacedUnderTerrain = minTerrainHeight > (CaveConfig.bedRockMargin + prefabData.size.y + CaveConfig.terrainMargin);

            if (!canBePlacedUnderTerrain || OverLaps2D(position, rotatedSize, CaveConfig.overLapMargin))
                continue;

            position.y = rand.Next(CaveConfig.bedRockMargin, minTerrainHeight - prefabData.size.y - CaveConfig.terrainMargin);

            return new PrefabDataInstance(
                PrefabManager.PrefabInstanceId++,
                position + worldBuilder.PrefabWorldOffset,
                (byte)rotation,
                prefabData
            );
        }

        return null;
    }

    public void SpawnUnderGroundPrefabs(int count, Random rand, RawHeightMap heightMap)
    {
        var undergroundPrefabs = GetUndergroundPrefabs();
        var HalfWorldSize = CaveUtils.HalfWorldSize(worldBuilder.WorldSize);

        CaveUtils.Assert(undergroundPrefabs.Count > 0, "No underground prefab was found in prefab manager.allPrefabDatas");

        for (int i = 0; i < count; i++)
        {
            var prefabData = undergroundPrefabs[i % undergroundPrefabs.Count];
            var pdi = TrySpawnCavePrefab(prefabData, rand, heightMap);

            if (pdi == null)
                continue;

            var cavePrefab = new CavePrefab(PrefabCount + 1, pdi, HalfWorldSize);

            AddPrefab(cavePrefab);
            PrefabManager.AddUsedPrefabWorld(-1, pdi);

            Logging.Info($"cave prefab '{cavePrefab.PrefabName}' added at {cavePrefab.position}");
        }

        Logging.Info($"{PrefabCount} cave prefabs added.");
    }

    /// <summary>
    /// Allocate cave room spaces
    /// </summary>
    /// <param name="count">The number of cave rooms to spawn</param>
    /// <param name="rand"></param>
    /// <param name="heightMap"></param>
    public void SpawnCaveRooms(int count, Random rand, RawHeightMap heightMap)
    {
        for (int i = 0; i < count; i++)
        {
            int maxTries = 5;

            for (int j = 0; j < maxTries; j++)
            {
                Vector3i size = new Vector3i(
                    rand.Next(20, 50),
                    rand.Next(15, 30),
                    rand.Next(30, 100)
                );

                Vector3i position = GetRandomPositionFor(size, rand);

                var minTerrainHeight = GetMinTerrainHeight(position, size, heightMap);
                var canBePlacedUnderTerrain = minTerrainHeight > (CaveConfig.bedRockMargin + size.y + CaveConfig.terrainMargin);

                if (!canBePlacedUnderTerrain)
                    continue;

                if (OverLaps2D(position, size, 30))
                    continue;

                position.y = rand.Next(CaveConfig.bedRockMargin, minTerrainHeight - size.y - CaveConfig.terrainMargin);

                var prefab = new CavePrefab(PrefabCount)
                {
                    Size = size,
                    position = position,
                    isRoom = true,
                    PrefabName = $"CaveRoom_{PrefabCount}"
                };

                prefab.UpdateMarkers(rand);

                caveRooms[prefab.id] = new CaveRoom(prefab, rand.Next());

                AddPrefab(prefab);

                Logging.Info($"Room added at '{position - CaveUtils.HalfWorldSize(worldBuilder.WorldSize)}', size: '{size}'");
                break;
            }
        }
    }

    public void TryCacheCavePrefab(PrefabData prefabData)
    {
        if (!prefabData.Tags.Test_AnySet(CaveTags.tagCave) || !CavePrefabChecker.IsValid(prefabData))
        {
            return;
        }

        string prefabName = prefabData.Name.ToLower();
        string suffix = "";

        if (prefabData.Tags.Test_AllSet(CaveTags.tagWildernessEntrance))
        {
            suffix = "(wild entrance)";
            wildernessEntranceNames.Add(prefabName);
        }
        else if (prefabData.Tags.Test_AllSet(CaveTags.tagCaveEntrance))
        {
            suffix = $"(town entrance)";
        }

        Logging.Info($"caching prefab '{prefabName}' {suffix}".TrimEnd());

        allCavePrefabs[prefabName] = prefabData;
    }

    /// <summary>
    /// Gather the prefabs that have been added by the vanilla PrefabManager
    /// </summary>
    public void AddUsedCavePrefabs()
    {
        var halfWorldSize = new Vector3i(
            worldBuilder.WorldSize >> 1,
            0,
            worldBuilder.WorldSize >> 1
        );

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CaveTags.tagCave))
            {
                AddPrefab(new CavePrefab(pdi.id, pdi, halfWorldSize));
            }
        }
    }

    /// <summary>
    /// Gather all AABB of surface prefabs to store the zones
    /// where tunnels can't be dig
    /// </summary>
    public void AddSurfacePrefabs()
    {
        var prefabClusters = new Dictionary<string, List<BoundingBox>>();
        var halfWorldSize = CaveUtils.HalfWorldSize(worldSize);

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CaveTags.tagCave))
                continue;

            if (!prefabClusters.TryGetValue(pdi.prefab.Name, out var clusters))
            {
                clusters = BlockClusterizer.Clusterize(pdi);
                prefabClusters[pdi.prefab.Name] = clusters;
            }

            foreach (var cluster in clusters)
            {
                var position = pdi.boundingBoxPosition + halfWorldSize;
                var rectangle = cluster.Transform(position, pdi.rotation, pdi.prefab.size);
                var cavePrefab = new CavePrefab(rectangle) { isCluster = true };

                AddPrefab(cavePrefab);

                Logging.Info($"add cluster ({pdi.prefab.Name}), position: {rectangle.start}, rotation: {pdi.rotation}, size: {rectangle.size}");
            }
        }
    }
}
