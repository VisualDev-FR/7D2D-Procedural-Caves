using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

public class Utils
{
    public static float FastMax(float value1, float value2)
    {
        return value1 > value2 ? value1 : value2;
    }

    public static Stopwatch StartTimer()
    {
        var timer = new Stopwatch();
        timer.Start();

        return timer;
    }
}

public struct Vector2
{
    public float x;

    public float y;

    public Vector2() { }

    public Vector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(
            a.x + b.x,
            a.y + b.y
        );
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(
            a.x - b.x,
            a.y - b.y
        );
    }

    public static Vector2 operator *(Vector2 a, Vector2 b)
    {
        return new Vector2(
            a.x * b.x,
            a.y * b.y
        );
    }

    public static Vector2 operator *(Vector2 a, float value)
    {
        return new Vector2(
            a.x * value,
            a.y * value
        );
    }

    public static Vector2 operator *(float value, Vector2 a)
    {
        return new Vector2(
            a.x * value,
            a.y * value
        );
    }

    public static Vector2 operator /(Vector2 a, float value)
    {
        return new Vector2(
            a.x / value,
            a.y / value
        );
    }

    public float length_squared()
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L48
        return x * x + y * y;
    }

    public Vector2 orthogonal()
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.h#L171
        return new Vector2(y, -x);
    }

    public float cross(Vector2 other)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L92
        return x * other.y - y * other.x;
    }

    public float distance_squared_to(Vector2 p_vector2)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L76

        var dx = x - p_vector2.x;
        var dy = y - p_vector2.y;

        return dx * dx + dy * dy;

    }

    public override string ToString()
    {
        return $"{x}, {y}";
    }
}

public class Delaunay2D
{
    public class Triangle
    {
        // https://github.com/godotengine/godot/blob/master/core/math/delaunay_2d.h#L38
        public int[] points = new int[3];

        public Vector2 circum_center;

        public float circum_radius_squared;

        public Triangle(int p_a, int p_b, int p_c)
        {
            points[0] = p_a;
            points[1] = p_b;
            points[2] = p_c;
        }

        public IEnumerable<Edge> GetEdges()
        {
            yield return new Edge(points[0], points[1]);
            yield return new Edge(points[0], points[2]);
            yield return new Edge(points[1], points[2]);
        }
    }

    public class Edge
    {
        public int[] points = new int[2];

        public bool bad = false;

        public Edge(int p_a, int p_b)
        {
            // Store indices in a sorted manner to avoid having to check both orientations later.
            if (p_a > p_b)
            {
                points[0] = p_b;
                points[1] = p_a;
            }
            else
            {
                points[0] = p_a;
                points[1] = p_b;
            }
        }
    }

    public struct Rect2
    {
        public Vector2 position;

        public Vector2 size;

        public Rect2(Vector2 point, Vector2 size)
        {
            this.position = point;
            this.size = size;
        }

        public void ExpandTo(Vector2 p_vector)
        {
            Vector2 begin = position;
            Vector2 end = position + size;

            if (p_vector.x < begin.x)
                begin.x = p_vector.x;

            if (p_vector.y < begin.y)
                begin.y = p_vector.y;

            if (p_vector.x > end.x)
                end.x = p_vector.x;

            if (p_vector.y > end.y)
                end.y = p_vector.y;

            position = begin;
            size = end - begin;
        }

        public Vector2 GetCenter()
        {
            return new Vector2(
                position.x + size.x / 2,
                position.y + size.y / 2
            );
        }
    }

    private static Triangle CreateTriangle(List<Vector2> p_vertices, int p_a, int p_b, int p_c)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/delaunay_2d.h#L66

        Triangle triangle = new Triangle(p_a, p_b, p_c);

        // Get the values of the circumcircle and store them inside the triangle object.
        Vector2 a = p_vertices[p_b] - p_vertices[p_a];
        Vector2 b = p_vertices[p_c] - p_vertices[p_a];

        Vector2 O = (b * a.length_squared() - a * b.length_squared()).orthogonal() / (a.cross(b) * 2.0f);

