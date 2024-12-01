using System;
using System.Collections.Generic;
using System.Linq;

public class SphereManager
{
    public static readonly Dictionary<int, HashSet<int>> spheresMapping = new Dictionary<int, HashSet<int>>();

    public static readonly Dictionary<int, Vector3i> spheres = InitSpheres();

    public static Dictionary<int, Vector3i> InitSpheres(int maxRadius = -1)
    {
        if (maxRadius < 0)
            maxRadius = CaveConfig.maxTunnelRadius;

        var spheres = new Dictionary<int, Vector3i>() { { 0, Vector3i.zero } };
        var queue = new HashSet<Vector3i>() { Vector3i.zero };
        var visited = new HashSet<Vector3i>();

        Vector3i pos = new Vector3i();

        for (int i = CaveConfig.minTunnelRadius; i <= maxRadius; i++)
        {
            spheresMapping[i] = new HashSet<int>() { Vector3i.zero.GetHashCode() };
        }

        while (queue.Count > 0)
        {
            Vector3i currentPosition = queue.First();

            foreach (var offset in CaveUtils.offsets)
            {
                pos.x = currentPosition.x + offset.x;
                pos.y = currentPosition.y + offset.y;
                pos.z = currentPosition.z + offset.z;

                if (visited.Contains(pos))
                    continue;

                int magnitude = (int)Math.Sqrt(pos.x * pos.x + pos.y * pos.y + pos.z * pos.z);

                if (magnitude >= maxRadius)
                    continue;

                for (int radius = magnitude + 1; radius <= maxRadius; radius++)
                {
                    if (spheresMapping.ContainsKey(radius))
                    {
                        spheresMapping[radius].Add(pos.GetHashCode());
                    }
                }

                spheres[pos.GetHashCode()] = pos;
                queue.Add(pos);
            }

            visited.Add(currentPosition);
            queue.Remove(currentPosition);
        }

        return spheres;
    }

    public static IEnumerable<CaveBlock> GetSphere(Vector3i center, float _radius)
    {
        var radius = (int)Utils.FastClamp(_radius, CaveConfig.minTunnelRadius, CaveConfig.maxTunnelRadius);

        foreach (var hashcode in spheresMapping[radius])
        {
            yield return new CaveBlock(
                center.x + spheres[hashcode].x,
                center.y + spheres[hashcode].y,
                center.z + spheres[hashcode].z
            );
        }
    }

}