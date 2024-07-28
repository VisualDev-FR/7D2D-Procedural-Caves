using System;
using System.Collections.Generic;
using System.Linq;

public class Graph
{
    public HashSet<Edge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<string, int> prefabsConnections;

    public Graph()
    {
        Edges = new HashSet<Edge>();
        Nodes = new HashSet<GraphNode>();
        prefabsConnections = new Dictionary<string, int>();
    }

    public int GetEdgeWeight(Edge edge)
    {
        prefabsConnections.TryGetValue(edge.HashPrefabs(), out int occurences);

        // const float distCoef = .5f;
        const float occurencesCoef = 2f;
        const float orientationCoef = 2f;

        double weight = 1d; // Math.Pow(edge.Weight, distCoef);

        weight *= Math.Pow(occurences + 1, occurencesCoef);
        weight *= Math.Pow(edge.GetOrientationWeight(), orientationCoef);

        return (int)weight;
    }

    public void AddPrefabConnection(Edge edge)
    {
        string hash = edge.HashPrefabs();

        if (!prefabsConnections.ContainsKey(hash))
            prefabsConnections[hash] = 0;

        prefabsConnections[hash] += 1;

        // Log.Out($"{hash}: {prefabsConnections[hash]}");
    }

    public void AddEdge(Edge edge)
    {
        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);
    }

    public List<Edge> GetEdgesFromNode(GraphNode node)
    {
        return Edges.Where(e => e.node1.Equals(node) || e.node2.Equals(node)).ToList();
    }

    public List<Edge> FindMST()
    {
        var graph = new List<Edge>();
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

    public static Graph BuildDelauneyGraph(List<CavePrefab> prefabs)
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

            var edges = new Edge[]
            {
                new Edge(node1, node2),
                new Edge(node1, node3),
                new Edge(node2, node3),
            };

            foreach (var edge in edges)
            {
                foreach (var nodeA in edge.Prefab1.nodes)
                {
                    foreach (var nodeB in edge.Prefab2.nodes)
                    {
                        graph.AddEdge(new Edge(nodeA, nodeB));
                    }
                }
            }
        }

        return graph;
    }

    public static List<Edge> Resolve(List<CavePrefab> prefabs)
    {
        var timer = CaveUtils.StartTimer();

        Graph graph = BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph: edges={graph.Edges.Count}, nodes={graph.Nodes.Count}");

        var edges = graph.FindMST();

        Log.Out($"Graph resolved in {CaveUtils.TimeFormat(timer)}, edges={edges.Count}");

        return edges; //graph.Edges.ToList();
    }
}
