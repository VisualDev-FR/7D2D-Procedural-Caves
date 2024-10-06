using System;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public class CavePrefabManager
{
    private static readonly HashSet<CavePrefab> emptyPrefabsHashset = new HashSet<CavePrefab>();

    // all prefabs grouped by chunk, where key is the hashcode of the chunk
    public readonly Dictionary<int, List<CavePrefab>> groupedCavePrefabs;

    // a dictionary to store the nearest prefabs for each chunk, where key is chunk hashcode, and values are the nearest prefabs
    public readonly Dictionary<int, HashSet<CavePrefab>> nearestPrefabs;

    public readonly Dictionary<string, List<Vector3i>> prefabPlacements;

    public readonly List<CavePrefab> Prefabs;

    public WorldBuilder worldBuilder;

    public int WorldSize => worldBuilder.WorldSize;

    public int Count => Prefabs.Count;

    public CavePrefabManager(WorldBuilder worldBuilder)
    {
        this.worldBuilder = worldBuilder;
        Prefabs = new List<CavePrefab>();
        groupedCavePrefabs = new Dictionary<int, List<CavePrefab>>();
        nearestPrefabs = new Dictionary<int, HashSet<CavePrefab>>();
        prefabPlacements = new Dictionary<string, List<Vector3i>>();
    }

    public static int GetChunkHash(int x, int z)
    {
        return CaveUtils.GetChunkHash(x, z);
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        if (prefab?.Name != null)
        {
            if (!prefabPlacements.ContainsKey(prefab.Name))
            {
                prefabPlacements[prefab.Name] = new List<Vector3i>();
            }

            prefabPlacements[prefab.Name].Add(prefab.GetCenter());
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

        if (!prefabPlacements.TryGetValue(prefab.Name, out var positions))
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

    private bool TryPlacePrefab(Random rand, ref CavePrefab prefab)
    {
        int minDist = prefab.prefabDataInstance == null ? int.MaxValue : prefab.prefabDataInstance.prefab.DuplicateRepeatDistance;
        int overLapMargin = CaveConfig.overLapMargin;
        int maxTries = 10;

        while (maxTries-- > 0)
        {
            prefab.SetRandomPosition(worldBuilder, rand, WorldSize);

            if (!prefab.OverLaps2D(Prefabs, overLapMargin) && !IsNearSamePrefab(prefab, minDist))
            {
                return true;
            }
        }

        return false;
    }

    public void GetRandomPrefabs(Random rand, int targetCount, List<PrefabData> prefabs)
    {
        Log.Out("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var pdi = new PrefabDataInstance(Count + 1, Vector3i.zero, (byte)rand.Next(4), prefabs[i % prefabs.Count]);
            var prefab = new CavePrefab(pdi.id, pdi, Vector3i.zero);

            if (TryPlacePrefab(rand, ref prefab))
            {
                AddPrefab(prefab);
            }
        }

        Log.Out($"{Count} / {targetCount} prefabs added");
    }

    public void GetRandomPrefabs(Random rand, int targetCount, int minMarkers = 4, int maxMarkers = 4)
    {
        Log.Out("Start POIs placement...");

        for (int i = 0; i < targetCount; i++)
        {
            var markerCount = rand.Next(minMarkers, maxMarkers);
            var prefab = new CavePrefab(Count + 1, Vector3i.zero, rand, markerCount);

            if (TryPlacePrefab(rand, ref prefab))
            {
                AddPrefab(prefab);
            }
        }

        Log.Out($"{Count} / {targetCount} prefabs added");
    }

    public void SetupBoundaryPrefabs(Random rand, int tileSize)
    {
        var tileGridSize = WorldSize / tileSize;
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

}
