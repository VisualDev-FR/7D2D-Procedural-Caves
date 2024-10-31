using System;
using System.Linq;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public class CavePrefabManager
{
    private static readonly HashSet<CavePrefab> emptyPrefabsHashset = new HashSet<CavePrefab>();

    public readonly Dictionary<int, List<CavePrefab>> groupedCavePrefabs;

    public readonly Dictionary<int, HashSet<CavePrefab>> nearestPrefabs;

    public readonly Dictionary<string, List<Vector3i>> prefabPlacements;

    public readonly List<CavePrefab> Prefabs;

    public WorldBuilder worldBuilder;

    public PrefabManager PrefabManager => worldBuilder.PrefabManager;

    public int worldSize;

    public int Count => Prefabs.Count;

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

        Prefabs = new List<CavePrefab>();
        groupedCavePrefabs = new Dictionary<int, List<CavePrefab>>();
        nearestPrefabs = new Dictionary<int, HashSet<CavePrefab>>();
        prefabPlacements = new Dictionary<string, List<Vector3i>>();
    }

    public CavePrefabManager(WorldBuilder worldBuilder)
    {
        this.worldBuilder = worldBuilder;

        worldSize = worldBuilder.WorldSize;
        Prefabs = new List<CavePrefab>();
        groupedCavePrefabs = new Dictionary<int, List<CavePrefab>>();
        nearestPrefabs = new Dictionary<int, HashSet<CavePrefab>>();
        prefabPlacements = new Dictionary<string, List<Vector3i>>();
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

        foreach (var chunkHash in prefab.GetOverlappingChunkHashes())
        {
            if (!groupedCavePrefabs.ContainsKey(chunkHash))
            {
                groupedCavePrefabs[chunkHash] = new List<CavePrefab>();
            }

            groupedCavePrefabs[chunkHash].Add(prefab);

            // caching occupied neighbors chunks to avoid computing nearest prefabs in critical sections
            foreach (var offsetHash in CaveUtils.offsetsHorizontalHashes)
            {
                var neighborHashcode = chunkHash + offsetHash;

                if (!nearestPrefabs.ContainsKey(neighborHashcode))
                {
                    nearestPrefabs[neighborHashcode] = new HashSet<CavePrefab>();
                }

                nearestPrefabs[neighborHashcode].Add(prefab);
            }
        }
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
            if (CaveUtils.SqrEuclidianDist(center, other.GetCenter()) < sqrMinDist)
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<CavePrefab> GetNearestPrefabsFrom(int x, int z)
    {
        var chunkHash = GetChunkHash(x >> 4, z >> 4); // -> (x / 16, z / 16)

        if (nearestPrefabs.TryGetValue(chunkHash, out var closePrefabs))
        {
            return closePrefabs;
        }

        return emptyPrefabsHashset;
    }

    public float MinSqrDistanceToPrefab(Vector3i position)
    {
        var minSqrDist = float.MaxValue;

        foreach (CavePrefab prefab in GetNearestPrefabsFrom(position.x, position.z))
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

    public bool IntersectMarker(CaveBlock block)
    {
        foreach (CavePrefab prefab in GetNearestPrefabsFrom(block.x, block.z))
        {
            if (prefab.IntersectMarker(block.x, block.y, block.z))
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
        Log.Out("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var pdi = new PrefabDataInstance(Count + 1, Vector3i.zero, (byte)rand.Next(4), prefabs[i % prefabs.Count]);
            var prefab = new CavePrefab(pdi.id, pdi, Vector3i.zero);

            if (TryPlacePrefab(rand, ref prefab, heightMap))
            {
                AddPrefab(prefab);
            }
        }

        Log.Out($"{Count} / {targetCount} prefabs added");
    }

    public void AddRandomPrefabs(Random rand, RawHeightMap heightMap, int targetCount, int minMarkers = 4, int maxMarkers = 4)
    {
        Log.Out("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var markerCount = rand.Next(minMarkers, maxMarkers);
            var prefab = new CavePrefab(Count + 1, Vector3i.zero, rand, markerCount);

            if (TryPlacePrefab(rand, ref prefab, heightMap))
            {
                AddPrefab(prefab);
            }
        }

        Log.Out($"{Count} / {targetCount} prefabs added");
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
            .Where(p => p.Tags.Test_AnySet(CaveConfig.tagCaveUnderground))
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

        // Log.Out($"[Cave] random selected entrance: '{entranceName}'");

        usedEntrances.Add(entranceName);

        return allCavePrefabs[entranceName];
    }

    public List<PrefabDataInstance> GetPrefabsAbove(Vector3i position, Vector3i size)
    {
        var prefabs = PrefabManager.UsedPrefabsWorld.Where(pdi =>
            !pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveUnderground)
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

            var cavePrefab = new CavePrefab(Count + 1, pdi, HalfWorldSize);

            AddPrefab(cavePrefab);
            PrefabManager.AddUsedPrefabWorld(-1, pdi);

            Log.Out($"[Cave] cave prefab '{cavePrefab.PrefabName}' added at {cavePrefab.position}");
        }

        Log.Out($"[Cave] {Count} cave prefabs added.");
    }

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

                var prefab = new CavePrefab(Count)
                {
                    Size = size,
                    position = position,
                    isRoom = true,
                };

                prefab.UpdateMarkers(rand);

                caveRooms[prefab.id] = new CaveRoom(prefab, rand.Next());

                AddPrefab(prefab);

                Log.Out($"Room added at '{position - CaveUtils.HalfWorldSize(worldBuilder.WorldSize)}', size: '{size}'");
                break;
            }
        }
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

    public void AddUsedCavePrefabs()
    {
        var halfWorldSize = new Vector3i(
            worldBuilder.WorldSize >> 1,
            0,
            worldBuilder.WorldSize >> 1
        );

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
            {
                AddPrefab(new CavePrefab(pdi.id, pdi, halfWorldSize));
                Log.Out($"[Cave] used cave prefab: {pdi.prefab.Name} at [{pdi.boundingBoxPosition}]");
            }
        }
    }

    public void AddSurfacePrefabs()
    {
        var rwgTileClusters = new Dictionary<string, List<BoundingBox>>();
        var halfWorldSize = new Vector3i(
            worldBuilder.WorldSize >> 1,
            0,
            worldBuilder.WorldSize >> 1
        );

        foreach (var pdi in PrefabManager.UsedPrefabsWorld)
        {
            bool isRwgTile = pdi.prefab.Tags.Test_AnySet(CaveConfig.tagRwgStreetTile);
            bool isUndergound = pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveUnderground);
            bool isCaveEntrance = pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance);

            if (isRwgTile)
            {
                if (!rwgTileClusters.TryGetValue(pdi.prefab.Name, out var clusters))
                {
                    clusters = TTSReader.Clusterize(pdi.location.FullPath, pdi.prefab.yOffset);
                    rwgTileClusters[pdi.prefab.Name] = clusters;
                }

                foreach (var cluster in clusters)
                {
                    var position = pdi.boundingBoxPosition + halfWorldSize;
                    var rectangle = cluster.Transform(position, pdi.rotation, pdi.prefab.size);
                    var cavePrefab = new CavePrefab(rectangle);
                    AddPrefab(cavePrefab);
                }
            }
            else if (!isUndergound && !isCaveEntrance)
            {
                AddPrefab(new CavePrefab(Count, pdi, halfWorldSize));
            }
        }
    }


}
