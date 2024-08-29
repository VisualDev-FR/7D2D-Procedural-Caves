// # pragma warning disable CS0436

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldGenerationEngineFinal;

public class CaveTunneler
{
    public readonly List<CaveBlock> path = new List<CaveBlock>();

    public readonly HashSet<CaveBlock> localMinimas = new HashSet<CaveBlock>();

    public readonly HashSet<CaveBlock> tunnel = new HashSet<CaveBlock>();

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

    private Vector3i HalfWorldSize => new Vector3i(CaveBuilder.worldSize / 2, 0, CaveBuilder.worldSize / 2);

    // public API
    public HashSet<CaveBlock> GenerateTunnel(GraphEdge edge, PrefabCache cachedPrefabs, CaveMap cavemap)
    {
        FindPath(edge, cachedPrefabs);
        FindLocalMinimas();
        ThickenTunnel(edge.node1, edge.node2);
        PostProcessTunnel();

        return tunnel;
    }

    // private API
    private void FindPath(GraphEdge edge, PrefabCache cachedPrefabs)
    {
        var start = edge.node1.Normal(CaveUtils.FastMax(5, edge.node1.NodeRadius));
        var target = edge.node2.Normal(CaveUtils.FastMax(5, edge.node2.NodeRadius));

        if (cachedPrefabs.MinSqrDistanceToPrefab(start) == 0)
        {
            Log.Warning($"[Cave] '{edge.Prefab1.Name}' ({start - HalfWorldSize}) intersect with another prefab");
            return;
        }

        if (cachedPrefabs.MinSqrDistanceToPrefab(target) == 0)
        {
            Log.Warning($"[Cave] '{edge.Prefab2.Name}' ({target - HalfWorldSize}) intersect with another prefab");
            return;
        }

        var startNode = new AstarNode(start);
        var goalNode = new AstarNode(target);

        var queue = new HashedPriorityQueue<AstarNode>();
        var visited = new HashSet<int>();

        int bedRockMargin = CaveBuilder.bedRockMargin + 1;
        int terrainMargin = CaveBuilder.terrainMargin + 1;
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
                    || neighborPos.y + terrainMargin > WorldBuilder.Instance.GetHeight(neighborPos.x, neighborPos.z)
                    || visited.Contains(neighborPos.GetHashCode()); // NOTE: AstarNode and Vector3i must have same hashcode function

                if (shouldContinue)
                    continue;

                float minDist = cachedPrefabs.MinSqrDistanceToPrefab(neighborPos);

                if (minDist == 0)
                {
                    continue;
                }

                bool isCave = CaveNoise.pathingNoise.IsCave(neighborPos);
                int factor = 0;

                if (!isCave) factor += 1;
                if (minDist < sqrMinPrefabDistance) factor += 1;

                float tentativeGCost = currentNode.GCost + (neighborDistance << factor);

                AstarNode neighbor = new AstarNode(neighborPos);

                // TODO: try to remove condition 'tentativeGCost < neighbor.GCost'
                // -> it seems to be useless (to be confirmed)
                // -> try with condition 'tentativeGCost < currentNode.GCost' instead
                if (tentativeGCost < neighbor.GCost || !queue.Contains(neighbor))
                {
                    neighbor.Parent = currentNode;
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
        var height1 = WorldBuilder.Instance.GetHeight(start.x, start.z);
        var height2 = WorldBuilder.Instance.GetHeight(target.x, target.z);

        var p1 = start - HalfWorldSize;
        var p2 = target - HalfWorldSize;

        Log.Warning($"No Path found from '{edge.Prefab1.Name}' ({p1} / {height1}) to '{edge.Prefab2.Name}' ({p2} / ({height2})) after {index} iterations ");
    }

    private void FindLocalMinimas()
    {
        for (int i = 1; i < path.Count - 1; i++)
        {
            if (IsLocalMinima(path, i))
            {
                localMinimas.Add(path[i]);
            }
            else if (IsStartOfFlatMinimum(path, i))
            {
                if (IsFlatMinimum(path, ref i))
                {
                    localMinimas.Add(path[i]);
                }
            }
        }
    }

    private void ThickenTunnel(GraphNode start, GraphNode target)
    {
        tunnel.UnionWith(start.GetSphere());
        tunnel.UnionWith(target.GetSphere());

        int r1 = start.NodeRadius;
        int r2 = target.NodeRadius;

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = r1 + (r2 - r1) * ((float)i / path.Count);
            var sphere = GetSphere(path[i], tunnelRadius);

            tunnel.UnionWith(sphere);
        }
    }

    private HashSet<CaveBlock> SmoothTunnel()
    {
        var dictionary = tunnel.ToDictionary(
            block => block.GetHashCode(),
            block => block
        );

        foreach (var block in tunnel.ToList())
        {
            int neighborsCount = 0;
            int totalDensity = 0;

            foreach (var offset in CaveUtils.offsetsNoDiagonal)
            {
                var hash = CaveBlock.GetHashCode(
                    block.x + offset.x,
                    block.y + offset.y,
                    block.z + offset.z
                );

                if (dictionary.ContainsKey(hash))
                {
                    totalDensity += dictionary[hash].density;
                    neighborsCount++;
                }
            }

            if (neighborsCount < 2)
            {
                tunnel.Remove(block);
            }
            else
            {
                block.density = (sbyte)((float)totalDensity / neighborsCount);
            }
        }

        return tunnel;
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

    private void PostProcessTunnel()
    {
        var positions = tunnel.Select(block => block.ToVector3i()).ToHashSet();

        foreach (CaveBlock block in tunnel)
        {
            var position = block.ToVector3i();
            var lower = position + Vector3i.down;
            var upper = position + Vector3i.up;

            if (!positions.Contains(lower) && positions.Contains(upper))
                block.isFloor = true;

            if (!positions.Contains(upper) && positions.Contains(lower))
                block.isCeiling = true;

            block.isFlat = true;

            foreach (var offset in CaveUtils.offsetsHorizontal8)
            {
                if (!positions.Contains(position + offset))
                {
                    block.isFlat = false;
                    break;
                }
            }
        }
    }

    // static API
    public static IEnumerable<CaveBlock> GetSphere(CaveBlock center, float radius)
    {
        if (radius < 2) radius = 2;

        var centerPos = new Vector3i(center.x, center.y, center.z);
        var queue = new HashSet<Vector3i>() { centerPos };
        var sphere = new HashSet<Vector3i>();
        var sqrRadius = radius * radius;
        var index = 100_000;

        Vector3i pos = new Vector3i();

        while (queue.Count > 0 && index-- > 0)
        {
            Vector3i currentPosition = queue.First();

            sphere.Add(currentPosition);
            queue.Remove(currentPosition);

            foreach (var offset in CaveUtils.offsets)
            {
                pos.x = currentPosition.x + offset.x;
                pos.y = currentPosition.y + offset.y;
                pos.z = currentPosition.z + offset.z;

                bool shouldEnqueue =
                    !sphere.Contains(pos)
                    && pos.y > CaveBuilder.bedRockMargin
                    && pos.y + CaveBuilder.terrainMargin < WorldBuilder.Instance.GetHeight(pos.x, pos.z)
                    && CaveUtils.SqrEuclidianDistInt32(pos, centerPos) < sqrRadius;

                if (shouldEnqueue)
                {
                    queue.Add(pos);
                }
            }
        }

        return sphere.Select(position => new CaveBlock(position));
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
