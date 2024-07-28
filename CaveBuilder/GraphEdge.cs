using System;
using System.Collections.Generic;


public class Edge : IComparable<Edge>
{
    public float Weight;

    public GraphNode node1;

    public GraphNode node2;

    public CavePrefab Prefab1 => node1.prefab;

    public CavePrefab Prefab2 => node2.prefab;

    public string HashPrefabs()
    {
        int index1 = CaveUtils.FastMin(Prefab1.id, Prefab2.id);
        int index2 = CaveUtils.FastMax(Prefab1.id, Prefab2.id);

        return $"{index1};{index2}";
    }

    private float GetWeight()
    {
        // return CaveUtils.SqrEuclidianDist2D(node1, node2) / CaveUtils.FastAbs(node1.y - node2.y);
        return CaveUtils.SqrEuclidianDist(node1.position, node2.position);
    }

    public int GetOrientationWeight()
    {
        Vector3i p1 = node1.position;
        Vector3i p2 = node2.position;

        var segment = new Segment(p1, p2);

        int result = Prefab1.CountIntersections(segment) + Prefab2.CountIntersections(segment);

        return result + 1;
    }

    public Edge(GraphNode node1, GraphNode node2)
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

    public string ToWaveFront(ref int vertexOffset)
    {
        var wavefront = new List<String>();

        var pointA = node1.position;
        var pointB = node2.position;

        // Write vertices
        wavefront.Add($"v {pointA.x} {pointA.y} {pointA.z}");
        wavefront.Add($"v {pointB.x} {pointB.y} {pointB.z}");

        // Write edges
        wavefront.Add($"l {vertexOffset} {vertexOffset + 1}");

        vertexOffset += 2; // Each tetrahedron adds 4 new vertices

        return string.Join("\n", wavefront);
    }
}


public class EdgeWeightComparer : IComparer<Edge>
{
    private readonly Graph _graph;

    public EdgeWeightComparer(Graph graph)
    {
        _graph = graph;
    }

    public int Compare(Edge x, Edge y)
    {
        if (x == null || y == null)
        {
            throw new ArgumentException("Comparing null objects is not supported.");
        }

        return _graph.GetEdgeWeight(x).CompareTo(_graph.GetEdgeWeight(y));
    }
}
