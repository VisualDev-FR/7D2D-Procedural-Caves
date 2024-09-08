using System;
using System.Collections.Generic;
using System.Linq;

public class Graph
{
    public HashSet<GraphEdge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<string, int> prefabsConnections;

    public Graph()
    {
        Edges = new HashSet<GraphEdge>();
        Nodes = new HashSet<GraphNode>();
        prefabsConnections = new Dictionary<string, int>();
    }

    public int GetEdgeWeight(GraphEdge edge)
    {
        prefabsConnections.TryGetValue(edge.HashPrefabs(), out int occurences);

        const float occurencesCoef = 2f;
        const float orientationCoef = 2f;

        double weight = 1d; // Math.Pow(edge.Weight, distCoef);

        weight *= Math.Pow(occurences + 1, occurencesCoef);
        weight *= Math.Pow(edge.GetOrientationWeight(), orientationCoef);

        return (int)weight;
    }

    public void AddPrefabConnection(GraphEdge edge)
    {
        string hash = edge.HashPrefabs();

        if (!prefabsConnections.ContainsKey(hash))
            prefabsConnections[hash] = 0;

        prefabsConnections[hash] += 1;

        // Log.Out($"{hash}: {prefabsConnections[hash]}");
    }

    public void AddEdge(GraphNode node1, GraphNode node2)
    {
        var edge = new GraphEdge(Edges.Count, node1, node2);

        Edges.Add(edge);
        Nodes.Add(node1);
        Nodes.Add(node2);
    }

    private List<GraphEdge> GetEdgesFromNode(GraphNode node)
    {
        return Edges.Where(e => e.node1.Equals(node) || e.node2.Equals(node)).ToList();
    }

    private List<GraphEdge> FindMST()
    {
        var graph = new List<GraphEdge>();
        var nodes = new HashSet<GraphNode>();

        foreach (var node in Nodes)
        {
            if (nodes.Contains(node))
                continue;

            var relatedEdges = GetEdgesFromNode(node);

            // CaveUtils.Assert(node.direction != Direction.None);

            relatedEdges.Sort(new EdgeWeightComparer(this));

            graph.Add(relatedEdges[0]);

            nodes.Add(relatedEdges[0].node1);
            nodes.Add(relatedEdges[0].node1);

            AddPrefabConnection(relatedEdges[0]);

            // Log.Out(prefabsConnections[relatedEdges[0].HashPrefabs()].ToString());
        }

        return graph;
    }

    private static Graph BuildDelauneyGraph(List<CavePrefab> prefabs)
    {
        var points = prefabs.Select((prefab) => new DelauneyPoint(prefab));
        var triangulator = new DelaunayTriangulator();
        var worldSize = CaveBuilder.worldSize;
        var graph = new Graph();

        foreach (var triangle in triangulator.BowyerWatson(points, worldSize, worldSize))
        {
            var node1 = triangle.Vertices[0].ToGraphNode();
            var node2 = triangle.Vertices[1].ToGraphNode();
            var node3 = triangle.Vertices[2].ToGraphNode();

            var edges = new GraphEdge[]
            {
                new GraphEdge(node1, node2),
                new GraphEdge(node1, node3),
                new GraphEdge(node2, node3),
            };

            foreach (var edge in edges)
            {
                foreach (var nodeA in edge.Prefab1.nodes)
                {
                    foreach (var nodeB in edge.Prefab2.nodes)
                    {
                        graph.AddEdge(nodeA, nodeB);
                    }
                }
            }
        }

        return graph;
    }

    public static List<GraphEdge> Resolve(List<CavePrefab> prefabs)
    {
        var timer = CaveUtils.StartTimer();

        Graph graph = BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph: edges={graph.Edges.Count}, nodes={graph.Nodes.Count}");

        var edges = graph.FindMST();

        Log.Out($"Graph resolved in {CaveUtils.TimeFormat(timer)}, edges={edges.Count}");

        return edges; //graph.Edges.ToList();
    }

}
