using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class PrefabCache
{
    private static readonly HashSet<CavePrefab> emptyPrefabs = new HashSet<CavePrefab>();

    // all prefabs grouped by chunk, where key is the hashcode of the chunk
    public readonly Dictionary<int, List<CavePrefab>> groupedPrefabs;

    // a dictionary to store the nearest prefabs for each chunk, where key is chunk hashcode, and values are the nearest prefabs
    public readonly Dictionary<int, HashSet<CavePrefab>> nearestPrefabs;

    public readonly List<CavePrefab> Prefabs;

    public int Count => Prefabs.Count;

    public PrefabCache()
    {
        Prefabs = new List<CavePrefab>();
        groupedPrefabs = new Dictionary<int, List<CavePrefab>>();
        nearestPrefabs = new Dictionary<int, HashSet<CavePrefab>>();
    }

    public static int GetChunkHash(int x, int z)
    {
        return x + z * 1031;
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        foreach (var chunkHash in prefab.GetOverlappingChunkHashes())
        {
            if (!groupedPrefabs.ContainsKey(chunkHash))
            {
                groupedPrefabs[chunkHash] = new List<CavePrefab>();
            }

            groupedPrefabs[chunkHash].Add(prefab);

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

    private HashSet<CavePrefab> GetNearestPrefabsFrom(int worldX, int worldZ)
    {
        var chunkHash = GetChunkHash(worldX >> 4, worldZ >> 4); // -> worldX / 16, worldZ / 16

        if (nearestPrefabs.TryGetValue(chunkHash, out var closePrefabs))
        {
            return closePrefabs;
        }

        return emptyPrefabs;
    }

    public float MinSqrDistanceToPrefab(Vector3i position)
    {
        var minSqrDist = float.MaxValue;

        foreach (CavePrefab prefab in GetNearestPrefabsFrom(position.x, position.z))
        {
            Vector3i start = prefab.position;
            Vector3i end = start + prefab.Size;

            if (prefab.Intersect3D(position))
            {
                return 0f;
            }

            float dx = CaveUtils.FastMax(CaveUtils.FastMax(start.x - position.x, 0), position.x - end.x);
            float dy = CaveUtils.FastMax(CaveUtils.FastMax(start.y - position.y, 0), position.y - end.y);
            float dz = CaveUtils.FastMax(CaveUtils.FastMax(start.z - position.z, 0), position.z - end.z);

            float sqrDistance = dx * dx + dy * dy + dz * dz;

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
}
