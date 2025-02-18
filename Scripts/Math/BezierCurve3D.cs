using System;
using System.Collections.Generic;
using UnityEngine;

public class BezierCurve3D
{
    public HashSet<Vector3i> GetPoints(int nbPoints, Vector3i P0, Vector3i P1, Vector3i P2, Vector3i P3)
    {
        var positions = new HashSet<Vector3i>();

        Vector3i previous = Vector3i.zero;

        for (int i = 0; i <= nbPoints; i++)
        {
            float t = i / (float)nbPoints;

            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 point = uuu * P0; // (1-t)^3 * P0
            point += 3 * uu * t * P1; // 3*(1-t)^2 * t * P1
            point += 3 * u * tt * P2; // 3*(1-t) * t^2 * P2
            point += ttt * P3;        // t^3 * P3

            var position = new Vector3i(point);

            if (previous != Vector3i.zero)
            {
                positions.UnionWith(Bresenham3D(previous, position));
            }

            previous = position;

            positions.Add(position);
        }

        return positions;
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