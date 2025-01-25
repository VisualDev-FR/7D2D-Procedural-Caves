using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class SphereManager
{
    private static readonly Dictionary<int, int[,]> spheres = new Dictionary<int, int[,]>();

    public static void InitSpheres(int maxRadius = -1)
    {
        if (maxRadius < 0)
            maxRadius = CaveConfig.maxTunnelRadius;

        for (int i = CaveConfig.minTunnelRadius; i <= maxRadius; i++)
        {
            spheres[i] = CreateSphere(i);
        }
    }

    public static int[,] CreateSphere(int radius)
    {
        var queue = new HashSet<Vector3i>() { Vector3i.zero };
        var visited = new HashSet<Vector3i>();
        var sqrRadius = radius * radius;

        Vector3i pos = new Vector3i();

        while (queue.Count > 0)
        {
            Vector3i currentPosition = queue.First();

            foreach (var offset in CaveUtils.offsets)
            {
                pos.x = currentPosition.x + offset.x;
                pos.y = currentPosition.y + offset.y;
                pos.z = currentPosition.z + offset.z;

                int SqrMagniture = pos.x * pos.x + pos.y * pos.y + pos.z * pos.z;

                if (visited.Contains(pos) || SqrMagniture >= sqrRadius)
                    continue;

                queue.Add(pos);
            }

            visited.Add(currentPosition);
            queue.Remove(currentPosition);
        }

        var layers = new int[2 * radius, 2 * radius];
        var layersCount = 0;

        foreach (var group in visited.GroupBy(p => CaveBlock.HashZX(p.x + radius - 1, p.z + radius - 1)))
        {
            var hash = group.Key;

            CaveBlock.ZXFromHash(hash, out var x, out var z);

            var yMin = group.Min(p => p.y) + radius - 1;
            var yMax = group.Max(p => p.y) + radius - 1;

            // Logging.Debug("x, z :", x, z, "ymin:", yMin, ", ymax:", yMax);

            var layer = new LayerRLE(yMin, yMax, 0);

            layers[x, z] = layer.GetHashCode();
            layersCount++;
        }

        // Logging.Debug($"radius: {radius}, count: {visited.Count}, layers: {layersCount}");

        return layers;
    }

    public static IEnumerable<Vector3i> GetSphere(Vector3i center, float _radius)
    {
        var radius = (int)Utils.FastClamp(_radius, CaveConfig.minTunnelRadius, CaveConfig.maxTunnelRadius);

        for (int x = 0; x < radius * 2; x++)
        {
            for (int z = 0; z < radius * 2; z++)
            {
                var hashcode = spheres[radius][x, z];

                if (hashcode == 0) continue;

                var layer = new LayerRLE(hashcode);

                for (int y = layer.Start; y <= layer.End; y++)
                {
                    yield return new Vector3i(center.x + x, center.y + y, center.z + z);
                }
            }
        }
    }

    public static IEnumerable<Vector3i> GetSpherePositions(Vector3i center, int radius)
    {
        var position = Vector3i.zero;

        foreach (var hashcode in spheresMapping[radius])
        {
            position.x = center.x + spheres[hashcode].x;
            position.y = center.y + spheres[hashcode].y;
            position.z = center.z + spheres[hashcode].z;

            yield return position;
        }
    }

}