using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Collections.Generic;


public class Rectangle
{
    public const int OVERLAP_MARGIN = 50;

    public int x;

    public int y;

    public int z;

    public int sizeX = 50;

    public int sizeY = 50;

    public int sizeZ = 50;

    public int rotation = 1;

    public bool OverLaps2D(Rectangle other, int map_size, int map_offset)
    {
        // is under left border
        if (x < map_offset)
            return true;

        // is under top border
        if (z < map_offset)
            return true;

        // is over right border
        if (x + sizeX >= map_size - map_offset)
            return true;

        // is over bottom border
        if (z + sizeZ >= map_size - map_offset)
            return true;

        if (x + sizeX + OVERLAP_MARGIN < other.x || other.x + other.sizeX + OVERLAP_MARGIN < x)
            return false;

        if (z + sizeZ + OVERLAP_MARGIN < other.z || other.z + other.sizeZ + OVERLAP_MARGIN < z)
            return false;

        return true;
    }

    public bool OverLaps2D(List<Rectangle> others, int map_size, int map_offset)
    {
        foreach (var point in others)
        {
            if (OverLaps2D(point, map_size, map_offset))
                return true;
        }

        return false;
    }

    public PointF ToPointF()
    {
        return new PointF(x, z);
    }
}


public class Edge : IComparable<Edge>
{
    public int Start { get; set; }

    public int End { get; set; }

    public float Weight { get; set; }

    public Rectangle StartPoint { get; }

    public Rectangle EndPoint { get; }

    private float GetWeight()
    {
        float euclidianDist = MathF.Sqrt(
              MathF.Pow(StartPoint.x - EndPoint.x, 2)
            + MathF.Pow(StartPoint.z - EndPoint.z, 2)
        );

        return euclidianDist; // MathF.Abs(StartPoint.sizeX * StartPoint.sizeZ - EndPoint.sizeX * EndPoint.sizeZ);
    }

    public Edge(int _start, int _end, Rectangle _startPoint, Rectangle _endPoint)
    {
        Start = _start;
        End = _end;
        StartPoint = _startPoint;
        EndPoint = _endPoint;
        Weight = GetWeight();
    }

    public int CompareTo(Edge? other)
    {
        return Weight.CompareTo(other?.Weight);
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


[SupportedOSPlatform("windows")]
public static class CaveBuilder
{
    static readonly int SEED = new Random().Next();

    const int MAP_SIZE = 6144;

    const int MAP_OFFSET = 200;

    const float POINT_WIDTH = 5;

    const float EDGE_WIDTH = MAP_SIZE / 2000;

    const int POINTS_COUNT = MAP_SIZE / 5;

    static readonly Random rand = new Random(SEED);

    static readonly Color POINT_COLOR = Color.Green;

    static readonly Color EDGE_COLOR = Color.DarkGray;

    private static FastNoiseLite GetNoise()
    {
        var noise = new FastNoiseLite(SEED);

        return noise;
    }

    private static bool checkOverlaps(Rectangle rect, List<Rectangle> others)
    {
        int i;

        for (i = 0; i < 100; i++)
        {
            if (rect.OverLaps2D(others, MAP_SIZE, MAP_OFFSET))
            {
                rect.x = rand.Next(0, MAP_SIZE);
                rect.z = rand.Next(0, MAP_SIZE);
                rect.z = rand.Next(0, MAP_SIZE);
            }
            else
            {
                break;
            }
        }

        Console.WriteLine($"{i + 1} iterations done.");

        return !rect.OverLaps2D(others, MAP_SIZE, MAP_OFFSET);
    }

    private static List<Rectangle> GetRectangles(int count)
    {
        var points = new List<Rectangle>();

        for (int i = 0; i < count; i++)
        {
            var point = new Rectangle()
            {
                x = rand.Next(0, MAP_SIZE),
                y = rand.Next(0, MAP_SIZE),
                z = rand.Next(0, MAP_SIZE),
                sizeX = rand.Next(8, 100),
                sizeZ = rand.Next(8, 100)
            };

            if (checkOverlaps(point, points))
                points.Add(point);
        }

        Console.WriteLine($"{points.Count} points added");

        return points;
    }

    public static void DrawPrefabs(Graphics graph, List<Rectangle> points)
    {
        using Pen pen = new Pen(POINT_COLOR, POINT_WIDTH);

        foreach (var point in points)
        {
            graph.DrawRectangle(pen, point.x, point.z, point.sizeX, point.sizeZ);
        }
    }

    public static void DrawEdges(Graphics graph, List<Edge> edges)
    {
        using Pen pen = new Pen(EDGE_COLOR, EDGE_WIDTH);

        foreach (var edge in edges)
        {
            graph.DrawCurve(pen, new PointF[2]{
                edge.StartPoint.ToPointF(),
                edge.EndPoint.ToPointF(),
            });
        }
    }

    public static List<Edge> KruskalMST(List<Rectangle> points)
    {
        List<Edge> edges = new List<Edge>();

        // Generate all edges with them weight
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                edges.Add(new Edge(i, j, points[i], points[j]));
            }
        }

        // Sort edges by weight
        edges.Sort();

        UnionFind uf = new UnionFind(points.Count);
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

    private static void DrawGraph(string[] args)
    {
        int pointCounts = args.Length > 1 ? int.Parse(args[1]) : POINTS_COUNT;

        var points = GetRectangles(pointCounts);

        List<Edge> edges = KruskalMST(points);

        using Bitmap b = new Bitmap(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.Black);
            DrawEdges(g, edges);
            DrawPrefabs(g, points);
        }

        b.Save(@"graph.png", ImageFormat.Png);
    }

    private static List<Edge> WormPathing(Rectangle startPoint, Rectangle endPoint)
    {
        var edges = new List<Edge>
        {
            // new Edge(0, 1, startPoint, endPoint)
        };

        var lastPoint = startPoint;
        int n = 10;
        int amplitude = 20;

        for (int i = 0; i < n; i++)
        {
            int xi = startPoint.x + (endPoint.x - startPoint.x) * i / n;
            int zi = startPoint.z + (endPoint.z - startPoint.z) * i / n;

            xi += rand.Next(amplitude);
            zi += rand.Next(amplitude);

            var interPoint = new Rectangle()
            {
                x = xi,
                y = 0,
                z = zi,
            };

            edges.Add(new Edge(0, 0, lastPoint, interPoint));

            lastPoint = interPoint;
        }

        edges.Add(new Edge(0, 0, lastPoint, endPoint));

        return edges;
    }

    private static void DrawPath(string[] args)
    {
        Rectangle p1 = new Rectangle() { x = 10, y = 0, z = 10 };
        Rectangle p2 = new Rectangle() { x = MAP_SIZE - 10, y = 0, z = MAP_SIZE - 10 };

        List<Rectangle> points = new List<Rectangle> { p1, p2 };

        List<Edge> edges = WormPathing(p1, p2);

        using Bitmap b = new Bitmap(MAP_SIZE, MAP_SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.White);
            DrawEdges(g, edges);
            DrawPrefabs(g, points);
        }

        b.Save(@"pathing.png", ImageFormat.Png);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Seed = {SEED}");

        switch (args[0])
        {
            case "graph":
                DrawGraph(args);
                break;

            case "path":
                DrawPath(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }
}
