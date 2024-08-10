// MIT License

// Copyright (c) 2018 Rafael Kübler da Silva
// https://github.com/RafaelKuebler/DelaunayVoronoi/tree/master

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


public class DelaunayTriangulator
{

    public IEnumerable<DelauneyTriangle> BowyerWatson(IEnumerable<DelauneyPoint> points, int xMax, int yMax)
    {
        var point0 = new DelauneyPoint(0, 0, 0, null);
        var point1 = new DelauneyPoint(0, 0, yMax, null);
        var point2 = new DelauneyPoint(xMax, 0, yMax, null);
        var point3 = new DelauneyPoint(xMax, 0, 0, null);

        var tri1 = new DelauneyTriangle(point0, point1, point2);
        var tri2 = new DelauneyTriangle(point0, point2, point3);

        var border = new HashSet<DelauneyTriangle>() { tri1, tri2 };
        var triangulation = new HashSet<DelauneyTriangle>(border);

        foreach (var point in points)
        {
            var badTriangles = FindBadTriangles(point, triangulation);
            var polygon = FindHoleBoundaries(badTriangles);

            foreach (var triangle in badTriangles)
            {
                foreach (var vertex in triangle.Vertices)
                {
                    vertex.AdjacentTriangles.Remove(triangle);
                }
            }
            triangulation.RemoveWhere(o => badTriangles.Contains(o));

            foreach (var edge in polygon.Where(possibleEdge => possibleEdge.Point1 != point && possibleEdge.Point2 != point))
            {
                var triangle = new DelauneyTriangle(point, edge.Point1, edge.Point2);
                triangulation.Add(triangle);
            }
        }

        triangulation.RemoveWhere(triangle => triangle.Vertices.Any(vertice => vertice.prefab == null));

        return triangulation;
    }

    private List<DelauneyEdge> FindHoleBoundaries(ISet<DelauneyTriangle> badTriangles)
    {
        var edges = new List<DelauneyEdge>();
        foreach (var triangle in badTriangles)
        {
            edges.Add(new DelauneyEdge(triangle.Vertices[0], triangle.Vertices[1]));
            edges.Add(new DelauneyEdge(triangle.Vertices[1], triangle.Vertices[2]));
            edges.Add(new DelauneyEdge(triangle.Vertices[2], triangle.Vertices[0]));
        }
        var grouped = edges.GroupBy(o => o);
        var boundaryEdges = edges.GroupBy(o => o).Where(o => o.Count() == 1).Select(o => o.First());
        return boundaryEdges.ToList();
    }

    private ISet<DelauneyTriangle> FindBadTriangles(DelauneyPoint point, HashSet<DelauneyTriangle> triangles)
    {
        var badTriangles = triangles.Where(o => o.IsPointInsideCircumcircle(point));
        return new HashSet<DelauneyTriangle>(badTriangles);
    }
}


public class DelauneyTriangle
{
    public DelauneyPoint[] Vertices { get; } = new DelauneyPoint[3];
    public DelauneyPoint Circumcenter { get; private set; }
    public double RadiusSquared;

    public IEnumerable<DelauneyTriangle> TrianglesWithSharedEdge
    {
        get
        {
            var neighbors = new HashSet<DelauneyTriangle>();
            foreach (var vertex in Vertices)
            {
                var trianglesWithSharedEdge = vertex.AdjacentTriangles.Where(o =>
                {
                    return o != this && SharesEdgeWith(o);
                });
                neighbors.UnionWith(trianglesWithSharedEdge);
            }

            return neighbors;
        }
    }

    public DelauneyTriangle(DelauneyPoint point1, DelauneyPoint point2, DelauneyPoint point3)
    {
        // In theory this shouldn't happen, but it was at one point so this at least makes sure we're getting a
        // relatively easily-recognised error message, and provides a handy breakpoint for debugging.
        if (point1 == point2 || point1 == point3 || point2 == point3)
        {
            throw new ArgumentException("Must be 3 distinct points");
        }

        if (!IsCounterClockwise(point1, point2, point3))
        {
            Vertices[0] = point1;
            Vertices[1] = point3;
            Vertices[2] = point2;
        }
        else
        {
            Vertices[0] = point1;
            Vertices[1] = point2;
            Vertices[2] = point3;
        }

        Vertices[0].AdjacentTriangles.Add(this);
        Vertices[1].AdjacentTriangles.Add(this);
        Vertices[2].AdjacentTriangles.Add(this);
        UpdateCircumcircle();
    }

