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
using UnityEngine;

// TODO: see godot implementation of delaunay triangulation: https://github.com/godotengine/godot/blob/master/core/math/delaunay_2d.h
public class DelaunayTriangulator
{
    public IEnumerable<DelauneyTriangle> BowyerWatson(IEnumerable<DelauneyPoint> points, int xMax, int yMax)
    {
        var point0 = new DelauneyPoint(0, 0, 0);
        var point1 = new DelauneyPoint(0, 0, yMax);
        var point2 = new DelauneyPoint(xMax, 0, yMax);
        var point3 = new DelauneyPoint(xMax, 0, 0);

        var tri1 = new DelauneyTriangle(point0, point1, point2);
        var tri2 = new DelauneyTriangle(point0, point2, point3);

        var triangles = new HashSet<DelauneyTriangle>() { tri1, tri2 };

        foreach (var point in points)
        {
            var badTriangles = FindBadTriangles(point, triangles);
            var polygon = FindHoleBoundaries(badTriangles);

            foreach (var triangle in badTriangles)
            {
                foreach (var vertex in triangle.Vertices)
                {
                    vertex.AdjacentTriangles.Remove(triangle);
                }
            }
            triangles.RemoveWhere(t => badTriangles.Contains(t));

            foreach (var edge in polygon)
            {
                var triangle = new DelauneyTriangle(point, edge.Point1, edge.Point2);

                triangles.Add(triangle);
            }
        }

        triangles.RemoveWhere(triangle => triangle.Vertices.Any(vertice => vertice.node is null));

        return triangles;
    }

    private IEnumerable<DelauneyEdge> FindHoleBoundaries(HashSet<DelauneyTriangle> badTriangles)
    {
        return badTriangles
            .SelectMany(t => t.Edges)
            .GroupBy(e => e)
            .Where(e => e.Count() == 1)
            .Select(e => e.First());
    }

    private HashSet<DelauneyTriangle> FindBadTriangles(DelauneyPoint point, HashSet<DelauneyTriangle> triangles)
    {
        return triangles
            .Where(t => t.IsPointInsideCircumcircle(point))
            .ToHashSet();
    }
}

public class DelauneyTriangle
{
    public DelauneyPoint[] Vertices { get; } = new DelauneyPoint[3];

    public DelauneyEdge[] Edges { get; } = new DelauneyEdge[3];

    public DelauneyPoint Circumcenter { get; private set; }

    public double RadiusSquared;

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

        Edges = new DelauneyEdge[]
        {
            new DelauneyEdge(Vertices[0], Vertices[1]),
            new DelauneyEdge(Vertices[0], Vertices[2]),
            new DelauneyEdge(Vertices[1], Vertices[2]),
        };

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
            Log.Out($"[Cave] {p0.Prefab.id}, {p1.Prefab.id}, {p2.Prefab.id}");
            Log.Out($"[Cave] {p0.Prefab.PrefabName}, {p1.Prefab.PrefabName}, {p2.Prefab.PrefabName}");
            Log.Out($"[Cave] {p0.position}, {p1.position}, {p2.position}");
            throw new DivideByZeroException();
        }

        var center = new DelauneyPoint(aux1 / div, 0, aux2 / div);
        Circumcenter = center;
        RadiusSquared = (center.X - p0.X) * (center.X - p0.X) + (center.Z - p0.Z) * (center.Z - p0.Z);
    }

    private bool IsCounterClockwise(DelauneyPoint point1, DelauneyPoint point2, DelauneyPoint point3)
    {
        var result = (point2.X - point1.X) * (point3.Z - point1.Z) - (point3.X - point1.X) * (point2.Z - point1.Z);
        return result > 0;
    }

    public bool IsPointInsideCircumcircle(DelauneyPoint point)
    {
        var dx = point.X - Circumcenter.X;
        var dz = point.Z - Circumcenter.Z;

        var d_squared = dx * dx + dz * dz;

        return d_squared < RadiusSquared;
    }
}


public class DelauneyPoint
{
    public HashSet<DelauneyTriangle> AdjacentTriangles { get; } = new HashSet<DelauneyTriangle>();

    public readonly Vector3 position;

    public float X => position.x;

    public float Z => position.z;

    public GraphNode node;

    public Prefab.Marker Marker => node.marker;

    public CavePrefab Prefab => node.prefab;

    public DelauneyPoint(GraphNode node)
    {
        this.node = node;
        this.position = node.position.ToVector3();
    }

    public DelauneyPoint(float x, float y, float z)
    {
        position = new Vector3(x, y, z);
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
        if (obj is DelauneyEdge other)
        {
            return other.GetHashCode() == GetHashCode();
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Point1.GetHashCode() ^ Point2.GetHashCode();
    }
}
