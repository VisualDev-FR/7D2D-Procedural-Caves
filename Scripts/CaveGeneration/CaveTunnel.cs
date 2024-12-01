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
        ThickenTunnel(edge.node1, edge.node2, seed, cachedPrefabs);
    }

    private void FindPath(GraphEdge edge, CavePrefabManager cachedPrefabs)
    {
        var HalfWorldSize = CaveUtils.HalfWorldSize(worldSize);

        var start = edge.node1.Normal(Utils.FastMax(5, edge.node1.NodeRadius));
        var target = edge.node2.Normal(Utils.FastMax(5, edge.node2.NodeRadius));

        var yMin = Utils.FastMin(start.y, target.y);
        var yMax = Utils.FastMax(start.y, target.y);

        if (cachedPrefabs.MinSqrDistanceToPrefab(start) == 0)
        {
            Logging.Warning($"'{edge.Prefab1.PrefabName}' ({start - HalfWorldSize}) intersect with another prefab");
            return;
        }

        if (cachedPrefabs.MinSqrDistanceToPrefab(target) == 0)
        {
            Logging.Warning($"'{edge.Prefab2.PrefabName}' ({target - HalfWorldSize}) intersect with another prefab");
            return;
        }

        var startNode = new AstarNode(start, edge.node1.position);
        var goalNode = new AstarNode(target, edge.node2.position);

        var queue = new HashedPriorityQueue<AstarNode>();
        var visited = new HashSet<int>();

        int bedRockMargin = CaveConfig.bedRockMargin + 1;
        int terrainMargin = CaveConfig.terrainMargin + 1;
        int sqrMinPrefabDistance = 25;
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

        Logging.Warning($"No Path found from '{edge.Prefab1.PrefabName}' ({p1} / {height1}) to '{edge.Prefab2.PrefabName}' ({p2} / ({height2})) after {index} iterations ");
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

    private void ThickenTunnel(GraphNode start, GraphNode target, int seed, CavePrefabManager cachedPrefabs)
    {
        // TODO: handle duplicates with that instead of hashset: https://stackoverflow.com/questions/1672412/filtering-duplicates-out-of-an-ienumerable

        if (!start.prefab.isNaturalEntrance)
        {
            blocks.UnionWith(start.GetSphere());
        }

        if (!target.prefab.isNaturalEntrance)
        {
            blocks.UnionWith(target.GetSphere());
        }

        int r1 = start.NodeRadius;
        int r2 = target.NodeRadius;

        CaveUtils.Assert(r1 > 0, "start radius should be greater than 0");
        CaveUtils.Assert(r2 > 0, "target radius should be greater than 0");

        var random = new System.Random(seed);
        var noise = new Noise1D(random, r1, r2, path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = noise.Interpolate(i);
            var sphere = SphereManager.GetSphere(path[i].ToVector3i(), tunnelRadius);

            blocks.UnionWith(sphere);
        }

        blocks.RemoveWhere(caveBlock =>
                caveBlock.y <= CaveConfig.bedRockMargin
            || (caveBlock.y + CaveConfig.terrainMargin) >= (int)heightMap.GetHeight(caveBlock.x, caveBlock.z)
            || cachedPrefabs.IntersectWithPrefab(caveBlock.ToVector3i()));
    }

    public static IEnumerable<CaveBlock> CreateNaturalEntrance(GraphNode node, RawHeightMap heightMap)
    {
        var position = node.Normal(0);
        var entranceTunnel = new HashSet<CaveBlock>();


        while (position.y < heightMap.GetHeight(position.x, position.z))
        {
            position.y += 1;
            entranceTunnel.UnionWith(SphereManager.GetSphere(position, 2));
        }

        foreach (var block in entranceTunnel.Where(block => block.y <= heightMap.GetHeight(block.x, block.z)))
        {
            block.skipDecoration = true;
            yield return block;
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

}
