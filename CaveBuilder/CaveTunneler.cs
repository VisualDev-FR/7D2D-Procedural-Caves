using System;
using System.Collections.Generic;
using System.Linq;
using WorldGenerationEngineFinal;

public static class CaveTunneler
{
    private static List<CaveBlock> ReconstructPath(AstarNode currentNode)
    {
        var path = new List<CaveBlock>();

        while (currentNode != null)
        {
            path.Add(new CaveBlock(currentNode.position));
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }

    private static List<CaveBlock> FindPath(Vector3i start, Vector3i target, PrefabCache cachedPrefabs)
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

    private static readonly List<Vector3i> offsets = new List<Vector3i>(){
        new Vector3i(1, 0, 0),
        new Vector3i(0, 1, 0),
        new Vector3i(0, 0, 1),

        new Vector3i(1, 1, 1),
        new Vector3i(1, 1, 0),
        new Vector3i(0, 1, 1),
        new Vector3i(1, 0, 1),
    };

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

                var caveBlock = new CaveBlock(pos)
                {
                    isWater = center.isWater && pos.y < center.y && sqrRadius < 16
                };

                if (sphere.Contains(caveBlock))
                    continue;

                if (pos.y <= CaveBuilder.bedRockMargin)
                    continue;

                if (pos.y + CaveBuilder.terrainMargin >= WorldBuilder.Instance.GetHeight(pos.x, pos.z))
                    continue;

                sphere.Add(new CaveBlock(pos));

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

    private static int GetTunnelRadius_sinusoid(int x, int yA, int yB, int xB)
    {
        const float pi = 3.14159f;
        const float factorPi = 2.5f;
        const float scalePi = 4f;
        const float factor1 = 100f;

        var scale1 = 2;
        var linear = yA + x * (yB - yA) / xB;
        var radius = linear + factor1 * Math.Sin(scale1 * x * pi / xB) + factorPi * Math.Cos(scalePi * x * pi / xB);

        if (radius < 4)
            return 4;

        if (radius > 20)
            return 20;

        return (int)radius;
    }

    private static int GetTunnelRadius_parabolic(int x, int yA, int yB, int xB)
    {
        const float mini = 1f;

        float x1 = 0;
        float x2 = xB;
        float x3 = xB / 2;

        float y1 = yA;
        float y2 = yB;
        float y3 = mini;

        float a = ((y1 - y2) * (x3 - x2) - (y2 - y3) * (x2 - x1)) / ((x1 * x1 - x2 * x2) * (x3 - x2) - (x2 * x2 - x3 * x3) * (x2 - x1));
        float b = (y1 - y2 - a * (x1 * x1 - x2 * x2)) / (x1 - x2);
        float c = y1 - a * x1 * x1 - b * x1;

        float radius = a * x * x + b * x + c;

        if (radius < mini)
            return 4;

        return (int)radius;
    }

    private static HashSet<CaveBlock> ThickenTunnel(List<CaveBlock> path, GraphNode start, GraphNode target)
    {
        var caveMap = path.ToHashSet();

        caveMap.UnionWith(start.GetSphere());
        caveMap.UnionWith(target.GetSphere());

        int r1 = start.Radius;
        int r2 = target.Radius;

        for (int i = 0; i < path.Count; i++)
        {
            var tunnelRadius = (int)(r1 + (r2 - r1) * (1f * i / path.Count));
            var circle = GetSphere(path[i], tunnelRadius);
            caveMap.UnionWith(circle);
        }

        return caveMap;
    }

    private static List<Vector3i> GetVerticalSections(List<Vector3i> path)
    {
        var result = new List<Vector3i>();

        for (int i = 1; i < path.Count - 1; i++)
        {
            var prev = path[i - 1].y;
            var curr = path[i].y;
            var next = path[i + 1].y;

            if (prev < curr && next > curr || prev > curr && next < curr)
            {
                result.Add(path[i]);
                result.Add(path[i - 1]);
                result.Add(path[i + 1]);
            }
        }

        return result;
    }

    private static List<CaveBlock> GetWaterBlocks(List<CaveBlock> path)
    {
        var result = new List<CaveBlock>();
        var queue = new HashSet<CaveBlock>() { path.First() };
        var visited = new HashSet<CaveBlock>();
        var hashPath = path.ToHashSet();
        var index = 0;
        var waterLevel = path.First().y;
        var currentLevel = path.First().y;

        while (queue.Count > 0 && index++ < 100_000)
        {
            var currentNode = queue.First();

            visited.Add(currentNode);
            queue.Remove(currentNode);

            foreach (var position in CaveUtils.GetValidNeighbors(currentNode.position))
            {
                var neighbor = new CaveBlock(position);

                if (visited.Contains(neighbor) || !hashPath.Contains(neighbor))
                    continue;

                if (neighbor.y < currentNode.y)
                    waterLevel -= 1;

                if (neighbor.y > currentNode.y)
                    waterLevel += 1;

                if (waterLevel > currentLevel)
                    currentLevel = waterLevel;

                neighbor.SetWater(waterLevel < currentLevel);

                result.Add(neighbor);

                queue.Add(neighbor);
                visited.Add(neighbor);
            }

        }

        return result;
    }

    public static HashSet<CaveBlock> GenerateTunnel(GraphNode start, GraphNode target, PrefabCache cachedPrefabs)
    {
        var markers1 = start.GetMarkerPoints();
        var markers2 = target.GetMarkerPoints();

        var p1 = start.Normal(CaveUtils.FastMax(5, start.Radius));
        var p2 = target.Normal(CaveUtils.FastMax(5, target.Radius));

        markers1.Remove(p1);
        markers2.Remove(p2);

        var path = FindPath(p1, p2, cachedPrefabs);
        var tunnel = ThickenTunnel(path, start, target);
        // var tunnel = JoinMarkers(path.ToHashSet(), markers1, markers2);

        return tunnel;
    }
}
