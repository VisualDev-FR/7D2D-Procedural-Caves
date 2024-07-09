#pragma warning disable CA1416, CA1050, IDE0090


using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;


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

    public override bool Equals(object obj)
    {
        var other = (Vector3i)obj;

        return GetHashCode() == other.GetHashCode();
    }

    public override int GetHashCode()
    {
        return x * CaveBuilder.MAP_SIZE + z;
    }
}


public class Prefab
{
    public const int OVERLAP_MARGIN = 50;

    public Vector3i position;

    public Vector3i size;

    public int rotation = 1;

    public void SetRandomPosition(Random rand, int mapSize, int mapOffset)
    {
        position = new Vector3i(
            rand.Next(mapOffset, mapSize - mapOffset - size.x),
            rand.Next(mapOffset, mapSize - mapOffset - size.y),
            rand.Next(mapOffset, mapSize - mapOffset - size.z)
        );
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

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.z; position.z < position.z + size.z; z++)
            {
                points.Add(new Vector3i(x, 0, z));
            }
        }

        return points;
    }

    public PointF ToPointF()
    {
        return new PointF(position.x, position.z);
    }
}


public class Edge : IComparable<Edge>
{
    public int Start { get; set; }

    public int End { get; set; }

    public float Weight { get; set; }

    public Prefab StartPoint { get; }

    public Prefab EndPoint { get; }

    private float GetWeight()
    {
        float euclidianDist = MathF.Sqrt(
              MathF.Pow(StartPoint.position.x - EndPoint.position.x, 2)
            + MathF.Pow(StartPoint.position.z - EndPoint.position.z, 2)
        );

        return euclidianDist; // MathF.Abs(StartPoint.sizeX * StartPoint.sizeZ - EndPoint.sizeX * EndPoint.sizeZ);
    }

