#pragma warning disable CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using Random = System.Random;
using Debug = System.Diagnostics.Debug;



public static class Logger
{
    private static void Logging(string level, string message)
    {
        Console.WriteLine($"{level,-10} {message}");
    }

    public static void Blank()
    {
        Console.WriteLine("");
    }

    public static void Debug(string message)
    {
        Logging("DEBUG", message);
    }

    public static void Info(string message)
    {
        Logging("INFO", message);
    }

    public static void Warning(string message)
    {
        Logging("WARNING", message);
    }

    public static void Error(string message)
    {
        Logging("ERROR", message);
    }

    public static void Test()
    {
        Debug("coucou");
        Info("coucou");
        Warning("coucou");
        Error("coucou");
    }
}


public static class CaveUtils
{
    public static int FastMin(int a, int b)
    {
        return a > b ? a : b;
    }

    public static int FastMax(int a, int b)
    {
        return a <= b ? a : b;
    }

    public static string TimeFormat(Stopwatch timer, string format = @"hh\:mm\:ss")
    {
        return TimeSpan.FromSeconds(timer.ElapsedMilliseconds / 1000).ToString(format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Node nodeA, Node nodeB)
    {
        return SqrEuclidianDist(nodeA.position, nodeB.position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Vector3i p1, Vector3i p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.z - p2.z;

        return dx * dx + dy * dy;
    }

}


public class PrefabWrapper
{
    public const int OVERLAP_MARGIN = 50;

    public Vector3i position;

    public Vector3i size;

    public List<Vector3i> nodes;

    public int rotation = 1;

    public List<Vector3i> innerPoints;

    public PrefabWrapper()
    {
        nodes = new List<Vector3i>();
    }

    public PrefabWrapper(Random rand)
    {
        nodes = new List<Vector3i>();
        size = new Vector3i(
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            1,
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE)
        );
    }

    public void UpdateNodes(Random rand = null)
    {
        if (rand == null)
        {
            nodes = new List<Vector3i>()
            {
                position + new Vector3i(size.x / 2 , 0, 0),
                position + new Vector3i(0, 0, size.z / 2),
                position + new Vector3i(size.x / 2, 0, size.z),
                position + new Vector3i(size.x , 0, size.z / 2),
            };
        }
        else
        {
            nodes = new List<Vector3i>()
            {
                position + new Vector3i(rand.Next(size.x) , 0, 0),
                position + new Vector3i(0, 0, rand.Next(size.z)),
                position + new Vector3i(rand.Next(size.x), 0, size.z),
                position + new Vector3i(size.x , 0, rand.Next(size.z)),
            };
        }
    }

    public void UpdateInnerPoints()
    {
        innerPoints = new List<Vector3i>();

        for (int x = position.x; x <= (position.x + size.x); x++)
        {
            for (int z = position.z; z <= (position.z + size.z); z++)
            {
                innerPoints.Add(new Vector3i(x, position.y, z));
            }
        }
    }

    public void SetRandomPosition(Random rand, int mapSize, int mapOffset)
    {
        position = new Vector3i(
            rand.Next(mapOffset, mapSize - mapOffset - size.x),
            CaveBuilder.PREFAB_Y,
            rand.Next(mapOffset, mapSize - mapOffset - size.z)
        );

        UpdateNodes(rand);
        UpdateInnerPoints();
    }

    public bool OverLaps2D(PrefabWrapper other, int map_size, int map_offset)
    {
        if (position.x + size.x + OVERLAP_MARGIN < other.position.x || other.position.x + other.size.x + OVERLAP_MARGIN < position.x)
            return false;

        if (position.z + size.z + OVERLAP_MARGIN < other.position.z || other.position.z + other.size.z + OVERLAP_MARGIN < position.z)
            return false;

        return true;
    }

    public bool OverLaps2D(List<PrefabWrapper> others, int map_size, int map_offset)
    {
        foreach (var prefab in others)
        {
            if (OverLaps2D(prefab, map_size, map_offset))
                return true;
        }

        return false;
    }

    public List<Vector3i> GetEdges()
    {
        List<Vector3i> points = new List<Vector3i>();

        int x0 = position.x;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int z1 = z0 + size.z;

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z0));
        }

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z1));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x0, position.y, z));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x1, position.y, z));
        }

        return points;
    }

    public HashSet<Vector3i> GetNoiseAround(Random rand)
    {
        const int MIN_RADIUS = 1;
        const int MAX_RADIUS = 10;

        var noiseMap = new HashSet<Vector3i>();

        FastNoiseLite perlinNoise = CaveBuilder.ParsePerlinNoise(rand.Next());

        foreach (var pos in GetEdges())
        {
            if (rand.NextDouble() > 0.2f)
                continue;

            float noise = 0.5f * (1 + perlinNoise.GetNoise(pos.x, pos.z));
            float dist = MIN_RADIUS + noise * (MAX_RADIUS - MIN_RADIUS);

            noiseMap.UnionWith(CaveBuilder.ParseCircle(pos, dist));
        }

        noiseMap.ExceptWith(innerPoints);

        return noiseMap;
    }

    public Vector3i GetCenter()
    {
        return new Vector3i(
            position.x + size.x / 2,
            position.y,
            position.z + size.z / 2
        );
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var other = (Vector3i)obj;

        return GetHashCode() == other.GetHashCode();
    }
}


