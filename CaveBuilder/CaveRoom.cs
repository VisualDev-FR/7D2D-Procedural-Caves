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

    public CaveRoom(Vector3i start, Vector3i size, int seed = -1)
    {
        this.size = size;
        offset = start == null ? Vector3i.zero : start;
        rand = new Random(seed);
        map = new bool[size.x, size.y, size.z];
        markers = new List<CaveMarker>();
    }

    public CaveRoom(CavePrefab prefab, int seed = -1)
    {
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

        var temp = new Vector3i();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if ((map[x, y, z] && !invert) || (!map[x, y, z] && invert))
                    {
                        temp.x = x + offset.x;
                        temp.y = y + offset.y;
                        temp.z = z + offset.z;

                        yield return temp;
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
            var path = Bresenham3D(marker.start, center);

            foreach (var p in path)
            {
                int x = p.x;
                int y = p.y;
                int z = p.z;

                if (x >= 0 && y >= 0 && z >= 0 && x < size.x && y < size.y && z < size.z && !map[x, y, z])
                {
                    SetSphere(new Vector3i(x, y, z), 2);
                }
            }
        }
    }

    private void SetSphere(Vector3i center, int radius)
    {
        foreach (var hashcode in CaveTunnel.spheresMapping[radius])
        {
            var position = CaveTunnel.spheres[hashcode];

            int x = center.x + position.x;
            int y = center.y + position.y;
            int z = center.z + position.z;

            if (x >= 0 && y >= 0 && z >= 0 && x < size.x && y < size.y && z < size.z)
            {
                map[x, y, z] = true;
            }
        }
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

    public static List<Vector3i> Bresenham3D(Vector3i v1, Vector3i v2)
    {
        // https://www.geeksforgeeks.org/bresenhams-algorithm-for-3-d-line-drawing/

        var ListOfPoints = new List<Vector3i>
        {
            v1
        };

        int dx = Math.Abs(v2.x - v1.x);
        int dy = Math.Abs(v2.y - v1.y);
        int dz = Math.Abs(v2.z - v1.z);
        int xs;
        int ys;
        int zs;

        if (v2.x > v1.x)
            xs = 1;
        else
            xs = -1;
        if (v2.y > v1.y)
            ys = 1;
        else
            ys = -1;
        if (v2.z > v1.z)
            zs = 1;
        else
            zs = -1;

        // Driving axis is X-axis"
        if (dx >= dy && dx >= dz)
        {
            int p1 = 2 * dy - dx;
            int p2 = 2 * dz - dx;
            while (v1.x != v2.x)
            {
                v1.x += xs;
                if (p1 >= 0)
                {
                    v1.y += ys;
                    p1 -= 2 * dx;
                }
                if (p2 >= 0)
                {
                    v1.z += zs;
                    p2 -= 2 * dx;
                }
                p1 += 2 * dy;
                p2 += 2 * dz;
                ListOfPoints.Add(v1);
            }
        }
        // Driving axis is Y-axis"
        else if (dy >= dx && dy >= dz)
        {
            int p1 = 2 * dx - dy;
            int p2 = 2 * dz - dy;
            while (v1.y != v2.y)
            {
                v1.y += ys;
                if (p1 >= 0)
                {
                    v1.x += xs;
                    p1 -= 2 * dy;
                }
                if (p2 >= 0)
                {
                    v1.z += zs;
                    p2 -= 2 * dy;
                }
                p1 += 2 * dx;
                p2 += 2 * dz;
                ListOfPoints.Add(v1);
            }
        }
        // Driving axis is Z-axis"
        else
        {
            int p1 = 2 * dy - dz;
            int p2 = 2 * dx - dz;
            while (v1.z != v2.z)
            {
                v1.z += zs;
                if (p1 >= 0)
                {
                    v1.y += ys;
                    p1 -= 2 * dz;
                }
                if (p2 >= 0)
                {
                    v1.x += xs;
                    p2 -= 2 * dz;
                }
                p1 += 2 * dy;
                p2 += 2 * dx;
                ListOfPoints.Add(v1);
            }
        }

        return ListOfPoints;
    }

}
