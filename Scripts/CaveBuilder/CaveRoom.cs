using System;
using System.Collections.Generic;
using System.Linq;


public struct CaveMarker
{
    public Vector3i start;

    public int radius;

    public CaveMarker(Vector3i start, int radius)
    {
        this.start = start;
        this.radius = radius;
    }
}

public class CaveRoom
{
    public int randomFillPercent = 51;

    public int passes = 10;

    public int criteria = 13;

    private readonly Vector3i size;

    private readonly Vector3i offset;

    private readonly Random rand;

    private bool[,,] map;

    private List<CaveMarker> markers;

    private readonly int seed;

    public CaveRoom(Vector3i start, Vector3i size, int seed = -1)
    {
        this.seed = seed;
        this.size = size;
        offset = start == null ? Vector3i.zero : start;
        rand = new Random(seed);
        map = new bool[size.x, size.y, size.z];
        markers = new List<CaveMarker>();
    }

    public CaveRoom(CavePrefab prefab, int seed = -1)
    {
        this.seed = seed;
        size = prefab.Size;
        offset = prefab.position;
        rand = new Random(seed);
        map = new bool[size.x, size.y, size.z];
        markers = prefab.nodes
            .Select(node => new CaveMarker(GraphNode.MarkerCenter(node.marker), 3))
            .ToList();
    }

    public IEnumerable<Vector3i> GetBlocks(bool invert = false)
    {
        RandomFillMap();

        for (int i = 0; i < passes; i++)
        {
            SmoothMap();
        }

        AddMarkers();

        var blockPos = new Vector3i();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if ((map[x, y, z] && !invert) || (!map[x, y, z] && invert))
                    {
                        blockPos.x = x + offset.x;
                        blockPos.y = y + offset.y;
                        blockPos.z = z + offset.z;

                        yield return blockPos;
                    }
                }
            }
        }
    }

    private void RandomFillMap()
    {
        for (int x = 1; x < size.x - 1; x++)
        {
            for (int y = 1; y < size.y - 1; y++)
            {
                for (int z = 1; z < size.z - 1; z++)
                {
                    map[x, y, z] = rand.Next(0, 100) < randomFillPercent;
                }
            }
        }
    }

    public void AddMarkers()
    {
        var center = new Vector3i(size.x / 2, size.y / 2, size.z / 2);

        foreach (var marker in markers)
        {
            var path = FindPathToCenter(marker.start, center);

            foreach (var p in path)
            {
                SetSphere(p, 3);
            }
        }
    }

    private bool IsInside(Vector3i p)
    {
        return p.x >= 0 && p.y >= 0 && p.z >= 0 && p.x < size.x && p.y < size.y && p.z < size.z;
    }

    private void SetSphere(Vector3i center, int radius)
    {
        throw new NotImplementedException();
        // foreach (var hashcode in SphereManager.spheresMapping[radius])
        // {
        //     var position = SphereManager.spheres[hashcode];

        //     int x = center.x + position.x;
        //     int y = center.y + position.y;
        //     int z = center.z + position.z;

        //     if (x >= 0 && y >= 0 && z >= 0 && x < size.x && y < size.y && z < size.z)
        //     {
        //         map[x, y, z] = true;
        //     }
        // }
    }

    private void SmoothMap()
    {
        var newMap = new bool[size.x, size.y, size.z];

        for (int x = 1; x < size.x - 1; x++)
        {
            for (int z = 1; z < size.z - 1; z++)
            {
                for (int y = 1; y < size.y - 1; y++)
                {
                    int neighbourWallTiles = GetNeighborsCount(x, y, z);

                    if (neighbourWallTiles > criteria)
                    {
                        newMap[x, y, z] = true;
                    }
                    else if (neighbourWallTiles > (criteria - 1))
                    {
                        newMap[x, y, z] = rand.Next(0, 100) < 50;
                    }
                    else
                    {
                        newMap[x, y, z] = false;
                    }
                }
            }
        }

        map = newMap;
    }

    private int GetNeighborsCount(int x, int y, int z)
    {
        int neighborsCount = 0;

        foreach (var offset in CaveUtils.offsets)
        {
            if (map[x + offset.x, y + offset.y, z + offset.z])
            {
                neighborsCount++;
            }
        }

        return neighborsCount;
    }

    private IEnumerable<Vector3i> FindPathToCenter(Vector3i start, Vector3i center)
    {
        var startNode = new AstarNode(start);
        var queue = new Queue<AstarNode>();
        var visited = new HashSet<Vector3i>();
        int index = 0;

        queue.Enqueue(startNode);

        while (queue.Count > 0 && index++ < 10_000)
        {
            AstarNode currentNode = queue.Dequeue();

            if (IsInside(currentNode.position) && map[currentNode.position.x, currentNode.position.y, currentNode.position.z])
            {
                return currentNode.ReconstructPath().Select(block => block.ToVector3i());
            }

            visited.Add(currentNode.position);

            foreach (var offset in CaveUtils.offsetsNoVertical)
            {
                Vector3i neighborPos = currentNode.position + offset;

                if (!IsInside(neighborPos) || visited.Contains(neighborPos))
                    continue;

                AstarNode neighbor = new AstarNode(neighborPos, currentNode);

                if (!queue.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        Logging.Warning($"room: no path found, index: {index}, seed: {seed}");

        return new List<Vector3i>();
    }
}
