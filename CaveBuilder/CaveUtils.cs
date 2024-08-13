using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using WorldGenerationEngineFinal;


public static class CaveUtils
{
    public static readonly Vector3i[] neighborsOffsets = new Vector3i[]
    {
        new Vector3i(1, 0, 0),
        new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0),
        new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1),
        new Vector3i(0, 0, -1),
        new Vector3i(0, 1, 1),
        new Vector3i(0, -1, 1),
        new Vector3i(0, 1, -1),
        new Vector3i(0, -1, -1),
        new Vector3i(1, 0, 1),
        new Vector3i(-1, 0, 1),
        new Vector3i(1, 0, -1),
        new Vector3i(-1, 0, -1),
        new Vector3i(1, 1, 0),
        new Vector3i(1, -1, 0),
        new Vector3i(-1, 1, 0),
        new Vector3i(-1, -1, 0),
        new Vector3i(1, 1, 1),
        new Vector3i(1, -1, 1),
        new Vector3i(1, 1, -1),
        new Vector3i(1, -1, -1),
        new Vector3i(-1, 1, 1),
        new Vector3i(-1, -1, 1),
        new Vector3i(-1, 1, -1),
        new Vector3i(-1, -1, -1)
    };

    public static readonly Vector3i[] neighborsOffsetsNonVertical = neighborsOffsets
        .Where(offset => !(FastAbs(offset.y) == 1 && offset.x == 0 && offset.z == 0))
        .ToArray();

    public static Stopwatch StartTimer()
    {
        var timer = new Stopwatch();
        timer.Start();
        return timer;
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion error: {message}");
    }

    public static int FastMin(int a, int b)
    {
        return a < b ? a : b;
    }

    public static int FastMax(int a, int b)
    {
        return a > b ? a : b;
    }

    public static int FastMax(int a, int b, int c)
    {
        if (a > b && a > c)
            return a;

        if (b > c)
            return b;

        return c;
    }

    public static int FastMin(int a, int b, int c)
    {
        if (a < b && a < c)
            return a;

        if (b < c)
            return b;

        return c;
    }

    public static string TimeFormat(Stopwatch timer, string format = @"hh\:mm\:ss")
    {
        return TimeSpan.FromSeconds(timer.ElapsedMilliseconds / 1000).ToString(format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(AstarNode nodeA, AstarNode nodeB)
    {
        return SqrEuclidianDist(nodeA.position, nodeB.position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Vector3i p1, Vector3i p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return dx * dx + dy * dy + dz * dz;
    }

    public static float SqrEuclidianDist2D(Vector3i p1, Vector3i p2)
    {
        float dx = p1.x - p2.x;
        float dz = p1.z - p2.z;

        return dx * dx + dz * dz;
    }

    public static int FastAbs(int value)
    {
        if (value > 0)
            return value;

        return -value;
    }

    public static float FastAbs(float value)
    {
        if (value > 0)
            return value;

        return -value;
    }

    public static float EuclidianDist(Vector3i p1, Vector3i p2)
    {
        return (float)Math.Sqrt(SqrEuclidianDist(p1, p2));
    }

    public static bool PositionIsValid(Vector3i pos)
    {
        return (
            pos.y > 0 && pos.y < WorldBuilder.Instance.GetHeight(pos.x, pos.z)
            && pos.x > 0 && pos.x < CaveBuilder.worldSize
            && pos.z > 0 && pos.z < CaveBuilder.worldSize
        );
    }

    public static List<Vector3i> GetValidNeighbors(Vector3i position)
    {
        List<Vector3i> validNeighbors = new List<Vector3i>();

        foreach (var offset in neighborsOffsets)
        {
            Vector3i neighbor = position + offset;

            CaveUtils.Assert(neighbor != null, "null neighbor");

            if (PositionIsValid(neighbor))
            {
                validNeighbors.Add(neighbor);
            }
        }

        return validNeighbors;
    }

    public static Vector3i RandomVector3i(Random rand, int xMax, int yMax, int zMax)
    {
        int x = rand.Next(xMax);
        int y = rand.Next(yMax);
        int z = rand.Next(zMax);

        return new Vector3i(x, y, z);
    }

    public static HashSet<Vector3i> GetPointsInside(Vector3i p1, Vector3i p2)
    {
        var result = new HashSet<Vector3i>();

        for (int x = p1.x; x < p2.x; x++)
        {
            for (int y = p1.y; y < p2.y; y++)
            {
                for (int z = p1.z; z < p2.z; z++)
                {
                    result.Add(new Vector3i(x, y, z));
                }
            }
        }

        return result;
    }

    public static Vector3i GetRotatedSize(Vector3i Size, int rotation)
    {
        if (rotation == 0 || rotation == 2)
            return new Vector3i(Size.x, Size.y, Size.z);

        return new Vector3i(Size.z, Size.y, Size.x);
    }

    public static bool Intersect2D(Vector3i point, Vector3i position, Vector3i size)
    {
        if (point.x < position.x)
            return false;

        if (point.x >= position.x + size.x)
            return false;

        if (point.z < position.z)
            return false;

        if (point.z >= position.z + size.z)
            return false;

        return true;
    }

    public static bool Intersect3D(Vector3i point, Vector3i position, Vector3i size)
    {
        if (!Intersect2D(point, position, size))
            return false;

        if (point.y < position.y)
            return false;

        if (point.y >= position.y + size.z)
            return false;

        return true;
    }

}
