using System.Collections.Generic;
using System.Linq;

public struct RLEDatas
{
    public int x;

    public int z;

    public int yMin;

    public int yMax;

    public int zxHash => CaveBlock.HashZX(x, z);
}


public class SphereManager
{
    private static readonly Dictionary<int, List<RLEDatas>> spheres = new Dictionary<int, List<RLEDatas>>();

    public static void InitSpheres(int maxRadius = -1)
    {
        if (maxRadius < 0)
            maxRadius = CaveConfig.maxTunnelRadius;

        for (int radius = CaveConfig.minTunnelRadius; radius <= maxRadius; radius++)
        {
            spheres[radius] = CreateSphere(radius);
        }
    }

    public static List<RLEDatas> CreateSphere(int radius)
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

                int sqrMagnitude = pos.x * pos.x + pos.y * pos.y + pos.z * pos.z;

                if (!visited.Contains(pos) && sqrMagnitude < sqrRadius)
                {
                    queue.Add(pos);
                }
            }

            visited.Add(currentPosition);
            queue.Remove(currentPosition);
        }

        var rleDatas = new List<RLEDatas>();
        var rleData = new RLEDatas();

        foreach (var group in visited.GroupBy(p => new Vector2i(p.x, p.z)))
        {
            rleData.yMin = group.Min(p => p.y);
            rleData.yMax = group.Max(p => p.y);
            rleData.x = group.Key.x;
            rleData.z = group.Key.y;

            rleDatas.Add(rleData);
        }

        return rleDatas;
    }

    public static IEnumerable<Vector3i> GetSphere(Vector3i center, float _radius)
    {
        var radius = (int)Utils.FastClamp(_radius, CaveConfig.minTunnelRadius, CaveConfig.maxTunnelRadius);

        foreach (var data in spheres[radius])
        {
            for (int y = data.yMin; y <= data.yMax; y++)
            {
                yield return new Vector3i(center.x + data.x, center.y + y, center.z + data.z);
            }
        }
    }

    public static IEnumerable<RLEDatas> GetSphereLRE(Vector3i center, float _radius)
    {
        var radius = (int)Utils.FastClamp(_radius, CaveConfig.minTunnelRadius, CaveConfig.maxTunnelRadius);
        var rleData = new RLEDatas();

        CaveUtils.Assert(spheres.ContainsKey(radius), $"radius: {radius}, spheres count: {spheres.Count}");

        foreach (var sphereLayer in spheres[radius])
        {
            rleData.yMin = sphereLayer.yMin + center.y;
            rleData.yMax = sphereLayer.yMax + center.y;

            CaveUtils.Assert(rleData.yMin > 0 && rleData.yMin < 256, $"invalid yMin: {rleData.yMin}, center: {center.y}");
            CaveUtils.Assert(rleData.yMax > 0 && rleData.yMax < 256, $"invalid yMax: {rleData.yMax}, center: {center.y}");

            rleData.x = center.x + sphereLayer.x;
            rleData.z = center.z + sphereLayer.z;

            yield return rleData;
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