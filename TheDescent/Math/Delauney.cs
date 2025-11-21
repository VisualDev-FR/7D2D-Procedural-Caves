// ***********************************************************************
//  https://github.com/godotengine/godot/blob/master/core/math/delaunay_2d.h
//
// ***********************************************************************
//                         This file is part of:
//                             GODOT ENGINE
//                        https://godotengine.org
// ***********************************************************************
// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using UnityEngine;


public class DelaunayPoint
{
    public readonly Vector2 position;

    public readonly CavePrefab prefab;

    public readonly GraphNode node;

    public DelaunayPoint(GraphNode node)
    {
        this.position = new Vector2(node.position.x, node.position.z);
        this.prefab = node.prefab;
        this.node = node;
    }

    public DelaunayPoint(int px, int pz)
    {
        this.position = new Vector2(px, pz);
        this.prefab = null;
        this.node = null;
    }
}


public class DelaunayTriangulator
{
    public class Triangle
    {
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
        Triangle triangle = new Triangle(p_a, p_b, p_c);

        // Get the values of the circumcircle and store them inside the triangle object.
        Vector2 a = p_vertices[p_b] - p_vertices[p_a];
        Vector2 b = p_vertices[p_c] - p_vertices[p_a];

        Vector2 O = orthogonal(b * length_squared(a) - a * length_squared(b)) / (cross(a, b) * 2.0f);

        triangle.circum_radius_squared = length_squared(O);
        triangle.circum_center = O + p_vertices[p_a];

        return triangle;
    }

    public static List<Triangle> Triangulate(Vector2[] p_points, int worldSize)
    {
        var points = new List<Vector2>(p_points);
        var triangles = new List<Triangle>();
        var point_count = p_points.Length;

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
                if (distance_squared_to(points[i], triangles[j].circum_center) < triangles[j].circum_radius_squared)
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

        // Delete the remaining extra triangles
        while (triangles.Count > preservedCount)
        {
            triangles.RemoveAt(triangles.Count - 1);
        }

        return triangles;
    }

    public static float distance_squared_to(Vector2 p1, Vector2 p2)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L76

        var dx = p1.x - p2.x;
        var dy = p1.y - p2.y;

        return dx * dx + dy * dy;

    }

    public static float length_squared(Vector2 vec)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L48

        return vec.x * vec.x + vec.y * vec.y;
    }

    public static float cross(Vector2 p1, Vector2 p2)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.cpp#L92

        return p1.x * p2.y - p1.y * p2.x;
    }

    public static Vector2 orthogonal(Vector2 vec)
    {
        // https://github.com/godotengine/godot/blob/master/core/math/vector2.h#L171

        return new Vector2(vec.y, -vec.x);
    }
}
