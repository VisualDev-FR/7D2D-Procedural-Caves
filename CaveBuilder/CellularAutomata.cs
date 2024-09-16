using System;
using System.Collections.Generic;
using System.Threading;


public class CellAut
{

    public static int size = 40;

    public static string seed = "seed";

    public static bool useRandomSeed = true;

    public static int randomFillPercent = 50;

    public static Random pseudoRandom;

    public static int[,] map;

    public static void execute(string[] args)
    {

        map = new int[size, size];

        RandomFillMap();

        for (int i = 0; i < 5; i++)
        {
            Console.Clear();
            PrintMap(map);
            SmoothMap();
            Thread.Sleep(1000);
        }
    }

    public static void PrintMap(int[,] map)
    {
        for (int x = 0; x < size; x++)
        {
            var row = new List<string>();

            for (int y = 0; y < size; y++)
            {
                if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                {
                    row.Add("+");
                }
                else
                {
                    row.Add(map[x, y] == 0 ? "." : " ");
                }
            }

            Console.WriteLine(string.Join(" ", row));
        }
    }

    public static void RandomFillMap()
    {

        if (useRandomSeed)
        {
            pseudoRandom = new System.Random();
        }
        else
        {
            pseudoRandom = new System.Random(seed.GetHashCode());
        }


        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    public static void SmoothMap()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)
                {
                    map[x, y] = 1;
                }
                else if (neighbourWallTiles < 4)
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    public static int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < size && neighbourY >= 0 && neighbourY < size)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

}
