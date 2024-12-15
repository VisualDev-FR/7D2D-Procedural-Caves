using System.Collections.Generic;
using System.Linq;


public class CaveTunnel
{
    public readonly List<CaveBlock> path = new List<CaveBlock>();

    public readonly HashSet<CaveBlock> blocks = new HashSet<CaveBlock>();

    public IEnumerable<CaveBlock> LocalMinimas => FindLocalMinimas();

    private readonly System.Random random;

    private readonly RawHeightMap heightMap;

    private readonly int worldSize;

    public CaveTunnel(GraphEdge edge, CavePrefabManager cachedPrefabs, RawHeightMap heightMap, int worldSize, int seed)
    {
        CaveNoise.pathingNoise.SetSeed(seed);

        this.random = new System.Random(seed);
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

        var startNode = new AstarNode(start);
        var goalNode = new AstarNode(target);

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

            if (currentNode.Equals(goalNode))
            {
                ReconstructPath(currentNode);
                return;
            }

            visited.Add(currentNode.GetHashCode());

            foreach (var neighborPos in GetAstarNeighbors(currentNode.position, target))
            {
                if (
                    neighborPos.y < bedRockMargin
                    || neighborPos.y + terrainMargin > heightMap.GetHeight(neighborPos.x, neighborPos.z)
                    || visited.Contains(neighborPos.GetHashCode()) // NOTE: AstarNode and Vector3i must have same hashcode function
                ) continue;

                float minDist = cachedPrefabs.MinSqrDistanceToPrefab(neighborPos);

                if (minDist == 0)
                {
                    continue;
                }

                AstarNode neighbor = new AstarNode(neighborPos, currentNode);

                int factor = 0;

                if (minDist < sqrMinPrefabDistance) factor += 1;

                float tentativeGCost = currentNode.GCost + (neighborDistance << factor);

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

    private IEnumerable<Vector3i> GetAstarNeighbors(Vector3i position, Vector3i target)
    {
        if (CaveUtils.SqrEuclidianDist(position, target) < 100)
        {
            yield return target;
            yield break;
        }

        var dx = 5;
        var dy = 2;
        var neighbor = Vector3i.zero;

        foreach (var offset in CaveUtils.offsetsHorizontal8)
        {
            neighbor.x = position.x + offset.x * random.Next(-dx, dx + 1);
            neighbor.z = position.z + offset.z * random.Next(-dx, dx + 1);
            neighbor.y = position.y + random.Next(-dy, dy + 1);

            yield return neighbor;
        }
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

    public void ThickenTunnel(GraphNode start, GraphNode target, int seed, CavePrefabManager cachedPrefabs)
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
        var points = new HashSet<Vector3i>();

        while (currentNode != null)
        {
            points.Add(currentNode.position);

            if (currentNode.Parent != null)
                points.UnionWith(BezierCurve3D.Bresenham3D(currentNode.position, currentNode.Parent.position));

            currentNode = currentNode.Parent;
        }

        path.AddRange(points.Select(pos => new CaveBlock(pos)));
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