public class Edge : IComparable<Edge>
{
    public int nodeIndex1;

    public int nodeIndex2;

    public int prefabIndex1;

    public int prefabIndex2;

    public float Weight;

    public Vector3i node1;

    public Vector3i node2;

    public string HashPrefabs()
    {
        int index1 = CaveUtils.FastMin(prefabIndex1, prefabIndex2);
        int index2 = CaveUtils.FastMax(prefabIndex1, prefabIndex2);

        return $"{index1};{index2}";
    }

    private float GetWeight()
    {
        return CaveUtils.SqrEuclidianDist(node1, node2);
    }

    public Edge(int index1, int index2, int prefab1, int prefab2, Vector3i _startPoint, Vector3i _endPoint)
    {
        nodeIndex1 = index1;
        nodeIndex2 = index2;
        prefabIndex1 = prefab1;
        prefabIndex2 = prefab2;
        node1 = _startPoint;
        node2 = _endPoint;
        Weight = GetWeight();
    }

    public Edge(int index1, int index2, Vector3i _startPoint, Vector3i _endPoint)
    {
        prefabIndex1 = index1;
        prefabIndex2 = index2;
        node1 = _startPoint;
        node2 = _endPoint;
        Weight = GetWeight();
    }

    public Edge(Vector3i node1, Vector3i node2)
    {
        this.node1 = node1;
        this.node2 = node2;
        Weight = GetWeight();
    }

    public int CompareTo(Edge other)
    {
        return Weight.CompareTo(other.Weight);
    }

    public override int GetHashCode()
    {
        int hash1 = node1.GetHashCode();
        int hash2 = node2.GetHashCode();

        int hash = 17;
        hash = hash * 23 + hash1 + hash2;
        hash = hash * 23 + hash1 + hash2;

        return hash;
    }
}


public class Node
{
    public Vector3i position;

    public float GCost { get; set; } // coût du chemin depuis le noeud de départ jusqu'à ce noeud

    public float HCost { get; set; } // estimation heuristique du coût restant jusqu'à l'objectif

    public float FCost => GCost + HCost; // coût total estimé (GCost + HCost)

    public Node Parent { get; set; } // noeud parent dans le chemin optimal

    public Node(Vector3i pos)
    {
        position = pos;
    }

    public Node(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
    }

    public List<Node> GetNeighbors()
    {
        var neighborsPos = new List<Vector3i>(){
            new Vector3i(position.x + 1, position.y, position.z + 1),
            new Vector3i(position.x - 1, position.y, position.z + 1),
            new Vector3i(position.x + 1, position.y, position.z - 1),
            new Vector3i(position.x - 1, position.y, position.z - 1),

            new Vector3i(position.x, position.y, position.z + 1),
            new Vector3i(position.x, position.y, position.z - 1),
            new Vector3i(position.x + 1, position.y, position.z),
            new Vector3i(position.x - 1, position.y, position.z),
        };

        var neighbors = new List<Node>();

        foreach (var position in neighborsPos)
        {
            if (position.x < 0 || position.x >= CaveBuilder.MAP_SIZE)
                continue;

            if (position.z < 0 || position.z >= CaveBuilder.MAP_SIZE)
                continue;

            neighbors.Add(new Node(position));
        }

        return neighbors;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }


    public override bool Equals(object obj)
    {
        Node other = (Node)obj;
        return position.GetHashCode() == other.position.GetHashCode();
    }
}


public static class CaveTunneler
{
    private static FastNoiseLite pathingNoise;

    private static FastNoiseLite thickingNoise;

    private static Node GetLowestFCostNode(HashSet<Node> nodes)
    {
        Node lowestCostNode = null;
        foreach (Node node in nodes)
        {
            if (lowestCostNode == null || node.FCost < lowestCostNode.FCost)
            {
                lowestCostNode = node;
            }
        }
        return lowestCostNode;
    }

