using System;


public class GraphEdge : IComparable<GraphEdge>
{
    public int id;

    public float Weight;

    public GraphNode node1;

    public GraphNode node2;

    public CavePrefab Prefab1 => node1.prefab;

    public CavePrefab Prefab2 => node2.prefab;

    public Vector3i center;

    public bool isVirtual = false;

    public string colorName = "DarkRed";

    public GraphEdge(GraphNode node1, GraphNode node2)
    {
        this.node1 = node1;
        this.node2 = node2;
        Weight = CaveUtils.SqrEuclidianDist(node1.position, node2.position);
        center = new Vector3i(
            (node1.position.x + node2.position.x) >> 1,
            (node1.position.y + node2.position.y) >> 1,
            (node1.position.z + node2.position.z) >> 1
        );
    }

    public GraphEdge(int id, GraphNode node1, GraphNode node2)
    {
        this.id = id;
        this.node1 = node1;
        this.node2 = node2;
        Weight = CaveUtils.SqrEuclidianDist(node1.position, node2.position);
        center = new Vector3i(
            (node1.position.x + node2.position.x) >> 1,
            (node1.position.y + node2.position.y) >> 1,
            (node1.position.z + node2.position.z) >> 1
        );
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
