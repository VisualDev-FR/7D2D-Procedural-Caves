using System;
using System.Collections.Generic;
using System.IO;
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

    public void AddEdge(GraphEdge edge)
    {
        edge.id = Edges.Count;

        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);
    }

    private List<GraphEdge> GetEdgesFromNode(GraphNode node)
    {
        return Edges.Where(e => e.node1.Equals(node) || e.node2.Equals(node)).ToList();
    }

    private Graph FindMST()
    {
        var graph = new Graph();

        foreach (var node in Nodes)
        {
            if (graph.Nodes.Contains(node))
                continue;

            var relatedEdges = GetEdgesFromNode(node);

            relatedEdges.Sort(new EdgeWeightComparer(this));

            graph.AddEdge(relatedEdges[0]);

            AddPrefabConnection(relatedEdges[0]);
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

    public static Graph Resolve(List<CavePrefab> prefabs)
    {
        var timer = CaveUtils.StartTimer();

        Graph graph = BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph: edges={graph.Edges.Count}, nodes={graph.Nodes.Count}");

        graph = graph.FindMST();

        Log.Out($"Graph resolved in {CaveUtils.TimeFormat(timer)}, edges={graph.Edges.Count}");

        return graph;
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