    private static HashSet<Vector3i> ReconstructPath(Node currentNode)
    {
        var path = new HashSet<Vector3i>();

        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.Parent;
        }

        return path;
    }

    private static HashSet<Vector3i> FindPath(Vector3i startPos, Vector3i targetPos, HashSet<Vector3i> obstacles, HashSet<Vector3i> noiseMap, FastNoiseLite perlinNoise)
    {
        var startNode = new Node(startPos);
        var goalNode = new Node(targetPos);

        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        obstacles.Remove(startPos);
        obstacles.Remove(targetPos);

        openSet.Add(startNode);

        var path = new HashSet<Vector3i>();

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openSet);

            if (currentNode.position == goalNode.position)
            {
                path = ReconstructPath(currentNode);
                break;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbor in currentNode.GetNeighbors())
            {
                // Logger.Debug($"{currentNode.position} {neighbor.position} ({obstacles.Contains(neighbor.position)})");

                if (closedSet.Contains(neighbor))
                    continue;

                if (obstacles.Contains(neighbor.position))
                {
                    continue;
                }

                float noise = 0.5f * (1 + perlinNoise.GetNoise(neighbor.position.x, neighbor.position.z));
                float factor = noise < CaveBuilder.NOISE_THRESHOLD ? .5f : 1f;

                factor *= noiseMap.Contains(neighbor.position) ? 1f : .5f;

                float tentativeGCost = currentNode.GCost + CaveUtils.SqrEuclidianDist(currentNode, neighbor) * factor;

                if (!openSet.Contains(neighbor) || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CaveUtils.SqrEuclidianDist(neighbor, goalNode) * factor;

                    openSet.Add(neighbor);
                }
            }
        }

        if (path.Count == 0)
        {
            Logger.Warning($"No Path found from {startPos} to {targetPos}.");
        }

        return path;
    }

    public static HashSet<Vector3i> ThickenCaveMap(HashSet<Vector3i> wiredCaveMap, HashSet<Vector3i> obstacles)
    {
        var caveMap = new HashSet<Vector3i>();

        foreach (var position in wiredCaveMap)
        {
            var circle = CaveBuilder.ParseCircle(position, 2f);

            caveMap.UnionWith(circle);
        }

        caveMap.ExceptWith(obstacles);

        return caveMap;
    }

    public static HashSet<Vector3i> PerlinRoute(Vector3i startPos, Vector3i targetpos, FastNoiseLite noise, HashSet<Vector3i> obstacles, HashSet<Vector3i> noiseMap)
    {
        pathingNoise = CaveBuilder.ParsePerlinNoise();
        thickingNoise = CaveBuilder.ParsePerlinNoise();

        HashSet<Vector3i> caveMap = FindPath(startPos, targetpos, obstacles, noiseMap, noise);

        return caveMap;
    }
}


public static class GraphSolver
{
    private static List<Edge> BuildPrefabGraph(List<PrefabWrapper> prefabs)
    {
        var prefabEdges = new Dictionary<int, List<Edge>>();

        for (int i = 0; i < prefabs.Count; i++)
        {
            for (int j = i + 1; j < prefabs.Count; j++)
            {
                Vector3i p1 = prefabs[i].GetCenter();
                Vector3i p2 = prefabs[j].GetCenter();

                var edge = new Edge(i, j, p1, p2);

                if (!prefabEdges.ContainsKey(i))
                    prefabEdges.Add(i, new List<Edge>());

                if (!prefabEdges.ContainsKey(j))
                    prefabEdges.Add(j, new List<Edge>());

                prefabEdges[i].Add(edge);
                prefabEdges[j].Add(edge);
            }
        }

        Logger.Debug($"node count = {prefabEdges.Count * 4}");

        var graph = new HashSet<Edge>();

        for (int i = 0; i < prefabs.Count; i++)
        {
            var relatedPrefabEdges = prefabEdges[i];
            var nodes = prefabs[i].nodes.ToHashSet();

            Debug.Assert(relatedPrefabEdges.Count == prefabs.Count - 1);

            relatedPrefabEdges.Sort();

            var connectedNodes = new HashSet<Vector3i>();

            for (int j = 0; j < relatedPrefabEdges.Count; j++)
            {
                if (nodes.Count == 0)
                    break;

                var relatedEdge = relatedPrefabEdges[j];
                var edges = new List<Edge>();

                var nodes1 = prefabs[relatedEdge.prefabIndex1].nodes;
                var nodes2 = prefabs[relatedEdge.prefabIndex2].nodes;

                foreach (var node1 in nodes1)
                {
                    foreach (var node2 in nodes2)
                    {
                        var edge = new Edge(node1, node2);

                        edges.Add(edge);
                    }
                }

                edges.Sort();

                var edgeNode1 = edges[0].node1;
                var edgeNode2 = edges[0].node2;

                if (connectedNodes.Contains(edgeNode1) || connectedNodes.Contains(edgeNode2))
                    continue;

                connectedNodes.Add(edgeNode1);
                connectedNodes.Add(edgeNode2);

                nodes.Remove(edgeNode1);
                nodes.Remove(edgeNode2);

                graph.Add(edges[0]);
                continue;
            }

            // break;
        }

        return graph.ToList();
    }

