using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Graph
{
    public HashSet<GraphEdge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<string, int> prefabsConnections;

    public Graph(List<CavePrefab> prefabs)
    {
        Edges = new HashSet<GraphEdge>();
        Nodes = new HashSet<GraphNode>();
        prefabsConnections = new Dictionary<string, int>();

        var timer = CaveUtils.StartTimer();

        BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph : edges: {Edges.Count}, nodes: {Nodes.Count}");

        Prune();

        Log.Out($"[Cave] secondary graph: edges: {Edges.Count}, nodes: {Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

    }

    private void AddEdge(GraphEdge edge)
    {
        edge.id = Edges.Count;

        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);
    }

    private void AddEdge(DelauneyPoint point1, DelauneyPoint point2)
    {
        if (point1.prefab == point2.prefab)
        {
            return;
        }

        var node1 = new GraphNode(point1.marker, point1.prefab);
        var node2 = new GraphNode(point2.marker, point2.prefab);

        var edge = new GraphEdge(Edges.Count, node1, node2);

        Edges.Add(edge);
        Nodes.Add(node1);
        Nodes.Add(node2);
    }

    private void Prune()
    {
        var groupedEdges = new Dictionary<int, HashSet<GraphEdge>>();
        var groupedNodes = new Dictionary<int, HashSet<GraphEdge>>();
        var nodesBefore = Nodes.ToList();

        foreach (var edge in Edges)
        {
            int hash1 = edge.Prefab1.GetHashCode();
            int hash2 = edge.Prefab2.GetHashCode();

            int hashcode = hash1 ^ hash2;

            if (!groupedEdges.ContainsKey(hashcode))
            {
                groupedEdges[hashcode] = new HashSet<GraphEdge>();
            }
            groupedEdges[hashcode].Add(edge);

            var nodeHash1 = edge.node1.GetHashCode();
            var nodeHash2 = edge.node2.GetHashCode();

            if (!groupedNodes.ContainsKey(nodeHash1))
            {
                groupedNodes[nodeHash1] = new HashSet<GraphEdge>();
            }

            if (!groupedNodes.ContainsKey(nodeHash2))
            {
                groupedNodes[nodeHash2] = new HashSet<GraphEdge>();
            }

            groupedNodes[nodeHash1].Add(edge);
            groupedNodes[nodeHash2].Add(edge);
        }

        return;

        Edges.Clear();
        Nodes.Clear();

        foreach (var edgeGroup in groupedEdges.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            AddEdge(shortestEdge);
        }
    }

    private void BuildDelauneyGraph(List<CavePrefab> prefabs)
    {
        var points = prefabs.SelectMany(prefab => prefab.DelauneyPoints());
        var triangulator = new DelaunayTriangulator();
        var worldSize = CaveBuilder.worldSize;

        foreach (var triangle in triangulator.BowyerWatson(points, worldSize, worldSize))
        {
            AddEdge(triangle.Vertices[0], triangle.Vertices[1]);
            AddEdge(triangle.Vertices[0], triangle.Vertices[2]);
            AddEdge(triangle.Vertices[1], triangle.Vertices[2]);
        }
    }

    public void Save(string filename)
    {
        var edgesCount = Edges.Count(edge => edge.node1.prefab.prefabDataInstance != null && edge.node2.prefab.prefabDataInstance != null);

        using (var stream = new StreamWriter(filename))
        {
            stream.WriteLine(edgesCount);

            foreach (var edges in Edges)
            {
                var pdi1 = edges.node1.prefab.prefabDataInstance;
                var pdi2 = edges.node2.prefab.prefabDataInstance;

                // TODO: filter cave rooms prefabs to avoid that ugly fix
                if (pdi1 == null || pdi2 == null)
                {
                    continue;
                }

                stream.WriteLine(edges.id);
                stream.WriteLine($"{pdi1.id}");
                stream.WriteLine($"{pdi2.id}");
                stream.WriteLine($"{pdi1.prefab.Name}");
                stream.WriteLine($"{pdi2.prefab.Name}");
            }
        }
    }

}
