#pragma warning disable CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;

public static class Logger
{
    private static void Logging(string level, string message)
    {
        Console.WriteLine($"{level,-10} {message}");
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

    public static string TimeFormat(Stopwatch timer, string format = @"hh\:mm\:ss")
    {
        return TimeSpan.FromSeconds(timer.ElapsedMilliseconds / 1000).ToString(format);
    }
}


public class Prefab
{
    public const int OVERLAP_MARGIN = 50;

    public Vector3i position;

    public Vector3i size;

    public List<Vector3i> nodes;

    public int rotation = 1;

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

    public void SetRandomPosition(Random rand, int mapSize, int mapOffset)
    {
        position = new Vector3i(
            rand.Next(mapOffset, mapSize - mapOffset - size.x),
            rand.Next(mapOffset, mapSize - mapOffset - size.y),
            rand.Next(mapOffset, mapSize - mapOffset - size.z)
        );

        nodes = new List<Vector3i>()
        {
            position + new Vector3i(size.x / 2 , 0, 0),
            position + new Vector3i(0, 0, size.z / 2),
            position + new Vector3i(size.x / 2, 0, size.z),
            position + new Vector3i(size.x , 0, size.z / 2),
        };
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

    public List<Vector3i> GetAllPoints()
    {
        var points = new List<Vector3i>();

        for (int x = position.x; x <= (position.x + size.x); x++)
        {
            for (int z = position.z; z <= (position.z + size.z); z++)
            {
                points.Add(new Vector3i(x, 0, z));
            }
        }

        return points;
    }
}


public class Edge : IComparable<Edge>
{
    public int Start { get; set; }

    public int End { get; set; }

    public float Weight { get; set; }

    public Vector3i StartPos { get; }

    public Vector3i EndPos { get; }

    private float GetWeight()
    {
        float euclidianDist = MathF.Sqrt(
              MathF.Pow(StartPos.x - EndPos.x, 2)
            + MathF.Pow(StartPos.z - EndPos.z, 2)
        );

        return euclidianDist; // MathF.Abs(StartPoint.sizeX * StartPoint.sizeZ - EndPoint.sizeX * EndPoint.sizeZ);
    }

    public Edge(int _start, int _end, Vector3i _startPoint, Vector3i _endPoint)
    {
        Start = _start;
        End = _end;
        StartPos = _startPoint;
        EndPos = _endPoint;
        Weight = GetWeight();
    }

