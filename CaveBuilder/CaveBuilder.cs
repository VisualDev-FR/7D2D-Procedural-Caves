#pragma warning disable CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;


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


public class Vector3i
{
    public int x;

    public int y;

    public int z;

    public Vector3i(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public static bool operator !=(Vector3i p1, Vector3i p2)
    {
        return !(p1 == p2);
    }

    public static bool operator ==(Vector3i p1, Vector3i p2)
    {
        return p1.x == p2.x && p1.z == p2.z;
    }

    public static Vector3i operator +(Vector3i p1, Vector3i p2)
    {
        return new Vector3i(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
    }

    public override bool Equals(object obj)
    {
        var other = (Vector3i)obj;

        return GetHashCode() == other.GetHashCode();
    }

    public override int GetHashCode()
    {
        return x * CaveBuilder.MAP_SIZE + z;
    }

    public PointF ToPointF()
    {
        return new PointF(x, z);
    }
}


public static class Utils
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


public class Prefab
{
    public const int OVERLAP_MARGIN = 50;

    public Vector3i position;

    public Vector3i size;

    public List<Vector3i> nodes;

    public int rotation = 1;

    public List<Vector3i> innerPoints;

    public Prefab()
    {
        nodes = new List<Vector3i>();
    }

    public Prefab(Random rand)
    {
        nodes = new List<Vector3i>();
        size = new Vector3i(
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
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
                innerPoints.Add(new Vector3i(x, 0, z));
            }
        }
    }

    public void SetRandomPosition(Random rand, int mapSize, int mapOffset)
    {
        position = new Vector3i(
            rand.Next(mapOffset, mapSize - mapOffset - size.x),
            rand.Next(mapOffset, mapSize - mapOffset - size.y),
            rand.Next(mapOffset, mapSize - mapOffset - size.z)
        );

        UpdateNodes(rand);
        UpdateInnerPoints();
    }

    public bool OverLaps2D(Prefab other, int map_size, int map_offset)
    {
        if (position.x + size.x + OVERLAP_MARGIN < other.position.x || other.position.x + other.size.x + OVERLAP_MARGIN < position.x)
            return false;

        if (position.z + size.z + OVERLAP_MARGIN < other.position.z || other.position.z + other.size.z + OVERLAP_MARGIN < position.z)
            return false;

        return true;
    }

    public bool OverLaps2D(List<Prefab> others, int map_size, int map_offset)
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
            position.y + size.y / 2,
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
        int index1 = Utils.FastMin(prefabIndex1, prefabIndex2);
        int index2 = Utils.FastMax(prefabIndex1, prefabIndex2);

        return $"{index1};{index2}";
    }

    private float GetWeight()
    {
        float euclidianDist = MathF.Sqrt(
              MathF.Pow(node1.x - node2.x, 2)
            + MathF.Pow(node1.z - node2.z, 2)
        );

        return euclidianDist; // MathF.Abs(StartPoint.sizeX * StartPoint.sizeZ - EndPoint.sizeX * EndPoint.sizeZ);
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


public class UnionFind
{
    private int[] parent;
    private int[] rank;

    public UnionFind(int size)
    {
        parent = new int[size];
        rank = new int[size];
        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }
    }

    public int Find(int x)
    {
        if (parent[x] != x)
        {
            parent[x] = Find(parent[x]);
        }
        return parent[x];
    }

    public void Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);

