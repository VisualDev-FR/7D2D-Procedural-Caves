using System;
using System.Collections.Generic;

public class PrefabCache
{
    public Dictionary<Vector2s, List<CavePrefab>> groupedPrefabs;

    public List<CavePrefab> Prefabs;

    public int Count => Prefabs.Count;

    public PrefabCache()
    {
        Prefabs = new List<CavePrefab>();
        groupedPrefabs = new Dictionary<Vector2s, List<CavePrefab>>();
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        var chunkPositions = prefab.GetOverlappingChunks();

        foreach (var chunkPos in chunkPositions)
        {
            if (!groupedPrefabs.ContainsKey(chunkPos))
                groupedPrefabs[chunkPos] = new List<CavePrefab>();

            groupedPrefabs[chunkPos].Add(prefab);
        }
    }

    public static IEnumerable<Vector2s> BrowseNeighborsChunks(int chunkX, int chunkZ, bool includeGiven = false)
    {
        if (includeGiven)
            yield return new Vector2s(chunkX, chunkZ);

        yield return new Vector2s(chunkX + 1, chunkZ);
        yield return new Vector2s(chunkX - 1, chunkZ);
        yield return new Vector2s(chunkX, chunkZ + 1);
        yield return new Vector2s(chunkX, chunkZ - 1);
        yield return new Vector2s(chunkX + 1, chunkZ - 1);
        yield return new Vector2s(chunkX - 1, chunkZ - 1);
        yield return new Vector2s(chunkX + 1, chunkZ + 1);
        yield return new Vector2s(chunkX - 1, chunkZ + 1);
    }

    public HashSet<CavePrefab> GetNearestPrefabs(int x, int z)
    {
        var nearestPrefabs = new HashSet<CavePrefab>();

        int chunkX = x / 16;
        int chunkZ = z / 16;

        foreach (var chunkPos in BrowseNeighborsChunks(chunkX, chunkZ, includeGiven: true))
        {
            if (!groupedPrefabs.TryGetValue(chunkPos, out var chunkPrefabs))
                continue;

            foreach (var prefab in chunkPrefabs)
            {
                nearestPrefabs.Add(prefab);
            }
        }

        return nearestPrefabs;
    }

    public float MinDistToPrefab(Vector3i position)
    {
        float minDist = int.MaxValue;
        var prefabs = GetNearestPrefabs(position.x, position.z);

        foreach (var prefab in prefabs)
        {
            var min = prefab.position;
            var max = min + prefab.Size;

            if (prefab.Intersect3D(position))
            {
                return 0f;
            }

            float dx = Math.Max(Math.Max(min.x - position.x, 0), position.x - max.x);
            float dy = Math.Max(Math.Max(min.y - position.y, 0), position.y - max.y);
            float dz = Math.Max(Math.Max(min.z - position.z, 0), position.z - max.z);

            float sqrDistance = dx * dx + dy * dy + dz * dz;

            if (sqrDistance < minDist)
            {
                minDist = sqrDistance;
            }
        }

        return minDist == 0 ? -1 : minDist;
    }


    public bool IntersectMarker(CaveBlock block)
    {
        var nearestPrefabs = GetNearestPrefabs(block.x, block.z);

        foreach (CavePrefab prefab in nearestPrefabs)
        {
            if (prefab.IntersectMarker(block.position))
            {
                return true;
            }
        }

        return false;
    }
}
