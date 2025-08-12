using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;


public class CmdDelaunay : CmdAbstract
{
    public const int worldSize = 800;

    public const int pointsCount = 50;

    public const int pointRadius = 2;

    public static readonly int seed = new Random().Next(1000);

    public static readonly Color backgroundColor = Color.Black;

    public static readonly Color verticeColor = Color.Yellow;

    public static readonly Color edgeColor = Color.Red;

    public static readonly Random random = new Random(seed);

    public override string[] GetCommands()
    {
        return new string[] { "delaunay" };
    }

    public override void Execute(List<string> args)
    {
        Console.WriteLine($"seed:      {seed}");

        var timer = ProfilingUtils.StartTimer();
        var points = GetRandomPoints().Select(p => p.position).ToArray();
        var triangles = DelaunayTriangulator.Triangulate(points, worldSize);
        var edges = triangles.SelectMany(tri => tri.GetEdges()).ToList();

        var edgePen = new Pen(edgeColor) { Width = 2f };
        var verticePen = new Pen(verticeColor) { Width = 2f };

        Console.WriteLine($"points:    {points.Length}");
        Console.WriteLine($"triangles: {triangles.Count}");
        Console.WriteLine($"edges:     {edges.Count}");
        Console.WriteLine($"timer:     {timer.ElapsedMilliseconds}ms");

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(backgroundColor);

                foreach (var edge in edges)
                {
                    var p1 = points[edge.points[0]];
                    var p2 = points[edge.points[1]];

                    g.DrawLine(edgePen, p1.x, p1.y, p2.x, p2.y);
                }

                foreach (var point in points)
                {
                    g.DrawEllipse(verticePen, (int)point.x, (int)point.y, pointRadius, pointRadius);
                }
            }

            b.Save("ignore/delaunay.png", ImageFormat.Png);
        }
    }

    private IEnumerable<DelaunayPoint> GetRandomPoints()
    {
        for (int i = 0; i < pointsCount; i++)
        {
            yield return new DelaunayPoint(
                random.Next(worldSize),
                random.Next(worldSize)
            );
        }
    }
}
