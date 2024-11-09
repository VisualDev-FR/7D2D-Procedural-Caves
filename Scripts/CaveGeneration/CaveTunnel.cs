using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CaveTunnel
{
    public readonly List<CaveBlock> path = new List<CaveBlock>();

    public readonly HashSet<CaveBlock> blocks = new HashSet<CaveBlock>();

    public IEnumerable<CaveBlock> LocalMinimas => FindLocalMinimas();

    private readonly RawHeightMap heightMap;

    private readonly int worldSize;

    private static readonly Vector3[] INNER_POINTS = new Vector3[17]
    {
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0f, 0f, 0f),
        new Vector3(1f, 0f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(1f, 1f, 0f),
        new Vector3(0f, 1f, 1f),
        new Vector3(1f, 0f, 1f),
        new Vector3(1f, 1f, 1f),
        new Vector3(0.25f, 0.25f, 0.25f),
        new Vector3(0.75f, 0.25f, 0.25f),
        new Vector3(0.25f, 0.75f, 0.25f),
        new Vector3(0.25f, 0.25f, 0.75f),
        new Vector3(0.75f, 0.75f, 0.25f),
        new Vector3(0.25f, 0.75f, 0.75f),
        new Vector3(0.75f, 0.25f, 0.75f),
        new Vector3(0.75f, 0.75f, 0.75f)
    };

    public CaveTunnel(GraphEdge edge, CavePrefabManager cachedPrefabs, RawHeightMap heightMap, int worldSize, int seed)
    {
        CaveNoise.pathingNoise.SetSeed(seed);

        this.heightMap = heightMap;
        this.worldSize = worldSize;

        FindPath(edge, cachedPrefabs);
        ThickenTunnel(edge.node1, edge.node2, seed);
    }

    // private API
    private void FindPath(GraphEdge edge, CavePrefabManager cachedPrefabs)
    {
        var HalfWorldSize = CaveUtils.HalfWorldSize(worldSize);

        var start = edge.node1.Normal(Utils.FastMax(5, edge.node1.NodeRadius));
        var target = edge.node2.Normal(Utils.FastMax(5, edge.node2.NodeRadius));

        var yMin = Utils.FastMin(start.y, target.y);
        var yMax = Utils.FastMax(start.y, target.y);

        if (cachedPrefabs.MinSqrDistanceToPrefab(start) == 0)
        {
            Log.Warning($"[Cave] '{edge.Prefab1.PrefabName}' ({start - HalfWorldSize}) intersect with another prefab");
            return;
        }

        if (cachedPrefabs.MinSqrDistanceToPrefab(target) == 0)
        {
            Log.Warning($"[Cave] '{edge.Prefab2.PrefabName}' ({target - HalfWorldSize}) intersect with another prefab");
            return;
        }

        var startNode = new AstarNode(start, edge.node1.position);
        var goalNode = new AstarNode(target, edge.node2.position);

        var queue = new HashedPriorityQueue<AstarNode>();
        var visited = new HashSet<int>();

        int bedRockMargin = CaveConfig.bedRockMargin + 1;
        int terrainMargin = CaveConfig.terrainMargin + 1;
        int sqrMinPrefabDistance = 100;
        int neighborDistance = 1;
        int index = 0;

        queue.Enqueue(startNode, float.MaxValue);

        while (queue.Count > 0 && index++ < 1_000_000)
        {
            AstarNode currentNode = queue.Dequeue();

            if (currentNode.hashcode == goalNode.hashcode)
            {
                ReconstructPath(currentNode);
                return;
            }

            visited.Add(currentNode.hashcode);

            foreach (var offset in CaveUtils.offsetsNoVertical)
            {
                Vector3i neighborPos = currentNode.position + offset;

                bool shouldContinue =
                    neighborPos.y < bedRockMargin
                    || neighborPos.y + terrainMargin > heightMap.GetHeight(neighborPos.x, neighborPos.z)
                    || visited.Contains(neighborPos.GetHashCode()); // NOTE: AstarNode and Vector3i must have same hashcode function

                if (shouldContinue)
                    continue;

                float minDist = cachedPrefabs.MinSqrDistanceToPrefab(neighborPos);

                if (minDist == 0)
                {
                    continue;
                }

                AstarNode neighbor = new AstarNode(neighborPos, currentNode);

                Vector3i dir = currentNode.direction + neighbor.direction;
                bool isCave = CaveNoise.pathingNoise.IsCave(neighborPos);
                int factor = 0;

                if (!isCave) factor += 1;
                if (minDist < sqrMinPrefabDistance) factor += 1;
                if (dir.x == 0) factor += 1;
                if (dir.z == 0) factor += 1;
                if (neighborPos.y > yMax || neighborPos.y < yMin) factor += 1;

                float tentativeGCost = currentNode.GCost + (neighborDistance << factor);

                // TODO: try to remove condition 'tentativeGCost < neighbor.GCost'
                // -> it seems to be useless (to be confirmed)
                // -> try with condition 'tentativeGCost < currentNode.GCost' instead
                if (tentativeGCost < neighbor.GCost || !queue.Contains(neighbor))
                {
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CaveUtils.SqrEuclidianDistInt32(neighbor.position, goalNode.position) << factor;

                    if (!queue.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor, neighbor.FCost);
                    }
                }
            }
        }

        // reaching here mean no path was found
        var height1 = heightMap.GetHeight(start.x, start.z);
        var height2 = heightMap.GetHeight(target.x, target.z);

        var p1 = start - HalfWorldSize;
        var p2 = target - HalfWorldSize;

        Log.Warning($"No Path found from '{edge.Prefab1.PrefabName}' ({p1} / {height1}) to '{edge.Prefab2.PrefabName}' ({p2} / ({height2})) after {index} iterations ");
    }

    private IEnumerable<CaveBlock> FindLocalMinimas()
    {
        for (int i = 1; i < path.Count - 1; i++)
        {
            if (IsLocalMinima(path, i))
            {
                yield return path[i];
            }
            else if (IsStartOfFlatMinimum(path, i))
            {
                if (IsFlatMinimum(path, ref i))
                {
                    yield return path[i];
                }
            }
        }
    }

    private void ThickenTunnel(GraphNode start, GraphNode target, int seed)
    {
        // TODO: handle duplicates with that instead of hashset: https://stackoverflow.com/questions/1672412/filtering-duplicates-out-of-an-ienumerable

        blocks.UnionWith(start.GetSphere());
        blocks.UnionWith(target.GetSphere());

        int r1 = start.NodeRadius;
        int r2 = target.NodeRadius;

        var noise = new Noise1D(new System.Random(seed), r1, r2, path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = noise.Interpolate(i);
            var sphere = GetSphere(path[i].ToVector3i(), tunnelRadius)
                .Where(caveBlock =>
                       caveBlock.y > CaveConfig.bedRockMargin
                    && caveBlock.y + CaveConfig.terrainMargin < (int)heightMap.GetHeight(caveBlock.x, caveBlock.z));

            blocks.UnionWith(sphere);
        }
    }

    private void ReconstructPath(AstarNode currentNode)
    {
        while (currentNode != null)
        {
            var block = new CaveBlock(currentNode.position);
            path.Add(block);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
    }

    // static API
    public static readonly int minRadius = 2;

    public static readonly int maxRadius = 10;

    public static readonly Dictionary<int, HashSet<int>> spheresMapping = new Dictionary<int, HashSet<int>>();

    public static readonly Dictionary<int, Vector3i> spheres = InitSpheres();

    public static Dictionary<int, Vector3i> InitSpheres(int maxRadius = -1)
    {
        if (maxRadius < 0)
            maxRadius = CaveTunnel.maxRadius;

        var spheres = new Dictionary<int, Vector3i>() { { 0, Vector3i.zero } };
        var queue = new HashSet<Vector3i>() { Vector3i.zero };
        var visited = new HashSet<Vector3i>();

        Vector3i pos = new Vector3i();

        for (int i = minRadius; i <= maxRadius; i++)
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
        var radius = (int)Utils.FastClamp(_radius, minRadius, maxRadius);

        foreach (var hashcode in spheresMapping[radius])
        {
            yield return new CaveBlock(
                center.x + spheres[hashcode].x,
                center.y + spheres[hashcode].y,
                center.z + spheres[hashcode].z
            );
        }
    }

    private static bool IsLocalMinima(List<CaveBlock> path, int i)
    {
        return path[i - 1].y > path[i].y && path[i].y < path[i + 1].y;
    }

    private static bool IsStartOfFlatMinimum(List<CaveBlock> path, int i)
    {
        return path[i - 1].y > path[i].y && path[i].y == path[i + 1].y;
    }

    private static bool IsFlatMinimum(List<CaveBlock> path, ref int i)
    {

        while (i < path.Count - 1 && path[i].y == path[i + 1].y)
        {
            i++;
        }

        return i < path.Count - 1 && path[i].y < path[i + 1].y;
    }

    private static IEnumerable<CaveBlock> GetSphereV2(CaveBlock center, float sphereRadius, CaveMap cavemap)
    {
        // adapted from ItemActionTerrainTool.RemoveTerrain

        Vector3 worldPos = new Vector3(center.x, center.y, center.z);

        if (sphereRadius < 2f) sphereRadius = 2f;

        int x_min = Utils.Fastfloor(worldPos.x - sphereRadius);
        int x_max = Utils.Fastfloor(worldPos.x + sphereRadius);
        int y_min = Utils.Fastfloor(worldPos.y - sphereRadius);
        int y_max = Utils.Fastfloor(worldPos.y + sphereRadius);
        int z_min = Utils.Fastfloor(worldPos.z - sphereRadius);
        int z_max = Utils.Fastfloor(worldPos.z + sphereRadius);

        for (int x = x_min; x <= x_max; x++)
        {
            for (int y = y_min; y <= y_max; y++)
            {
                for (int z = z_min; z <= z_max; z++)
                {
                    int pointIndex = 0;
                    for (int i = 0; i < INNER_POINTS.Length; i++)
                    {
                        Vector3 vector = INNER_POINTS[i];
                        if ((new Vector3(x + vector.x, y + vector.y, z + vector.z) - worldPos).magnitude <= sphereRadius)
                        {
                            pointIndex++;
                        }
                        if (i == 8)
                        {
                            switch (pointIndex)
                            {
                                case 9:
                                    pointIndex = INNER_POINTS.Length;
                                    break;
                                default:
                                    continue;
                                case 0:
                                    break;
                            }
                            break;
                        }
                    }
                    if (pointIndex == 0)
                    {
                        continue;
                    }

                    int hashcode = CaveBlock.GetHashCode(x, y, z);

                    sbyte initialDensity = 0;
                    sbyte density = initialDensity;

                    if (pointIndex > INNER_POINTS.Length / 2)
                    {
                        density = (sbyte)((float)MarchingCubes.DensityAir * (pointIndex - INNER_POINTS.Length / 2 - 1) / (INNER_POINTS.Length / 2));
                        if (density <= 0)
                        {
                            density = 1;
                        }
                    }
                    else if (!cavemap.IsCaveAir(hashcode))
                    {
                        density = (sbyte)((float)MarchingCubes.DensityTerrain * (INNER_POINTS.Length / 2 - pointIndex) / (INNER_POINTS.Length / 2));
                        if (density >= 0)
                        {
                            density = -1;
                        }
                    }

                    if (density > initialDensity)
                    {
                        yield return new CaveBlock(x, y, z, density);
                    }
                }
            }
        }

        yield break;
    }

}