        if (rootX != rootY)
        {
            if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
            }
            else if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }
        }
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
        HashSet<Vector3i> path = new();

        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.Parent;
        }

        return path;
    }

    private static HashSet<Vector3i> FindPath(Vector3i startPos, Vector3i targetPos, HashSet<Vector3i> obstacles, HashSet<Vector3i> noiseMap, FastNoiseLite perlinNoise)
    {
        Node startNode = new(startPos);
        Node goalNode = new(targetPos);

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

                float tentativeGCost = currentNode.GCost + Utils.SqrEuclidianDist(currentNode, neighbor) * factor;

                if (!openSet.Contains(neighbor) || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = Utils.SqrEuclidianDist(neighbor, goalNode) * factor;

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

        // const int MIN_CAVE_WIDTH = 1;
        // const int MAX_CAVE_WIDTH = 20;

        foreach (var position in wiredCaveMap)
        {
            float noise = 0.5f * (1 + thickingNoise.GetNoise(position.x, position.z));
            float radius = 2; // MIN_CAVE_WIDTH + noise * (MAX_CAVE_WIDTH - MIN_CAVE_WIDTH);

            var circle = CaveBuilder.ParseCircle(position, radius);

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
    private static List<Edge> BuildPrefabGraph(List<Prefab> prefabs)
    {
        Dictionary<int, List<Edge>> prefabEdges = new();

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

            HashSet<Vector3i> connectedNodes = new();

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

    public static List<Edge> Resolve(List<Prefab> prefabs)
    {
        Stopwatch timer = new();

        timer.Start();

        List<Edge> graph = BuildPrefabGraph(prefabs);

        Logger.Info($"Graph resolved in {Utils.TimeFormat(timer)}");

        return graph;
    }
}


public static class CaveBuilder
{
    private static int SEED = 12345; // new Random().Next();

    public const int MAP_SIZE = 6144;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;

    public const int MAP_OFFSET = MAP_SIZE / 60;

    public const float POINT_WIDTH = 5;

    public const int PREFAB_COUNT = MAP_SIZE / 5;

    public const float NOISE_THRESHOLD = 0.5f;

    public static Random rand = new Random(SEED);

    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.DarkGray;

    public static HashSet<Vector3i> ParseCircle(Vector3i center, float radius)
    {
        HashSet<Vector3i> queue = new() { center };
        HashSet<Vector3i> visited = new();

        while (queue.Count > 0)
        {
            foreach (var pos in queue.ToArray())
            {
                queue.Remove(pos);

                if (visited.Contains(pos))
                    continue;

                visited.Add(pos);

                if (Utils.SqrEuclidianDist(pos, center) >= radius)
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

    private static bool CheckPrefabOverlaps(Prefab prefab, List<Prefab> others)
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

    private static List<Prefab> GetRandomPrefabs(int count)
    {
        Logger.Info("Start POIs placement...");

        var prefabs = new List<Prefab>();

        for (int i = 0; i < count; i++)
        {
            var prefab = new Prefab(rand);

            prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

            if (CheckPrefabOverlaps(prefab, prefabs))
                prefabs.Add(prefab);
        }

        Logger.Info($"{prefabs.Count} / {PREFAB_COUNT} prefabs added");

        return prefabs;
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<Prefab> prefabs, bool fill = false)
    {
        using Pen pen = new Pen(PrefabBoundsColor, 1);

        foreach (var prefab in prefabs)
        {
            graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);

            if (fill)
                DrawPoints(b, new HashSet<Vector3i>(prefab.innerPoints), PrefabBoundsColor);

            DrawPoints(b, new HashSet<Vector3i>(prefab.nodes), NodeColor);
        }
    }

    public static void DrawPoints(Bitmap bitmap, HashSet<Vector3i> points, Color color)
    {
        foreach (var point in points)
        {
            bitmap.SetPixel(point.x, point.z, color);
        }
    }

    public static void DrawEdges(Graphics graph, List<Edge> edges)
    {
        using Pen pen = new Pen(TunnelsColor, 2);

        foreach (var edge in edges)
        {
            graph.DrawCurve(pen, new PointF[2]{
                edge.node1.ToPointF(),
                edge.node2.ToPointF(),
            });
        }
    }

    private static void DrawNoise(Bitmap b, FastNoiseLite perlinNoise)
    {
        for (int x = 0; x < MAP_SIZE; x++)
        {
            for (int z = 0; z < MAP_SIZE; z++)
            {
                float noise = 0.5f * (perlinNoise.GetNoise(x, z) + 1);

                if (noise < NOISE_THRESHOLD)
                    b.SetPixel(x, z, NoiseColor);
            }
        }
    }

    private static HashSet<Vector3i> CollectPrefabObstacles(List<Prefab> prefabs)
    {
        var obstacles = new HashSet<Vector3i>();

        foreach (Prefab prefab in prefabs)
        {
            obstacles.UnionWith(prefab.innerPoints);
        }

        return obstacles;
    }

    private static HashSet<Vector3i> CollectPrefabNoise(List<Prefab> prefabs, FastNoiseLite noise)
    {
        var noiseMap = new HashSet<Vector3i>();

        foreach (Prefab prefab in prefabs)
        {
            noiseMap.UnionWith(prefab.GetNoiseAround(rand));
        }

        return noiseMap;
    }

    private static void GenerateGraph(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = GetRandomPrefabs(prefabCounts);

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = GraphSolver.Resolve(prefabs);

        Logger.Info("Start Drawing graph...");

        using Bitmap b = new Bitmap(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            DrawEdges(g, edges);
            DrawPrefabs(b, g, prefabs);
        }

        Logger.Info($"{edges.Count} Generated edges.");

        b.Save(@"graph.png", ImageFormat.Png);
    }

    private static void GenerateNoise(string[] args)
    {
        var noise = ParsePerlinNoise();

        using Bitmap b = new Bitmap(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            DrawNoise(b, noise);
        }

        b.Save(@"noise.png", ImageFormat.Png);
    }

    private static void GeneratePath(string[] args)
    {
        Prefab p1 = new()
        {
            position = new Vector3i(10, 0, 10),
            size = new Vector3i(10, 0, 10),
        };

        Prefab p2 = new()
        {
            position = new Vector3i(MAP_SIZE - 20, 0, MAP_SIZE - 20),
            size = new Vector3i(10, 0, 10),
        };

        var prefabs = new List<Prefab>() { p1, p2 };

        p1.UpdateInnerPoints();
        p2.UpdateInnerPoints();

        FastNoiseLite noise = ParsePerlinNoise();

        HashSet<Vector3i> obstacles = CollectPrefabObstacles(prefabs);
        HashSet<Vector3i> noiseMap = CollectPrefabNoise(prefabs, noise);
        HashSet<Vector3i> path = CaveTunneler.PerlinRoute(p1.position, p2.position, noise, obstacles, noiseMap);

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            // DrawNoise(b, noise);
            DrawPoints(b, noiseMap, NoiseColor);
            DrawPoints(b, path, TunnelsColor);
            DrawPrefabs(b, g, prefabs);

            b.SetPixel(p1.position.x, p1.position.z, NodeColor);
            b.SetPixel(p2.position.x, p2.position.z, NodeColor);
        }

        b.Save(@"pathing.png", ImageFormat.Png);
    }

    private static void SaveCaveMap(HashSet<Vector3i> caveMap, string filename)
    {
        using (StreamWriter writer = new StreamWriter(filename))
        {
            foreach (var caveBlock in caveMap)
            {
                writer.WriteLine(caveBlock.ToString());
            }
            Logger.Info($"CaveMap saved '{filename}'.");
        }
    }

    private static void GenerateCaves(string[] args)
    {
        Stopwatch timer = new Stopwatch();
        timer.Start();

        FastNoiseLite noise = ParsePerlinNoise();

        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;
        var prefabs = GetRandomPrefabs(prefabCounts);

        HashSet<Vector3i> obstacles = CollectPrefabObstacles(prefabs);
        HashSet<Vector3i> noiseMap = CollectPrefabNoise(prefabs, noise);

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = GraphSolver.Resolve(prefabs);

        var wiredCaveMap = new ConcurrentBag<Vector3i>();
        int index = 0;

        Parallel.ForEach(edges, edge =>
        {
            Vector3i p1 = edge.node1;
            Vector3i p2 = edge.node2;

            Logger.Info($"Noise pathing: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count}), dist={Utils.SqrEuclidianDist(p1, p2)}");

            HashSet<Vector3i> path = CaveTunneler.PerlinRoute(p1, p2, noise, obstacles, noiseMap);

            foreach (Vector3i node in path)
            {
                wiredCaveMap.Add(node);
            }
        });

        var caveMap = CaveTunneler.ThickenCaveMap(wiredCaveMap.ToHashSet(), obstacles);

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);

            DrawPoints(b, caveMap, TunnelsColor);
            DrawPrefabs(b, g, prefabs);
        }

        b.Save(@"cave.png", ImageFormat.Png);

        SaveCaveMap(caveMap, "cavemap.csv");

        Console.WriteLine($"{caveMap.Count} cave blocks generated, timer={Utils.TimeFormat(timer)}.");
    }

    private static void GeneratePrefab(string[] args)
    {
        var mapCenter = new Vector3i(-10 + MAP_SIZE / 2, 0, -10 + MAP_SIZE / 2);
        var prefab = new Prefab()
        {
            position = mapCenter,
            size = new Vector3i(20, 0, 20),
        };

        prefab.UpdateNodes(rand);
        prefab.UpdateInnerPoints();

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);
        using Pen pen = new Pen(PrefabBoundsColor, 1);

        var noise = ParsePerlinNoise(SEED);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);

            DrawPoints(b, prefab.GetNoiseAround(rand), NoiseColor);
            g.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);
            DrawPoints(b, prefab.nodes.ToHashSet(), NodeColor);
        }

        b.Save(@"prefab.png", ImageFormat.Png);
    }

    public static void Main(string[] args)
    {
        Logger.Info($"SEED .......... {SEED}");
        Logger.Info($"SIZE .......... {MAP_SIZE}");
        Logger.Info($"PREFAB_COUNT .. {PREFAB_COUNT}");
        Logger.Blank();

        switch (args[0])
        {
            case "graph":
                GenerateGraph(args);
                break;

            case "path":
                GeneratePath(args);
                break;

            case "noise":
                GenerateNoise(args);
                break;

            case "cave":
            case "caves":
                GenerateCaves(args);
                break;

            case "prefab":
                GeneratePrefab(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }

    public static void Test(string[] args)
    {
        var vectorSet = new HashSet<Vector3i>();

        vectorSet.Add(new Vector3i(0, 1, 0));
        vectorSet.Add(new Vector3i(0, 2, 0));
        Debug.Assert(vectorSet.Count == 1);

        vectorSet.Add(new Vector3i(0, 2, 1));
        Debug.Assert(vectorSet.Count == 2);

        vectorSet.Add(new Vector3i(0, 3, 1));
        Debug.Assert(vectorSet.Count == 2);

        Debug.Assert(vectorSet.Contains(new Vector3i(0, 8, 1)));
        Debug.Assert(vectorSet.Contains(new Vector3i(0, 5, 0)));
    }
}
