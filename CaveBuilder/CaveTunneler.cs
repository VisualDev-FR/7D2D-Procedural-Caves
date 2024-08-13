using System;
using System.Collections.Generic;
using System.Linq;
using WorldGenerationEngineFinal;

public static class CaveTunneler
{
    public static List<CaveBlock> ReconstructPath(AstarNode currentNode)
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

    public static List<CaveBlock> FindPath(Vector3i start, Vector3i target, PrefabCache cachedPrefabs)
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

    public static int GetTunnelRadius_sinusoid(int x, int yA, int yB, int xB)
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

    public static int GetTunnelRadius_parabolic(int x, int yA, int yB, int xB)
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

    public static HashSet<CaveBlock> ThickenTunnel(List<CaveBlock> path, GraphNode start, GraphNode target)
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

    public static List<Vector3i> GetVerticalSections(List<Vector3i> path)
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

    public static HashSet<CaveBlock> FindLocalMinimas(List<CaveBlock> path)
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

    public static bool IsLocalMinima(List<CaveBlock> path, int i)
    {
        return path[i - 1].position.y > path[i].position.y && path[i].position.y < path[i + 1].position.y;
    }

    public static bool IsStartOfFlatMinimum(List<CaveBlock> path, int i)
    {
        return path[i - 1].position.y > path[i].position.y && path[i].position.y == path[i + 1].position.y;
    }

    public static bool IsFlatMinimum(List<CaveBlock> path, ref int i)
    {

        while (i < path.Count - 1 && path[i].position.y == path[i + 1].position.y)
        {
            i++;
        }

        return i < path.Count - 1 && path[i].position.y < path[i + 1].position.y;
    }

    public static CaveBlock GetVerticalLowerPoint(CaveBlock start, CaveMap cavemap)
    {
        var x = start.x;
        var z = start.z;
        var y = start.y;
        var index = 256;

        while (true && --index > 0)
        {
            int hashcode = CaveBlock.GetHashCode(x, y - 1, z);

            if (!cavemap.Contains(hashcode))
            {
                return cavemap.GetBlock(CaveBlock.GetHashCode(x, y, z));
            }

            y--;
        }

        throw new Exception("Lower point not found");
    }

    public static HashSet<int> ExpandWater(CaveBlock waterStart, CaveMap cavemap, PrefabCache cachedPrefabs)
    {
        CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        var waterDeep = 1;
        var queue = new Queue<CaveBlock>();
        var visited = new HashSet<CaveBlock>() { };
        var waterBlocks = new HashSet<int>();
        var startPosition = GetVerticalLowerPoint(waterStart, cavemap);

        for (int i = 0; i < waterDeep; i++)
        {
            var start = cavemap.GetBlock(startPosition.x, startPosition.y + i, startPosition.z);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                CaveBlock block = queue.Dequeue();

                if (cachedPrefabs.IntersectMarker(block))
                {
                    Log.Out("intersect Marker");
                    return new HashSet<int>();
                }

                if (visited.Contains(block) || !cavemap.Contains(block))
                    continue;

                visited.Add(block);
                waterBlocks.Add(block.GetHashCode());

                foreach (var offset in CaveUtils.neighborsOffsets)
                {
                    var hashcode = CaveBlock.GetHashCode(
                        block.x + offset.x,
                        block.y + offset.y,
                        block.z + offset.z
                    );

                    if (!cavemap.TryGetValue(hashcode, out var neighbor))
                        continue;

                    if (!visited.Contains(neighbor) && neighbor.y <= start.y)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return waterBlocks;
    }

    public static void SetTunnelWater(HashSet<CaveBlock> localMinimas, CaveMap cavemap, PrefabCache cachedPrefabs)
    {
        int index = 0;

        // TODO: multi-thread this loop
        foreach (var waterStart in localMinimas)
        {
            HashSet<int> hashcodes = ExpandWater(waterStart, cavemap, cachedPrefabs);

            Log.Out($"Water processing: {100.0f * ++index / localMinimas.Count:F0}% ({index} / {localMinimas.Count}) {hashcodes.Count:N0}");

            foreach (var hashcode in hashcodes)
            {
                cavemap.SetWater(hashcode, true);
            }

            // if(hashcodes.Count > 1000)
            //     break;
        }
    }

    public static HashSet<CaveBlock> GenerateTunnel(GraphNode start, GraphNode target, PrefabCache cachedPrefabs, byte[] densityMap)
    {
        var markers1 = start.GetMarkerPoints();
        var markers2 = target.GetMarkerPoints();

        var p1 = start.Normal(CaveUtils.FastMax(5, start.Radius));
        var p2 = target.Normal(CaveUtils.FastMax(5, target.Radius));

        markers1.Remove(p1);
        markers2.Remove(p2);

        var path = FindPath(p1, p2, cachedPrefabs);
        // return path.ToHashSet();

        if (path.Count == 0)
            return new HashSet<CaveBlock>();

        var tunnel = ThickenTunnel(path, start, target);

        return tunnel;
    }
}
