using System.Collections.Generic;

public class PrefabCache
{
    private static readonly HashSet<CavePrefab> emptyPrefabsHashset = new HashSet<CavePrefab>();

    // all prefabs grouped by chunk, where key is the hashcode of the chunk
    public readonly Dictionary<int, List<CavePrefab>> groupedCavePrefabs;

    public readonly Dictionary<int, List<CavePrefab>> groupedVanillaPrefabs;

    // a dictionary to store the nearest prefabs for each chunk, where key is chunk hashcode, and values are the nearest prefabs
    public readonly Dictionary<int, HashSet<CavePrefab>> nearestPrefabs;

    public readonly List<CavePrefab> Prefabs;

    public int Count => Prefabs.Count;

    public PrefabCache()
    {
        Prefabs = new List<CavePrefab>();
        groupedCavePrefabs = new Dictionary<int, List<CavePrefab>>();
        nearestPrefabs = new Dictionary<int, HashSet<CavePrefab>>();
    }

    public static int GetChunkHash(int x, int z)
    {
        return x + z * 1031;
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        CaveUtils.Assert(!prefab.prefabDataInstance.prefab.Tags.Test_AnySet(CaveConfig.tagRwgStreetTile), "Street tile should not be cached");

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
}
