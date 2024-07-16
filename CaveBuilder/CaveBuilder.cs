#pragma warning disable CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using Random = System.Random;
using Debug = System.Diagnostics.Debug;
using System.ComponentModel;


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
}


public static class CaveUtils
{
    public static int FastMin(int a, int b)
    {
        return a < b ? a : b;
    }

    public static int FastMax(int a, int b)
    {
        return a > b ? a : b;
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
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return dx * dx + dy * dy + dz * dz;
    }

}


public class PrefabWrapper
{
    public PrefabDataInstance prefabDataInstance;

    public Vector3i position;

    public Vector3i size;

    public byte rotation;

    public List<Vector3i> nodes;

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
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE)
        );
    }

    public PrefabWrapper(PrefabDataInstance prefab)
    {
        prefabDataInstance = prefab;
        position = prefab.boundingBoxPosition;
        size = prefab.boundingBoxSize;
        rotation = prefab.rotation;
    }

    public PrefabWrapper(int id, PrefabData prefabData)
    {
        position = new Vector3i();
        rotation = 0;
        prefabDataInstance = new PrefabDataInstance(id, position, rotation, prefabData);
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
            for (int y = position.y; y <= (position.y + size.z); y++)
            {
                for (int z = position.z; z <= (position.z + size.z); z++)
                {
                    innerPoints.Add(new Vector3i(x, y, z));
                }
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

    public bool OverLaps2D(PrefabWrapper other)
    {
        int overlapMargin = CavePlanner.overLapMargin;

        if (position.x + size.x + overlapMargin < other.position.x || other.position.x + other.size.x + overlapMargin < position.x)
            return false;

        if (position.z + size.z + overlapMargin < other.position.z || other.position.z + other.size.z + overlapMargin < position.z)
            return false;

        return true;
    }

    public bool OverLaps2D(List<PrefabWrapper> others)
    {
        foreach (var prefab in others)
        {
            if (OverLaps2D(prefab))
                return true;
        }

        return false;
    }

    public List<Edge> GetFaces()
    {
        // a face is described his diagonal in one xyz plane.

        int x0 = position.x;
        int y0 = position.y;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int y1 = y0 + size.z;
        int z1 = z0 + size.z;

        var p000 = new Vector3i(x0, y0, z0);
        var p001 = new Vector3i(x0, y0, z1);
        var p100 = new Vector3i(x1, y0, z0);
        var p101 = new Vector3i(x1, y0, z1);
        var p010 = new Vector3i(x0, y1, z0);
        var p011 = new Vector3i(x0, y1, z1);
        var p110 = new Vector3i(x1, y1, z0);
        var p111 = new Vector3i(x1, y1, z1);

        var faces = new List<Edge>(){
            new Edge(p000, p011),
            new Edge(p000, p101),
            new Edge(p000, p110),
            new Edge(p111, p100),
            new Edge(p111, p010),
            new Edge(p111, p001),
        };

        return faces;
    }

    public HashSet<Vector3i> GetNoiseAround(Random rand)
    {
        int maxNoiseSize = 10;
        var perlinNoise = CaveBuilder.ParsePerlinNoise(rand.Next());
        var noiseMap = new HashSet<Vector3i>();

        Logger.Debug($"size = {size}");

        foreach (Edge diagonal in GetFaces())
        {
            Vector3i p1 = diagonal.node1;
            Vector3i p2 = diagonal.node2;

            int normalDir = p1 == position ? -1 : 1;

            if (p1.x == p2.x)
            {
                Logger.Debug($"normalX [{p1}] [{p2}] {normalDir}");

                int yMin = CaveUtils.FastMin(p1.y, p2.y);
                int yMax = CaveUtils.FastMax(p1.y, p2.y);

                for (int y = yMin; y < yMax; y++)
                {
                    int zMin = CaveUtils.FastMin(p1.z, p2.z);
                    int zMax = CaveUtils.FastMax(p1.z, p2.z);

                    for (int z = zMin; z < zMax; z++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(p1.x, y, z));

                        int xMin = CaveUtils.FastMin(p1.x, p1.x + (int)(maxNoiseSize * noise));
                        int xMax = CaveUtils.FastMax(p1.x, p1.x + (int)(maxNoiseSize * noise));

                        for (int x = xMin; x <= xMax; x++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
            else if (p1.y == p2.y)
            {
                Logger.Debug($"normalY [{p1}] [{p2}]");

                int zMin = CaveUtils.FastMin(p1.z, p2.z);
                int zMax = CaveUtils.FastMax(p1.z, p2.z);

                for (int z = zMin; z < zMax; z++)
                {
                    int xMin = CaveUtils.FastMin(p1.x, p2.x);
                    int xMax = CaveUtils.FastMax(p1.x, p2.x);

                    for (int x = xMin; x < xMax; x++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(x, p1.y, z));

                        int yMin = CaveUtils.FastMin(p1.y, p1.y + (int)(maxNoiseSize * noise));
                        int yMax = CaveUtils.FastMax(p1.y, p1.y + (int)(maxNoiseSize * noise));

                        for (int y = yMin; y < yMax; y++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
            else if (p1.z == p2.z)
            {
                Logger.Debug($"normalZ [{p1}] [{p2}] {normalDir}");

                int xMin = CaveUtils.FastMin(p1.x, p2.x);
                int xMax = CaveUtils.FastMax(p1.x, p2.x);

                for (int x = xMin; x < xMax; x++)
                {
                    int yMin = CaveUtils.FastMin(p1.y, p2.y);
                    int yMax = CaveUtils.FastMax(p1.y, p2.y);

                    for (int y = yMin; y < yMax; y++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(x, y, p1.z));

                        int zMin = CaveUtils.FastMin(p1.z, p1.z + (int)(maxNoiseSize * noise));
                        int zMax = CaveUtils.FastMax(p1.z, p1.z + (int)(maxNoiseSize * noise));

                        for (int z = zMin; z < zMax; z++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
        }

        // noiseMap.ExceptWith(innerPoints);

        Logger.Debug($"noiseMap size = {noiseMap.Count}");

        return noiseMap;
    }

    public HashSet<Vector3i> GetNoiseAround()
    {
        var coveredPoints = new HashSet<Vector3i>();
        var noiseMap = new HashSet<Vector3i>();

        while (coveredPoints.Count <= size.x * size.z + size.x + size.z)
        {
            int radius = CaveBuilder.rand.Next(5, 10);

            Vector3i center = new Vector3i(
                CaveBuilder.rand.Next(position.x, position.x + size.x),
                0,
                CaveBuilder.rand.Next(position.z, position.z + size.z)
            );

            noiseMap.UnionWith(CaveBuilder.ParseCircle(center, radius));
            coveredPoints.UnionWith(noiseMap);
            coveredPoints.IntersectWith(innerPoints);

            Logger.Debug($"{coveredPoints.Count}");
        }

        Logger.Debug($"{coveredPoints.Count}, size = {size}");

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

    public PrefabDataInstance ToPrefabDataInstance(int y)
    {
        position.y = y;
        prefabDataInstance.boundingBoxPosition = position;
        prefabDataInstance.rotation = rotation;

        return prefabDataInstance;
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
            new Vector3i(position.x + 1, position.y, position.z),
            new Vector3i(position.x - 1, position.y, position.z),
            new Vector3i(position.x, position.y + 1, position.z),
            new Vector3i(position.x, position.y - 1, position.z),
            new Vector3i(position.x, position.y, position.z + 1),
            new Vector3i(position.x, position.y, position.z - 1),
        };

        var neighbors = new List<Node>();

        foreach (var position in neighborsPos)
        {
            if (position.x < 0 || position.x >= CaveBuilder.MAP_SIZE - CavePlanner.radiationZoneMargin)
                continue;

            if (position.z < 0 || position.z >= CaveBuilder.MAP_SIZE - CavePlanner.radiationZoneMargin)
                continue;

            if (position.y >= CaveBuilder.GetHeight(position.x, position.z))
                continue;

            if (position.y <= CavePlanner.cavePrefabBedRockMargin)
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


public static class Astar
{
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

        HashSet<Node> queue = new HashSet<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        obstacles.Remove(startPos);
        obstacles.Remove(targetPos);

        queue.Add(startNode);

        var path = new HashSet<Vector3i>();

        while (queue.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(queue);

            if (currentNode.position == goalNode.position)
            {
                path = ReconstructPath(currentNode);
                break;
            }

            queue.Remove(currentNode);
            visited.Add(currentNode);

            var neighbors = currentNode.GetNeighbors();

            foreach (Node neighbor in neighbors)
            {
                Logger.Debug($"{currentNode.position} {neighbor.position} ({obstacles.Contains(neighbor.position)})");

                if (visited.Contains(neighbor))
                    continue;

                if (obstacles.Contains(neighbor.position))
                    continue;

                float noise = 0.5f * (1 + perlinNoise.GetNoise(neighbor.position.x, neighbor.position.z));
                float factor = noise < CaveBuilder.NOISE_THRESHOLD ? .5f : 1f;

                factor *= noiseMap.Contains(neighbor.position) ? 1f : .5f;

                float tentativeGCost = currentNode.GCost + CaveUtils.SqrEuclidianDist(currentNode, neighbor) * factor;

                if (!queue.Contains(neighbor) || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CaveUtils.SqrEuclidianDist(neighbor, goalNode) * factor;

                    queue.Add(neighbor);
                }
            }
        }

        if (path.Count == 0)
        {
            Logger.Error($"No Path found from {startPos} to {targetPos}.");
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

    public static int MAP_SIZE = 50;

    public static int PREFAB_Y = 5;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;

    public static int MAP_OFFSET = MAP_SIZE / 60;

    public static float POINT_WIDTH = 5;

    public static int PREFAB_COUNT = MAP_SIZE / 5;

    public static float NOISE_THRESHOLD = 0.5f;

    public static Random rand = new Random(SEED);

    public static int GetHeight(int x, int z)
    {
        return 256;
    }

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

    public static bool TryPlacePrefab(ref PrefabWrapper prefab, List<PrefabWrapper> others)
    {
        int maxTries = 100;

        while (maxTries-- > 0)
        {
            prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

            if (!prefab.OverLaps2D(others))
            {
                return true;
            }
        }

        return false;
    }

    public static List<PrefabWrapper> GetRandomPrefabs(int count)
    {
        throw new NotImplementedException();
        // Logger.Info("Start POIs placement...");

        // var prefabs = new List<PrefabWrapper>();

        // for (int i = 0; i < count; i++)
        // {
        //     var prefab = new PrefabWrapper(rand);

        //     prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

        //     if (CheckPrefabOverlaps(prefab, prefabs))
        //         prefabs.Add(prefab);
        // }

        // Logger.Info($"{prefabs.Count} / {PREFAB_COUNT} prefabs added");

        // return prefabs;
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
