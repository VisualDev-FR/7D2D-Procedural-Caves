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
                positions.UnionWith(CaveRoom.Bresenham3D(previous, position));
            }

            previous = position;

            positions.Add(position);
        }

        return positions;
    }
}