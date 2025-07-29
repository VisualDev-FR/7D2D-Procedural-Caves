using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class CaveUtils
{
    public static int FastMax(int a, int b)
    {
        return a > b ? a : b;
    }

    public static float FastMax(float a, float b)
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

    public static int FastMin(int a, int b)
    {
        return a < b ? a : b;
    }

    public static float FastAbs(float value)
    {
        if (value > 0)
            return value;

        return -value;
    }

    public static int GetChunkHash(int x, int z)
    {
        return x + z * 1031;
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion error: {message}");
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

    public static bool Intersect2D(int x, int z, Vector3i position, Vector3i size)
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

    public static bool Intersect3D(int x, int y, int z, Vector3i start, Vector3i size)
    {
        if (!Intersect2D(x, z, start, size))
            return false;

        if (y < start.y)
            return false;

        if (y >= start.y + size.y)
            return false;

        return true;
    }

    public static bool Intersect3D(Vector3i position, Vector3i start, Vector3i size)
    {
        return Intersect3D(position.x, position.y, position.z, start, size);
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

    public static IEnumerable<BoundingBox> GetCaveMarkers(PrefabInstance prefabInstance)
    {
        if (prefabInstance == null)
            yield break;

        var markers = prefabInstance.prefab.POIMarkers
            .Where(m => m.tags.Test_AnySet(CaveTags.tagCaveMarker))
            .ToArray();

        foreach (var marker in markers)
        {
            yield return new BoundingBox(marker.start + prefabInstance.boundingBoxPosition, marker.size);
        }
    }

    public static bool OverLaps2D(Vector3i position1, Vector3i size1, Vector3i position2, Vector3i size2, int margin = 0)
    {
        if (position1.x + size1.x + margin < position2.x || position2.x + size2.x + margin < position1.x)
            return false;

        if (position1.z + size1.z + margin < position2.z || position2.z + size2.z + margin < position1.z)
            return false;

        return true;
    }

    public static void SetField<T>(object instance, string fieldName, object value)
    {
        var field = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    public static List<List<T>> SplitList<T>(List<T> parent, int count)
    {
        List<List<T>> resultat = new List<List<T>>();
        int subListSize = (int)Math.Ceiling((double)parent.Count / count);

        for (int i = 0; i < parent.Count; i += subListSize)
        {
            List<T> subList = parent.GetRange(i, Math.Min(subListSize, parent.Count - i));
            resultat.Add(subList);
        }

        return resultat;
    }

    public static Vector3i HalfWorldSize(int worldSize)
    {
        return new Vector3i(worldSize >> 1, 0, worldSize >> 1);
    }

    public static int SqrDistanceToRectangle3D(Vector3i point, Vector3i min, Vector3i max)
    {
        int dx = FastMax(min.x - point.x, 0, point.x - max.x);
        int dy = FastMax(min.y - point.y, 0, point.y - max.y);
        int dz = FastMax(min.z - point.z, 0, point.z - max.z);

        return dx * dx + dy * dy + dz * dz;
    }
}
