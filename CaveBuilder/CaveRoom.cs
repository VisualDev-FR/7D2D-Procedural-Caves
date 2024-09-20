using System;
using System.Collections.Generic;


public class CaveRoom
{
    public int randomFillPercent = 51;

    public int passes = 10;

    public int criteria = 13;

    private readonly Vector3i size;

    private readonly Vector3i offset;

    private readonly Random rand;

    private bool[,,] map;

    public CaveRoom(Vector3i start, Vector3i size, int seed = -1)
    {
        Log.Out($"start: {start}, size: {size}, seed: {seed}");
        this.size = size;
        offset = start == null ? Vector3i.zero : start;
        rand = new Random(seed);
        map = new bool[size.x, size.y, size.z];
    }

    public IEnumerable<Vector3i> GetBlocks(bool invert = false)
    {
        RandomFillMap();

        for (int i = 0; i < passes; i++)
        {
            SmoothMap();
        }

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

}