    public int CompareTo(Edge other)
    {
        return Weight.CompareTo(other.Weight);
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


public static class AStarPerlin
{
    public static HashSet<Vector3i> FindPath(Vector3i startPos, Vector3i targetPos, HashSet<Vector3i> obstacles, FastNoiseLite perlinNoise)
    {
        Node startNode = new(startPos);
        Node goalNode = new(targetPos);

        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        obstacles.Remove(startPos);
        obstacles.Remove(targetPos);

        openSet.Add(startNode);

        var path = new HashSet<Vector3i>();
        int counter = 0;

        while (openSet.Count > 0)
        {
            counter++;

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

                float tentativeGCost = currentNode.GCost + EuclidianDist(currentNode, neighbor) * factor;

                if (!openSet.Contains(neighbor) || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = EuclidianDist(neighbor, goalNode) * factor;

                    openSet.Add(neighbor);
                }
            }
        }

        if (path.Count == 0)
        {
            Logger.Warning($"No Path found from {startPos} to {targetPos}, {counter} iterations done.");
        }

        return path;
    }

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

    public static float EuclidianDist(Node nodeA, Node nodeB)
    {
        return EuclidianDist(nodeA.position, nodeB.position);
    }

    public static float EuclidianDist(Vector3i p1, Vector3i p2)
    {
        return MathF.Sqrt(
              MathF.Pow(p1.x - p2.x, 2)
            + MathF.Pow(p1.z - p2.z, 2)
        );
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
}


public static class CaveBuilder
{
    private static int SEED = new Random().Next();

    public const int MAP_SIZE = 6144;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;


    public const int MAP_OFFSET = MAP_SIZE / 60;

    public const float POINT_WIDTH = 5;

    public const int PREFAB_COUNT = MAP_SIZE / 5;

    public const float NOISE_THRESHOLD = 0.50f;

    public static Random rand = new Random(SEED);

    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.DarkGray;

    private static FastNoiseLite ParsePerlinNoise(int seed = -1)
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
        var prefabs = new List<Prefab>();

        for (int i = 0; i < count; i++)
        {
            var prefab = new Prefab(rand);

            prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

            if (CheckPrefabOverlaps(prefab, prefabs))
                prefabs.Add(prefab);
        }

        Console.WriteLine($"{prefabs.Count} prefabs added");

        return prefabs;
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<Prefab> prefabs, bool fill = false)
    {
        using Pen pen = new Pen(PrefabBoundsColor, 1);

        foreach (var prefab in prefabs)
        {
            graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);

            if (fill)
                DrawPoints(b, new HashSet<Vector3i>(prefab.GetAllPoints()), PrefabBoundsColor);

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
                edge.StartPos.ToPointF(),
                edge.EndPos.ToPointF(),
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

    public static List<Edge> KruskalMST(List<Prefab> prefabs)
    {
        List<Edge> edges = new List<Edge>();

        // Generate all edges with them weight
        for (int i = 0; i < prefabs.Count; i++)
        {
            for (int j = i + 1; j < prefabs.Count; j++)
            {
                foreach (var p1 in prefabs[i].nodes)
                {
                    foreach (var p2 in prefabs[j].nodes)
                    {
                        edges.Add(new Edge(i, j, p1, p2));
                    }
                }
            }
        }

        // Sort edges by weight
        edges.Sort();

        UnionFind uf = new UnionFind(prefabs.Count);
        List<Edge> mst = new List<Edge>();

        foreach (var edge in edges)
        {
            if (uf.Find(edge.Start) != uf.Find(edge.End))
            {
                uf.Union(edge.Start, edge.End);
                mst.Add(edge);
            }
        }

        return mst;
    }

    private static HashSet<Vector3i> GetPrefabObstacles(List<Prefab> prefabs)
    {
        var obstacles = new HashSet<Vector3i>();

        foreach (Prefab prefab in prefabs)
        {
            obstacles.UnionWith(prefab.GetAllPoints());
        }

        return obstacles;
    }

    private static HashSet<Vector3i> PerlinRoute(Vector3i startPos, Vector3i targetpos, FastNoiseLite noise, HashSet<Vector3i> obstacles)
    {
        HashSet<Vector3i> path = AStarPerlin.FindPath(startPos, targetpos, obstacles, noise);

        // int maxCaveWidth = 20;
        // int minCaveWidth = 1;

        // FastNoiseLite noiseX = ParsePerlinNoise(rand.Next());
        // FastNoiseLite noiseZ = ParsePerlinNoise(rand.Next());

        // foreach (Vector3i point in new List<Vector3i>(path))
        // {
        //     int widthX = minCaveWidth + (int)(0.5f * maxCaveWidth * (1 + noiseX.GetNoise(point.x, point.z)));
        //     int widthZ = widthX; // minCaveWidth + (int)(0.5f * maxCaveWidth * (1 + noiseZ.GetNoise(point.x, point.z)));

        //     for (int x = point.x; x < point.x + widthX; x++)
        //     {
        //         for (int z = point.z; z < point.z + widthZ; z++)
        //         {
        //             path.Add(new Vector3i(x, 0, z));
        //         }
        //     }
        // }

        return path;
    }

    private static void GenerateGraph(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = GetRandomPrefabs(prefabCounts);

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = KruskalMST(prefabs);

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

        FastNoiseLite noise = ParsePerlinNoise();

        HashSet<Vector3i> obstacles = GetPrefabObstacles(prefabs);
        HashSet<Vector3i> path = PerlinRoute(p1.position, p2.position, noise, obstacles);

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            DrawNoise(b, noise);
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

        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = GetRandomPrefabs(prefabCounts);

        HashSet<Vector3i> obstacles = GetPrefabObstacles(prefabs);
        HashSet<Vector3i> caveMap = new(3_000_000);
        HashSet<Vector3i> nodes = new();

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = KruskalMST(prefabs);

        FastNoiseLite noise = ParsePerlinNoise();

        int index = 0;

        foreach (Edge edge in edges)
        {
            Vector3i p1 = edge.StartPos;
            Vector3i p2 = edge.EndPos;

            caveMap.UnionWith(PerlinRoute(p1, p2, noise, obstacles));

            nodes.Add(p1);
            nodes.Add(p2);

            Logger.Info($"Perlin pathing: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count})");
        }

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);

            DrawPoints(b, caveMap, TunnelsColor);
            DrawPoints(b, nodes, NodeColor);
            DrawPrefabs(b, g, prefabs);
        }

        b.Save(@"cave.png", ImageFormat.Png);
        SaveCaveMap(caveMap, "cavemap.csv");

        Console.WriteLine($"{caveMap.Count} cave blocks generated, timer={Utils.TimeFormat(timer)}.");
    }

    public static void Main(string[] args)
    {
        Logger.Info($"SEED={SEED}");
        Logger.Info($"SIZE={MAP_SIZE}\n");

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
