using System;
using System.Collections.Generic;
using System.Threading;


public class CellAut
{
    public static Vector3i size = new Vector3i(20, 10, 20);

    public static Vector3i Center => size / 2;

    public static string seed = "";

    public static int randomFillPercent = 60;

    public static Random pseudoRandom;

    public static int[,,] map;

    public static void Execute(string[] args)
    {
        var width = 50;
        var height = 50;

        size = new Vector3i(width, height, width);
        map = new int[size.x, size.y, size.z];
        pseudoRandom = InitRandom();

        var timer = CaveUtils.StartTimer();

        RandomFillMap();

        for (int i = 0; i < 5; i++)
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

        Log.Out($"{voxels.Count} blocks, timer: {timer.ElapsedMilliseconds} ms");

        CaveViewer.GenerateObjFile("cellular.obj", voxels);
    }

    private static Random InitRandom()
    {
        int iSeed = seed.GetHashCode();

        if (seed == "")
        {
            iSeed = DateTime.Now.GetHashCode();
        }

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
                    map[x, y, z] = pseudoRandom.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

    public static void SmoothMap()
    {
        for (int x = 1; x < size.x - 1; x++)
        {
            for (int y = 1; y < size.y - 1; y++)
            {
                for (int z = 1; z < size.z - 1; z++)
                {
                    int neighbourWallTiles = GetNeighborsCount(x, y, z);

                    if (neighbourWallTiles > 13)
                    {
                        map[x, y, z] = 1;
                    }
                    else if (neighbourWallTiles < 13)
                    {
                        map[x, y, z] = 0;
                    }
                }
            }
        }
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
