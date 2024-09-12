using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using WorldGenerationEngineFinal;


public static class CaveUtils
{
    public static readonly Vector3i[] offsets = new Vector3i[]
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

    public static readonly int[] offsetHashes = offsets
        .Select(offset => CaveBlock.GetHashCode(offset.x, offset.y, offset.z))
        .ToArray();

    public static readonly int[] offsetsHorizontalHashes = offsets
        .Where(offset => offset.y == 0)
        .Select(offset => PrefabCache.GetChunkHash(offset.x, offset.z))
        .ToArray();

    public static readonly Vector3i[] offsetsHorizontal8 = offsets
        .Where(offset => offset.y == 0)
        .ToArray();

    public static readonly Vector3i[] offsetsHorizontal4 = offsets
        .Where(offset => offset.y == 0 && (offset.x == 0 || offset.z == 0))
        .ToArray();

    public static readonly Vector3i[] offsetsNoDiagonal = offsets
        .Where(offset =>
               (offset.x == 0 && offset.y == 0)
            || (offset.x == 0 && offset.z == 0)
            || (offset.y == 0 && offset.z == 0))
        .ToArray();

    public static readonly Vector3i[] offsetsNoVertical = offsets
        .Where(offset => offset.y == 0 || offset.x != 0 || offset.z != 0)
        .ToArray();

    public static readonly Vector3i[] offsetsBelow = offsets
        .Where(offset => offset.y == -1)
        .ToArray();

    public static Stopwatch StartTimer()
    {
        var timer = new Stopwatch();
        timer.Start();
        return timer;
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion error: {message}");
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Vector3 p1, Vector3 p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return dx * dx + dy * dy + dz * dz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SqrEuclidianDistInt32(Vector3i p1, Vector3i p2)
    {
        int dx = p1.x - p2.x;
        int dy = p1.y - p2.y;
        int dz = p1.z - p2.z;

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
               pos.x > 0 && pos.x < CaveBuilder.worldSize
            && pos.z > 0 && pos.z < CaveBuilder.worldSize
            && pos.y > 0 && pos.y < WorldBuilder.Instance.GetHeight(pos.x, pos.z)
        );
    }

    public static List<Vector3i> GetValidNeighbors(Vector3i position)
    {
        List<Vector3i> validNeighbors = new List<Vector3i>();

        foreach (var offset in offsets)
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

    public static Vector3i RandomVector3i(System.Random rand, int xMax, int yMax, int zMax)
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

    public static bool Intersect2D(int x, int y, int z, Vector3i position, Vector3i size)
    {
        if (x < position.x)
            return false;

        if (x >= position.x + size.x)
            return false;

        if (z < position.z)
            return false;

        if (z >= position.z + size.z)
            return false;

        return true;
    }

    public static bool Intersect3D(int x, int y, int z, Vector3i position, Vector3i size)
    {
        if (!Intersect2D(x, y, z, position, size))
            return false;

        if (y < position.y)
            return false;

        if (y >= position.y + size.z)
            return false;

        return true;
    }

    public static List<Vector3i> GetBoundingEdges(Vector3i position, Vector3i size)
    {
        List<Vector3i> points = new List<Vector3i>();

        int x0 = position.x;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int z1 = z0 + size.z;

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z0));
        }

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z1));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x0, position.y, z));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x1, position.y, z));
        }

        return points;
    }

    public static bool OverLaps2D(Vector3i position1, Vector3i size1, Vector3i position2, Vector3i size2, int margin = 0)
    {
        if (position1.x + size1.x + margin < position2.x || position2.x + size2.x + margin < position1.x)
            return false;

        if (position1.z + size1.z + margin < position2.z || position2.z + size2.z + margin < position1.z)
            return false;

        return true;
    }

    public static int SqrDistanceToRectangle3D(Vector3i point, Vector3i min, Vector3i max)
    {
        int dx = FastMax(min.x - point.x, 0, point.x - max.x);
        int dy = FastMax(min.y - point.y, 0, point.y - max.y);
        int dz = FastMax(min.z - point.z, 0, point.z - max.z);

        return dx * dx + dy * dy + dz * dz;
    }

    public static void SetField<T>(object instance, string fieldName, object value)
    {
        var field = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    public static readonly float terrainOffset = 50;

    public static float ClampHeight(float height)
    {
        return terrainOffset + (255f - terrainOffset) * height / 255f;
    }

}
