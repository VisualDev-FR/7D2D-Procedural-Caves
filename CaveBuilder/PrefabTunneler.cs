using System.Collections.Generic;
using System.Linq;

public class PrefabTunneler
{
    public List<CaveBlock> FindPath(Vector3i start, Vector3i target, CavePrefab prefab)
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

            foreach (var offset in CaveUtils.offsetsNoVertical)
            {
                var pos = currentNode.position + offset;
                var neighbor = new AstarNode(pos);

                if (neighbor.position == goalNode.position)
                {
                    return currentNode.ReconstructPath();
                }

                if (pos != start && !prefab.Intersect3D(pos))
                    continue;

                if (visited.Contains(neighbor))
                    continue;

                bool isCave = CaveBuilder.pathingNoise.IsCave(neighbor.position.x, neighbor.position.y, neighbor.position.z);
                float factor = 1.0f;

                factor *= isCave ? 0.5f : 1f;

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

    public List<CaveBlock> ThickenTunnels(List<CaveBlock> path, CavePrefab prefab)
    {
        var start = prefab.position;
        var end = prefab.position + prefab.Size;
        var tunnel = path.ToHashSet();
        var radius = 1f;

        for (int i = 0; i < path.Count; i++)
        {
            var center = path[i];
            var centerPos = new Vector3i(center.x, center.y, center.z);
            var queue = new HashSet<Vector3i>() { centerPos };
            var sphere = new HashSet<CaveBlock>();
            var sqrRadius = radius * radius;

            while (queue.Count > 0)
            {
                foreach (var pos in queue.ToArray())
                {
                    var caveBlock = new CaveBlock(pos);

                    queue.Remove(pos);

                    if (sphere.Contains(caveBlock))
                        continue;

                    sphere.Add(caveBlock);

                    if (!prefab.Intersect3D(pos))
                        continue;

                    if (CaveUtils.SqrEuclidianDist(pos, centerPos) >= sqrRadius)
                        continue;

                    foreach (var offset in CaveUtils.offsets)
                    {
                        queue.Add(pos + offset);
                    }
                }
            }

            tunnel.UnionWith(sphere);
        }

        return tunnel.ToList();
    }

}