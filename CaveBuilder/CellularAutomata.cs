using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class CellAut
{
    public static int width = 100;

    public static int height = 30;

    public static Vector3i size = new Vector3i(50, 100, 50);

    public static string seed = "";

    public static int randomFillPercent = 51;

    public static int passes = 10;

    public static int criteria = 13;

    public static Random pseudoRandom;

    public static byte[,,] map;

    public static void Execute(string[] args)
    {
        long memoryBefore = GC.GetTotalMemory(true);

        map = new byte[size.x, size.y, size.z];
        pseudoRandom = InitRandom();

        var timer = CaveUtils.StartTimer();

        RandomFillMap();

        for (int i = 0; i < passes; i++)
        {
            SmoothMap();
        }

        var voxels = new HashSet<Voxell>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (map[x, y, z] == 1)
                    {
                        voxels.Add(new Voxell(x, y, z));
                    }
                }
            }
        }

        Log.Out($"{voxels.Count} blocks, timer: {timer.ElapsedMilliseconds} ms, memory: {(GC.GetTotalMemory(true) - memoryBefore) / 1_048_000:F0}MB");

        CaveViewer.GenerateObjFile("cellular.obj", voxels);
    }

    private static Random InitRandom()
    {
        int iSeed = seed.GetHashCode();

        if (seed == "")
        {
            iSeed = DateTime.Now.GetHashCode();
        }

        CaveNoise.pathingNoise.SetSeed(iSeed);

        return new Random(iSeed); ;
    }

    public static void RandomFillMap()
    {
        for (int x = 1; x < size.x - 1; x++)
        {
            for (int y = 1; y < size.y - 1; y++)
            {
                for (int z = 1; z < size.z - 1; z++)
                {
                    map[x, y, z] = (byte)(pseudoRandom.Next(0, 100) < randomFillPercent ? 1 : 0);
                }
            }
        }
    }

    public static void AddMarkers()
    {
        var points = new Vector3i[]
        {
            new Vector3i(0, 10, 25),
            new Vector3i(25, 10, 0),
            new Vector3i(25, 10, 50),
        };

        foreach (var pos in points)
        {
            foreach (var p in CaveTunnel.GetSphere(pos, 5f))
            {
                int x = p.x;
                int y = p.y;
                int z = p.z;

                if (x > 0 && y > 0 && z > 0 && x < size.x && y < size.y && z < size.z)
                {
                    map[p.x, p.y, p.z] = 1;
                }
            }
        }
    }

    public static void SmoothMap()
    {
        var newMap = new byte[size.x, size.y, size.z];

        for (int x = 1; x < size.x - 1; x++)
        {
            for (int z = 1; z < size.z - 1; z++)
            {
                for (int y = 1; y < size.y - 1; y++)
                {
                    int neighbourWallTiles = GetNeighborsCount(x, y, z);

                    if (neighbourWallTiles > criteria)
                    {
                        newMap[x, y, z] = 1;
                    }
                    else if (neighbourWallTiles > (criteria - 1))
                    {
                        newMap[x, y, z] = (byte)(pseudoRandom.Next(0, 100) < 50 ? 1 : 0);
                    }
                    else
                    {
                        newMap[x, y, z] = 0;
                    }
                }
            }
        }

        map = newMap;
    }

    public static int GetNeighborsCount(int x, int y, int z)
    {
        int neighborsCount = 0;

        foreach (var offset in CaveUtils.offsets)
        {
            neighborsCount += map[x + offset.x, y + offset.y, z + offset.z];
        }

        return neighborsCount;
    }

}
