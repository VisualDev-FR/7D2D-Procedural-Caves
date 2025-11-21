using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CaveTunnel
{
    public readonly List<CaveBlock> path = new List<CaveBlock>();

    public readonly HashSet<CaveBlock> blocks = new HashSet<CaveBlock>();

    public IEnumerable<CaveBlock> LocalMinimas => FindLocalMinimas();

    private readonly System.Random random;

    private readonly RawHeightMap heightMap;

    public CaveTunnel() { }

    public CaveTunnel(GraphEdge edge, CavePrefabManager cachedPrefabs, RawHeightMap heightMap, int worldSize, int seed)
    {
        CaveNoise.pathingNoise.SetSeed(seed);

        this.random = new System.Random(seed);
        this.heightMap = heightMap;

        FindPath(edge, cachedPrefabs);
        ThickenTunnel(edge.node1, edge.node2, seed, cachedPrefabs);
    }

    public void FindPath(GraphEdge edge, CavePrefabManager cachedPrefabs)
    {
        var start = edge.node1.Normal(edge.node1.NodeRadius);
        var target = edge.node2.Normal(edge.node2.NodeRadius);

        var startNode = new AstarNode(start);
        var goalNode = new AstarNode(target);

        var midPoint = FindMidPoint(start, target, cachedPrefabs);

        FindPath(startNode, midPoint, cachedPrefabs);
        FindPath(goalNode, midPoint, cachedPrefabs);
    }

    public void FindPath(AstarNode startNode, Vector3i target, CavePrefabManager cachedPrefabs)
    {
        var queue = new HashSet<AstarNode>();
        var visited = new HashSet<int>();
        var targetHash = target.GetHashCode();
        int index = 0;

        queue.Add(startNode);

        while (queue.Count > 0 && index++ < 10_000)
        {
            AstarNode currentNode = queue.First();

            queue.Remove(currentNode);

            if (currentNode.hashcode == targetHash)
            {
                ReconstructPath(currentNode);
                return;
            }

            visited.Add(currentNode.GetHashCode());

            foreach (var neighborPos in AstarNeighbors(currentNode, target))
            {
                if (neighborPos.y <= CaveConfig.bedRockMargin || neighborPos.y > 200 || cachedPrefabs.MinSqrDistanceToPrefab(neighborPos) < 36)
                    continue;

                queue.Add(new AstarNode(neighborPos, currentNode));
                break;
            }
        }

        Logging.Warning($"No Path found from {startNode.position} to {target}");
    }

    public Vector3i FindMidPoint(Vector3i p1, Vector3i p2, CavePrefabManager cachedPrefabs)
    {
        var dx = 10;

        for (int i = 0; i < 100; i++)
        {
            var midPoint = new Vector3i(
                p1.x + ((p2.x - p1.x) >> 1) + random.Next(-dx, dx + 1),
                p1.y + ((p2.y - p1.y) >> 1) + random.Next(-dx, dx + 1),
                p1.z + ((p2.z - p1.z) >> 1) + random.Next(-dx, dx + 1)
            );

            if (cachedPrefabs.MinSqrDistanceToPrefab(midPoint) > 100)
            {
                return midPoint;
            }
        }

        return Vector3i.zero;
    }

    private IEnumerable<Vector3i> AstarNeighbors(AstarNode node, Vector3i target)
    {
        var dist = FastMath.SqrEuclidianDist(node.position, target);

        if (dist < 100)
        {
            yield return target;
            yield break;
        }

        Vector3 direction = node.Parent is null ? node.direction : (target - node.position).ToVector3().normalized;
        Vector3 result;

        var dx = 10;
        var dy = 2;

        for (int i = 0; i < 100; i++)
        {
            Vector3 pointOnLine = node.position + random.Next(2, 10) * direction;

            result.x = pointOnLine.x + random.Next(-dx, dx + 1);
            result.z = pointOnLine.z + random.Next(-dx, dx + 1);
            result.y = pointOnLine.y + random.Next(-dy, dy + 1);

            yield return new Vector3i(result);
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

        blocks.UnionWith(start.GetSphere());
        blocks.UnionWith(target.GetSphere());

        int r1 = start.NodeRadius;
        int r2 = target.NodeRadius;

        CaveUtils.Assert(r1 > 0, "start radius should be greater than 0");
        CaveUtils.Assert(r2 > 0, "target radius should be greater than 0");

        var random = new System.Random(seed);
        var noise = new Noise1D(random, r1, r2, path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = noise.InterpolateClamped(i, CaveConfig.minTunnelRadius + 1, CaveConfig.maxTunnelRadius);
            var sphere = SphereManager.GetSphere(path[i].ToVector3i(), tunnelRadius);

            blocks.UnionWith(sphere);
        }

        blocks.RemoveWhere(caveBlock =>
                caveBlock.y <= CaveConfig.bedRockMargin
            // TODO: check why the height map removes all blocks
            // || (caveBlock.y + CaveConfig.terrainMargin) >= (int)heightMap.GetHeight(caveBlock.x, caveBlock.z)
            || cachedPrefabs.IntersectWithPrefab(caveBlock.ToVector3i()));
    }

    public static IEnumerable<CaveBlock> CreateNaturalEntrance(Vector3i position, RawHeightMap heightMap)
    {
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