    public Edge(int _start, int _end, Prefab _startPoint, Prefab _endPoint)
    {
        Start = _start;
        End = _end;
        StartPoint = _startPoint;
        EndPoint = _endPoint;
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

    public List<Node> GetNeighbors(Dictionary<string, bool> obstacles, FastNoiseLite perlinNoise)
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


public static class AStar
{
    public static List<Vector3i> FindPath(Vector3i startPos, Vector3i targetPos, Dictionary<string, bool> obstacles, FastNoiseLite perlinNoise)
    {
        Node startNode = new(startPos);
        Node goalNode = new(targetPos);

        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        var path = new List<Vector3i>();
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

            // Console.WriteLine(counter);
            // Console.WriteLine(currentNode.position);

            foreach (Node neighbor in currentNode.GetNeighbors(obstacles, perlinNoise))
            {
                if (closedSet.Contains(neighbor))
                    continue;


                float noise = 0.5f * (1 + perlinNoise.GetNoise(neighbor.position.x, neighbor.position.z));
                float factor = noise < CaveBuilder.NOISE_THRESHOLD ? .5f : 1f;

                float tentativeGCost = currentNode.GCost + EuclidianDist(currentNode, neighbor) * factor;

                if (!openSet.Contains(neighbor) || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = EuclidianDist(neighbor, goalNode) * factor;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        if (path.Count > 0)
        {
            Console.WriteLine($"INFO     Path found from {startPos} to {targetPos}, {counter} iterations done.");
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

    private static List<Vector3i> ReconstructPath(Node currentNode)
    {
        List<Vector3i> path = new();

        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }
}


public static class CaveBuilder
{
    public static readonly int SEED = 479822329; //new Random().Next();

    public const int MAP_SIZE = 6144;

    public const int MAP_OFFSET = MAP_SIZE / 60;

    public const float POINT_WIDTH = 5;

    public const float EDGE_WIDTH = MAP_SIZE / 2000;

    public const int PREFAB_COUNT = MAP_SIZE / 5;

    public const float NOISE_THRESHOLD = 0.50f;

    public static readonly Random rand = new Random(SEED);

    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.DarkGray;

    private static FastNoiseLite ParsePerlinNoise()
    {
        var noise = new FastNoiseLite(SEED);

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
            var prefab = new Prefab()
            {
                size = new Vector3i(
                    rand.Next(8, 100),
                    rand.Next(8, 100),
                    rand.Next(8, 100)
                )
            };

            prefab.SetRandomPosition(rand, MAP_SIZE, MAP_OFFSET);

            if (CheckPrefabOverlaps(prefab, prefabs))
                prefabs.Add(prefab);
        }

        Console.WriteLine($"{prefabs.Count} prefabs added");

        return prefabs;
    }

    public static void DrawPrefabs(Graphics graph, List<Prefab> prefabs)
    {
        using Pen pen = new Pen(PrefabBoundsColor, POINT_WIDTH);

        foreach (var prefab in prefabs)
        {
            graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);
        }
    }

    public static void DrawPath(Bitmap bitmap, List<Vector3i> points)
    {
        foreach (var point in points)
        {
            Debug.Assert(point.x >= 0);
            Debug.Assert(point.z >= 0);
            Debug.Assert(point.z < MAP_SIZE);
            Debug.Assert(point.z < MAP_SIZE);

            bitmap.SetPixel(point.x, point.z, TunnelsColor);
        }
    }

    public static void DrawEdges(Graphics graph, List<Edge> edges)
    {
        using Pen pen = new Pen(TunnelsColor, EDGE_WIDTH);

        foreach (var edge in edges)
        {
            graph.DrawCurve(pen, new PointF[2]{
                edge.StartPoint.ToPointF(),
                edge.EndPoint.ToPointF(),
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
                edges.Add(new Edge(i, j, prefabs[i], prefabs[j]));
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

    private static Dictionary<string, bool> GetPrefabObstacles(List<Prefab> prefabs)
    {
        var obstacles = new Dictionary<string, bool>();

        foreach (Prefab prefab in prefabs)
        {
            foreach (Vector3i point in prefab.GetAllPoints())
            {
                obstacles[point.ToString()] = true;
            }
        }

        return obstacles;
    }

    private static List<Vector3i> PerlinRoute(Vector3i startPos, Vector3i targetpos, FastNoiseLite noise, List<Prefab> prefabs)
    {
        Dictionary<string, bool> obstacles = GetPrefabObstacles(prefabs);

        List<Vector3i> path = AStar.FindPath(startPos, targetpos, obstacles, noise);

        return path;
    }

    private static void GenerateGraph(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = GetRandomPrefabs(prefabCounts);

        List<Edge> edges = KruskalMST(prefabs);

        using Bitmap b = new Bitmap(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            DrawEdges(g, edges);
            DrawPrefabs(g, prefabs);
        }

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

        FastNoiseLite noise = ParsePerlinNoise();

        List<Vector3i> path = PerlinRoute(p1.position, p2.position, noise, new List<Prefab>());

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);
            DrawNoise(b, noise);
            DrawPath(b, path);

            b.SetPixel(p1.position.x, p1.position.z, NodeColor);
            b.SetPixel(p2.position.x, p2.position.z, NodeColor);
        }

        b.Save(@"pathing.png", ImageFormat.Png);
    }

    private static void GenerateCaves(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = GetRandomPrefabs(prefabCounts);

        List<Edge> edges = KruskalMST(prefabs);

        FastNoiseLite noise = ParsePerlinNoise();

        using Bitmap b = new(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(BackgroundColor);

            foreach (Edge edge in edges)
            {

                Vector3i p1 = edge.StartPoint.position;
                Vector3i p2 = edge.EndPoint.position;

                // Console.WriteLine($"Start pathing from {p1} to {p2}, dist={AStar.EuclidianDist(p1, p2)}");

                List<Vector3i> path = PerlinRoute(p1, p2, noise, new List<Prefab>());

                DrawPath(b, path);

                b.SetPixel(p1.x, p1.z, NodeColor);
                b.SetPixel(p2.x, p2.z, NodeColor);
            }
        }

        b.Save(@"cave.png", ImageFormat.Png);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"SEED :{SEED}");
        Console.WriteLine($"SIZE :{MAP_SIZE}");

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
}