    private void UpdateCircumcircle()
    {
        // https://codefound.wordpress.com/2013/02/21/how-to-compute-a-circumcircle/#more-58
        // https://en.wikipedia.org/wiki/Circumscribed_circle
        var p0 = Vertices[0];
        var p1 = Vertices[1];
        var p2 = Vertices[2];
        var dA = p0.X * p0.X + p0.Z * p0.Z;
        var dB = p1.X * p1.X + p1.Z * p1.Z;
        var dC = p2.X * p2.X + p2.Z * p2.Z;

        var aux1 = dA * (p2.Z - p1.Z) + dB * (p0.Z - p2.Z) + dC * (p1.Z - p0.Z);
        var aux2 = -(dA * (p2.X - p1.X) + dB * (p0.X - p2.X) + dC * (p1.X - p0.X));
        var div = 2 * (p0.X * (p2.Z - p1.Z) + p1.X * (p0.Z - p2.Z) + p2.X * (p1.Z - p0.Z));

        if (div == 0)
        {
            throw new DivideByZeroException();
        }

        var center = new DelauneyPoint(aux1 / div, 0, aux2 / div, null);
        Circumcenter = center;
        RadiusSquared = (center.X - p0.X) * (center.X - p0.X) + (center.Z - p0.Z) * (center.Z - p0.Z);
    }

    private bool IsCounterClockwise(DelauneyPoint point1, DelauneyPoint point2, DelauneyPoint point3)
    {
        var result = (point2.X - point1.X) * (point3.Z - point1.Z) -
            (point3.X - point1.X) * (point2.Z - point1.Z);
        return result > 0;
    }

    public bool SharesEdgeWith(DelauneyTriangle triangle)
    {
        var sharedVertices = Vertices.Where(o => triangle.Vertices.Contains(o)).Count();
        return sharedVertices == 2;
    }

    public bool IsPointInsideCircumcircle(DelauneyPoint point)
    {
        var d_squared = (point.X - Circumcenter.X) * (point.X - Circumcenter.X) +
            (point.Z - Circumcenter.Z) * (point.Z - Circumcenter.Z);
        return d_squared < RadiusSquared;
    }
}


public class DelauneyPoint
{
    /// <summary>
    /// Used only for generating a unique ID for each instance of this class that gets generated
    /// </summary>
    private static int _counter;

    /// <summary>
    /// Used for identifying an instance of a class; can be useful in troubleshooting when geometry goes weird
    /// (e.g. when trying to identify when DelauneyTriangle objects are being created with the same DelauneyPoint object twice)
    /// </summary>
    private readonly int _instanceId = _counter++;

    public readonly Vector3 position;

    public readonly CavePrefab prefab;

    public float X => position.X;

    public float Z => position.Z;

    public HashSet<DelauneyTriangle> AdjacentTriangles { get; } = new HashSet<DelauneyTriangle>();

    public DelauneyPoint(float x, float y, float z, CavePrefab prefab)
    {
        position = new Vector3(x, y, z);
        this.prefab = prefab;
    }

    public DelauneyPoint(CavePrefab prefab)
    {
        var center = prefab.GetCenter();
        position = new Vector3(center.x, center.y, center.z);
        this.prefab = prefab;
    }

    public override string ToString()
    {
        // Simple way of seeing what's going on in the debugger when investigating weirdness
        return $"{nameof(DelauneyPoint)} {_instanceId} {X:0.##}@{Z:0.##}";
    }

    public Vector3i ToVector3i(int y)
    {
        return new Vector3i((int)X, y, (int)Z);
    }

    public GraphNode ToGraphNode()
    {
        var pos = new Vector3i((int)position.X, (int)position.Y, (int)position.Z);
        return new GraphNode(pos, prefab);
    }
}


public class DelauneyEdge
{
    public DelauneyPoint Point1 { get; }
    public DelauneyPoint Point2 { get; }

    public DelauneyEdge(DelauneyPoint point1, DelauneyPoint point2)
    {
        Point1 = point1;
        Point2 = point2;
    }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (obj.GetType() != GetType()) return false;
        var edge = obj as DelauneyEdge;

        var samePoints = Point1 == edge.Point1 && Point2 == edge.Point2;
        var samePointsReversed = Point1 == edge.Point2 && Point2 == edge.Point1;
        return samePoints || samePointsReversed;
    }

    public override int GetHashCode()
    {
        int hCode = (int)Point1.X ^ (int)Point1.Z ^ (int)Point2.X ^ (int)Point2.Z;
        return hCode.GetHashCode();
    }
}
