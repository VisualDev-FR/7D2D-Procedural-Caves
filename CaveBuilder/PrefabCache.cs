using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class PrefabCache
{
    // all prefabs grouped by chunk, where key is the hashcode of the chunk
    public readonly Dictionary<int, List<CavePrefab>> groupedPrefabs;

    public readonly List<CavePrefab> Prefabs;

    public int Count => Prefabs.Count;

    public PrefabCache()
    {
        Prefabs = new List<CavePrefab>();
        groupedPrefabs = new Dictionary<int, List<CavePrefab>>();
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
        }
    }

    public static IEnumerable<int> GetChunkHashesAround(int chunkX, int chunkZ)
    {
        var hashcode = GetChunkHash(chunkX, chunkZ);

        yield return hashcode;

        foreach (int offsetHash in CaveUtils.offsetsHorizontalHashes)
        {
            yield return hashcode + offsetHash;
        }
    }

    public IEnumerable<CavePrefab> IteratePrefabsNearTo(int worldX, int worldZ)
    {
        int chunkX = worldX >> 4; // worldX / 16;
        int chunkZ = worldZ >> 4; // worldZ / 16;

        foreach (int chunkHash in GetChunkHashesAround(chunkX, chunkZ))
        {
            if (!groupedPrefabs.TryGetValue(chunkHash, out var chunkPrefabs))
                continue;

            foreach (var prefab in chunkPrefabs)
            {
                yield return prefab;
            }
        }
    }

    public float MinSqrDistanceToPrefab(Vector3i position)
    {
        var minSqrDist = float.MaxValue;

        foreach (CavePrefab prefab in IteratePrefabsNearTo(position.x, position.z))
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

        return minSqrDist == 0 ? -1 : minSqrDist;
    }

    public bool IntersectMarker(CaveBlock block)
    {
        foreach (CavePrefab prefab in IteratePrefabsNearTo(block.x, block.z))
        {
            if (prefab.IntersectMarker(block.position))
            {
                return true;
            }
        }

        return false;
    }
}
