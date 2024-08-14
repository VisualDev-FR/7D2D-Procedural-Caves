# pragma warning disable CS0436

using System.Collections.Generic;
using System.Linq;
using WorldGenerationEngineFinal;

public class CaveTunneler
{
    public List<CaveBlock> path;

    public HashSet<CaveBlock> localMinimas;

    public HashSet<CaveBlock> tunnel;

    public HashSet<CaveBlock> GenerateTunnel(Edge edge, PrefabCache cachedPrefabs)
    {
        var start = edge.node1;
        var target = edge.node2;

        var p1 = start.Normal(CaveUtils.FastMax(5, start.NodeRadius));
        var p2 = target.Normal(CaveUtils.FastMax(5, target.NodeRadius));

        path = FindPath(p1, p2, cachedPrefabs);

        if (path.Count == 0)
            return new HashSet<CaveBlock>();

        localMinimas = FindLocalMinimas();
        tunnel = ThickenTunnel(start, target);

        return tunnel;
    }

    public static HashSet<CaveBlock> GetSphere(CaveBlock center, float radius)
    {
        var queue = new HashSet<Vector3i>() { center.position };
        var sphere = new HashSet<CaveBlock>();
        var sqrRadius = radius * radius;

        while (queue.Count > 0)
        {
            foreach (var pos in queue.ToArray())
            {
                queue.Remove(pos);

                var caveBlock = new CaveBlock(pos);

                if (sphere.Contains(caveBlock))
                    continue;

                if (pos.y <= CaveBuilder.bedRockMargin)
                    continue;

                if (pos.y + CaveBuilder.terrainMargin >= WorldBuilder.Instance.GetHeight(pos.x, pos.z))
                    continue;

                sphere.Add(caveBlock);

                if (CaveUtils.SqrEuclidianDist(pos, center.position) >= sqrRadius)
                    continue;

                foreach (var offset in CaveUtils.neighborsOffsets)
                {
                    queue.Add(pos + offset);
                }
            }
        }

        return sphere;
    }

    private List<CaveBlock> ReconstructPath(AstarNode currentNode)
    {
        path = new List<CaveBlock>();

        while (currentNode != null)
        {
            path.Add(new CaveBlock(currentNode.position));
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }

    private List<CaveBlock> FindPath(Vector3i start, Vector3i target, PrefabCache cachedPrefabs)
    {
        var startNode = new AstarNode(start);
        var goalNode = new AstarNode(target);

        var queue = new HashedPriorityQueue<AstarNode>();
        var visited = new HashSet<AstarNode>();
        var index = 0;

        queue.Enqueue(startNode, float.MaxValue);

        while (queue.Count > 0 && index++ < 100_000)
        {
            AstarNode currentNode = queue.Dequeue();

            visited.Add(currentNode);

            foreach (AstarNode neighbor in currentNode.GetNeighbors())
            {
                if (neighbor.position == goalNode.position)
                {
                    neighbor.Parent = currentNode;
                    return ReconstructPath(neighbor);
                }

                if (neighbor.position.y < CaveBuilder.bedRockMargin + 1)
                    continue;

                if (neighbor.position.y + CaveBuilder.terrainMargin + 1 > WorldBuilder.Instance.GetHeight(neighbor.position.x, neighbor.position.z))
                    continue;

                if (visited.Contains(neighbor))
                    continue;

                float minDist = cachedPrefabs.MinDistToPrefab(neighbor.position);

                if (minDist == 0)
                    continue;

                bool isCave = CaveBuilder.pathingNoise.IsCave(neighbor.position.x, neighbor.position.y, neighbor.position.z);
                float factor = 1.0f;

                factor *= isCave ? 0.5f : 1f;
                factor *= minDist < 100 ? 1 : .5f;

                float tentativeGCost = currentNode.GCost + CaveUtils.SqrEuclidianDist(currentNode, neighbor) * factor;

                bool isInQueue = queue.Contains(neighbor);

                if (!isInQueue || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CaveUtils.SqrEuclidianDist(neighbor, goalNode) * factor;

                    if (!isInQueue)
                        queue.Enqueue(neighbor, neighbor.FCost);
                }
            }
        }

        Log.Warning($"No Path found from {start} to {target} after {index} iterations");

        return new List<CaveBlock>();
    }

    private HashSet<CaveBlock> ThickenTunnel(GraphNode start, GraphNode target)
    {
        tunnel = path.ToHashSet();

        tunnel.UnionWith(start.GetSphere());
        tunnel.UnionWith(target.GetSphere());

        int r1 = start.NodeRadius;
        int r2 = target.NodeRadius;

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = (int)(r1 + (r2 - r1) * (1f * i / path.Count));
            var circle = GetSphere(path[i], tunnelRadius);
            tunnel.UnionWith(circle);
        }

        return tunnel;
    }

    private HashSet<CaveBlock> FindLocalMinimas()
    {
        var localMinimas = new HashSet<CaveBlock>();

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

        return localMinimas;
    }

    private static bool IsLocalMinima(List<CaveBlock> path, int i)
    {
        return path[i - 1].position.y > path[i].position.y && path[i].position.y < path[i + 1].position.y;
    }

    private static bool IsStartOfFlatMinimum(List<CaveBlock> path, int i)
    {
        return path[i - 1].position.y > path[i].position.y && path[i].position.y == path[i + 1].position.y;
    }

    private static bool IsFlatMinimum(List<CaveBlock> path, ref int i)
    {

        while (i < path.Count - 1 && path[i].position.y == path[i + 1].position.y)
        {
            i++;
        }

        return i < path.Count - 1 && path[i].position.y < path[i + 1].position.y;
    }

}