    public static List<Edge> Resolve(List<PrefabWrapper> prefabs)
    {
        var timer = new Stopwatch();

        timer.Start();

        List<Edge> graph = BuildPrefabGraph(prefabs);

        Logger.Info($"Graph resolved in {CaveUtils.TimeFormat(timer)}");

        return graph;
    }
}


public static class CaveBuilder
{
    public static int SEED = 12345; // new Random().Next();

    public const int MAP_SIZE = 6144;

    public const int PREFAB_Y = 5;

    public const int MIN_PREFAB_SIZE = 8;

    public const int MAX_PREFAB_SIZE = 100;

    public const int MAP_OFFSET = MAP_SIZE / 60;

    public const float POINT_WIDTH = 5;

    public const int PREFAB_COUNT = MAP_SIZE / 5;

    public const float NOISE_THRESHOLD = 0.5f;

    public static Random rand = new Random(SEED);

    public static HashSet<Vector3i> ParseCircle(Vector3i center, float radius)
    {
        var queue = new HashSet<Vector3i>() { center };
        var visited = new HashSet<Vector3i>();

        while (queue.Count > 0)
        {
            foreach (var pos in queue.ToArray())
            {
                queue.Remove(pos);

                if (visited.Contains(pos))
                    continue;

                visited.Add(pos);

                if (CaveUtils.SqrEuclidianDist(pos, center) >= radius)
                    continue;

                queue.Add(new Vector3i(pos.x + 1, pos.y, pos.z));
                queue.Add(new Vector3i(pos.x - 1, pos.y, pos.z));
                queue.Add(new Vector3i(pos.x, pos.y, pos.z + 1));
                queue.Add(new Vector3i(pos.x, pos.y, pos.z - 1));
            }
        }

        return visited;
    }

    public static FastNoiseLite ParsePerlinNoise(int seed = -1)
    {
        var noise = new FastNoiseLite(seed == -1 ? SEED : seed);

        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFractalType(FastNoiseLite.FractalType.None);
        noise.SetFractalGain(1);
        noise.SetFractalOctaves(1);
        noise.SetFrequency(0.1f);

        return noise;
    }

    public static bool CheckPrefabOverlaps(PrefabWrapper prefab, List<PrefabWrapper> others)
    {
        int i;

        for (i = 0; i < 100; i++)
        {
            if (prefab.OverLaps2D(others, MAP_SIZE, MAP_OFFSET))
            {
                prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);
            }
            else
            {
                break;
            }
        }

        // Console.WriteLine($"{i + 1} iterations done.");

        return !prefab.OverLaps2D(others, MAP_SIZE, MAP_OFFSET);
    }

    public static List<PrefabWrapper> GetRandomPrefabs(int count)
    {
        Logger.Info("Start POIs placement...");

        var prefabs = new List<PrefabWrapper>();

        for (int i = 0; i < count; i++)
        {
            var prefab = new PrefabWrapper(rand);

            prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

            if (CheckPrefabOverlaps(prefab, prefabs))
                prefabs.Add(prefab);
        }

        Logger.Info($"{prefabs.Count} / {PREFAB_COUNT} prefabs added");

        return prefabs;
    }

    public static HashSet<Vector3i> CollectPrefabObstacles(List<PrefabWrapper> prefabs)
    {
        var obstacles = new HashSet<Vector3i>();

        foreach (PrefabWrapper prefab in prefabs)
        {
            obstacles.UnionWith(prefab.innerPoints);
        }

        return obstacles;
    }

    public static HashSet<Vector3i> CollectPrefabNoise(List<PrefabWrapper> prefabs, FastNoiseLite noise)
    {
        var noiseMap = new HashSet<Vector3i>();

        foreach (PrefabWrapper prefab in prefabs)
        {
            noiseMap.UnionWith(prefab.GetNoiseAround(rand));
        }

        return noiseMap;
    }

}
