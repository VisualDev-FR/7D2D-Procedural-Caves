using System;
using System.Collections.Generic;


public class GraphEdge : IComparable<GraphEdge>
{
    public int id;

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

    public int GetOrientationWeight()
    {
        Vector3i p1 = node1.position;
        Vector3i p2 = node2.position;

        var segment = new Segment(p1, p2);

        int result = Prefab1.CountIntersections(segment) + Prefab2.CountIntersections(segment);

        return result + 1;
    }

    public GraphEdge(GraphNode node1, GraphNode node2)
    {
        this.node1 = node1;
        this.node2 = node2;
        Weight = CaveUtils.SqrEuclidianDist(node1.position, node2.position);
    }

    public GraphEdge(int id, GraphNode node1, GraphNode node2)
    {
        this.id = id;
        this.node1 = node1;
        this.node2 = node2;
        Weight = CaveUtils.SqrEuclidianDist(node1.position, node2.position);
    }

    public int CompareTo(GraphEdge other)
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
}


public class EdgeWeightComparer : IComparer<GraphEdge>
{
    private readonly Graph _graph;

    public EdgeWeightComparer(Graph graph)
    {
        _graph = graph;
    }

    public int Compare(GraphEdge x, GraphEdge y)
    {
        if (x == null || y == null)
        {
            throw new ArgumentException("Comparing null objects is not supported.");
        }

        return _graph.GetEdgeWeight(x).CompareTo(_graph.GetEdgeWeight(y));
    }
}
