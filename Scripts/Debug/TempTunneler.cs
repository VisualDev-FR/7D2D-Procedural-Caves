using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Collections.Generic;


public class Point
{
    public int x;

    public int y;

    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public PointF ToPointF()
    {
        return new PointF(x, y);
    }
}

public class Edge : IComparable<Edge>
{
    public int Start { get; set; }

    public int End { get; set; }

    public float Weight { get; set; }

    public Point StartPoint { get; }

    public Point EndPoint { get; }

    private float GetWeight()
    {
        return MathF.Sqrt(
              MathF.Pow(StartPoint.x - EndPoint.x, 2)
            + MathF.Pow(StartPoint.y - EndPoint.y, 2)
        );
    }

    public Edge(int _start, int _end, Point _startPoint, Point _endPoint)
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
public static class Program
{
    static readonly int SEED = new Random().Next();

    const int SIZE = 200;

    const float POINT_WIDTH = SIZE / 200;

    const float EDGE_WIDTH = SIZE / 2000;

    const int POINTS_COUNT = SIZE / 5;

    static readonly Random rand = new Random(SEED);

    static readonly Color POINT_COLOR = Color.Red;

    static readonly Color EDGE_COLOR = Color.Black;

    private static FastNoiseLite GetNoise()
    {
        var noise = new FastNoiseLite(SEED);

        return noise;
    }

    private static List<Point> GetPoints(int count)
    {
        var points = new List<Point>();

        for (int i = 0; i < count; i++)
        {
            points.Add(new Point(rand.Next(0, SIZE), rand.Next(0, SIZE)));
        }

        return points;
    }

    public static void DrawPoints(Graphics graph, List<Point> points)
    {
        using Pen pen = new Pen(POINT_COLOR, POINT_WIDTH);

        foreach (var point in points)
        {
            graph.DrawEllipse(pen, point.x, point.y, 1, 1);
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

    public static List<Edge> KruskalMST(List<Point> points)
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

        var points = GetPoints(pointCounts);

        List<Edge> edges = KruskalMST(points);

        using Bitmap b = new Bitmap(SIZE, SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.White);
            DrawEdges(g, edges);
            DrawPoints(g, points);
        }

        b.Save(@"graph.png", ImageFormat.Png);
    }

    private static List<Edge> WormPathing(Point startPoint, Point endPoint)
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
            int yi = startPoint.y + (endPoint.y - startPoint.y) * i / n;

            xi += rand.Next(amplitude);
            yi += rand.Next(amplitude);

            var interPoint = new Point(xi, yi);

            edges.Add(new Edge(0, 0, lastPoint, interPoint));

            lastPoint = interPoint;
        }

        edges.Add(new Edge(0, 0, lastPoint, endPoint));

        return edges;
    }

    private static void DrawPath(string[] args)
    {
        List<Point> points = new List<Point>{
            new Point(10, 10),
            new Point(SIZE - 10, SIZE - 10),
        };

        List<Edge> edges = WormPathing(points[0], points[1]);

        using Bitmap b = new Bitmap(SIZE, SIZE);

        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.White);
            DrawEdges(g, edges);
            DrawPoints(g, points);
        }

        b.Save(@"pathing.png", ImageFormat.Png);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Seed = {SEED}");

        DrawPath(args);

        // switch (args[0])
        // {
        //     case "graph":
        //         DrawGraph(args);
        //         break;

        //     case "path":
        //         DrawPath(args);
        //         break;

        //     default:
        //         Console.WriteLine($"Invalid command: {args[0]}");
        //         break;
        // }
    }
}
