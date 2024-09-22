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

    public void AddEdge(GraphEdge edge)
    {
        edge.id = Edges.Count;

        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);
    }

    public void AddEdge(DelauneyPoint point1, DelauneyPoint point2)
    {
        if (point1.prefab == point2.prefab)
        {
            return;
        }

        var node1 = new GraphNode(point1.ToVector3i(0), point1.prefab);
        var node2 = new GraphNode(point2.ToVector3i(0), point2.prefab);

        var edge = new GraphEdge(Edges.Count, node1, node2);

        Edges.Add(edge);
        Nodes.Add(node1);
        Nodes.Add(node2);
    }

    private Graph PruneGraph()
    {
        var graph = new Graph();
        var groupedEdges = new Dictionary<int, List<GraphEdge>>();

        foreach (var edge in Edges)
        {
            int idx1 = edge.Prefab1.GetHashCode();
            int idx2 = edge.Prefab2.GetHashCode();

            int hashcode = idx1 ^ idx2;

            if (!groupedEdges.ContainsKey(hashcode))
            {
                groupedEdges[hashcode] = new List<GraphEdge>();
            }

            groupedEdges[hashcode].Add(edge);
        }

        foreach (var edgeGroup in groupedEdges.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            graph.AddEdge(shortestEdge);
        }

        return graph;
    }

    private static Graph BuildDelauneyGraph(List<CavePrefab> prefabs)
    {
        var points = prefabs.SelectMany(prefab => prefab.DelauneyPoints());
        var triangulator = new DelaunayTriangulator();
        var worldSize = CaveBuilder.worldSize;
        var graph = new Graph();

        foreach (var triangle in triangulator.BowyerWatson(points, worldSize, worldSize))
        {
            graph.AddEdge(triangle.Vertices[0], triangle.Vertices[1]);
            graph.AddEdge(triangle.Vertices[0], triangle.Vertices[2]);
            graph.AddEdge(triangle.Vertices[1], triangle.Vertices[2]);
        }

        return graph;
    }

    public static Graph Resolve(List<CavePrefab> prefabs)
    {

        var timer = CaveUtils.StartTimer();
        Graph graph = BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph : edges: {graph.Edges.Count}, nodes: {graph.Nodes.Count}");

        graph = graph.PruneGraph();

        Log.Out($"[Cave] secondary graph: edges: {graph.Edges.Count}, nodes: {graph.Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

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