        triangle.circum_radius_squared = O.length_squared();
        triangle.circum_center = O + p_vertices[p_a];

        return triangle;
    }

    public static List<Triangle> Triangulate(List<Vector2> p_points, int worldSize)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/delaunay_2d.h#L81

        var points = new List<Vector2>(p_points);
        var triangles = new List<Triangle>();
        var point_count = p_points.Count;

        if (point_count <= 2)
            return triangles;

        // Construct a bounding triangle around the rectangle.
        points.Add(new Vector2(0, 0));
        points.Add(new Vector2(0, worldSize));
        points.Add(new Vector2(worldSize, 0));
        points.Add(new Vector2(worldSize, worldSize));

        var tri1 = CreateTriangle(points, point_count + 0, point_count + 1, point_count + 2);
        var tri2 = CreateTriangle(points, point_count + 1, point_count + 2, point_count + 3);

        triangles.Add(tri1);
        triangles.Add(tri2);

        for (int i = 0; i < point_count; i++)
        {
            var polygon = new List<Edge>();

            // Save the edges of the triangles whose circumcircles contain the i-th vertex. Delete the triangles themselves.
            for (int j = triangles.Count - 1; j >= 0; j--)
            {
                if (points[i].distance_squared_to(triangles[j].circum_center) < triangles[j].circum_radius_squared)
                {
                    polygon.Add(new Edge(triangles[j].points[0], triangles[j].points[1]));
                    polygon.Add(new Edge(triangles[j].points[1], triangles[j].points[2]));
                    polygon.Add(new Edge(triangles[j].points[2], triangles[j].points[0]));

                    triangles.RemoveAt(j);
                }
            }

            // Create a triangle for every unique edge.
            for (int j = 0; j < polygon.Count; j++)
            {
                if (polygon[j].bad)
                    continue;

                for (int k = j + 1; k < polygon.Count; k++)
                {
                    // Compare the edges.
                    if (polygon[k].points[0] == polygon[j].points[0] && polygon[k].points[1] == polygon[j].points[1])
                    {
                        polygon[j].bad = true;
                        polygon[k].bad = true;

                        break; // Since no more than two triangles can share an edge, no more than two edges can share vertices.
                    }
                }

                // Create triangles out of good edges.
                if (!polygon[j].bad)
                {
                    triangles.Add(CreateTriangle(points, polygon[j].points[0], polygon[j].points[1], i));
                }
            }
        }

        // Filter out the triangles containing vertices of the bounding triangle.
        int preservedCount = 0;
        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle tri = triangles[i];

            if (!(tri.points[0] >= point_count || tri.points[1] >= point_count || tri.points[2] >= point_count))
            {
                triangles[preservedCount] = tri;
                preservedCount++;
            }
        }

        // Supprime les triangles restants en trop
        while (triangles.Count > preservedCount)
        {
            triangles.RemoveAt(triangles.Count - 1);
        }

        return triangles;
    }

}

public class Program
{
    public const int worldSize = 800;

    public const int pointsCount = 50;

    public const int pointRadius = 2;

    public static readonly Color backgroundColor = Color.Black;

    public static readonly Color verticeColor = Color.Yellow;

    public static readonly Color edgeColor = Color.Red;

    public static readonly Random random = new Random();

    public static void Main(string[] args)
    {
        var timer = Utils.StartTimer();
        var points = GetRandomPoints().ToList();
        var triangles = Delaunay2D.Triangulate(points, worldSize);
        var edges = triangles.SelectMany(tri => tri.GetEdges()).ToList();

        var edgePen = new Pen(edgeColor) { Width = 2f };
        var verticePen = new Pen(verticeColor) { Width = 2f };

        Console.WriteLine($"points:    {points.Count}");
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

    private static IEnumerable<Vector2> GetRandomPoints()
    {
        for (int i = 0; i < pointsCount; i++)
        {
            yield return new Vector2(
                random.Next(worldSize),
                random.Next(worldSize)
            );
        }
    }
}
