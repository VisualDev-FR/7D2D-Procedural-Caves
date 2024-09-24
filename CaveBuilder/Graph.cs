using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Graph
{
    public HashSet<GraphEdge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<GraphNode, HashSet<GraphEdge>> relatedNodes;

    public Graph(List<CavePrefab> prefabs)
    {
        Edges = new HashSet<GraphEdge>();
        Nodes = new HashSet<GraphNode>();
        relatedNodes = new Dictionary<GraphNode, HashSet<GraphEdge>>();

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

        if (!relatedNodes.ContainsKey(edge.node1))
        {
            relatedNodes[edge.node1] = new HashSet<GraphEdge>();
        }

        if (!relatedNodes.ContainsKey(edge.node2))
        {
            relatedNodes[edge.node2] = new HashSet<GraphEdge>();
        }

        relatedNodes[edge.node1].Add(edge);
        relatedNodes[edge.node2].Add(edge);
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

        AddEdge(edge);
    }

    private void Prune()
    {
        var groupedEdges = new Dictionary<int, HashSet<GraphEdge>>();
        var groupedNodes = new Dictionary<GraphNode, HashSet<GraphEdge>>();
        var nodesBefore = Nodes.ToList();
        var edgesBefore = Edges.ToList();

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

            if (!groupedNodes.ContainsKey(edge.node1))
            {
                groupedNodes[edge.node1] = new HashSet<GraphEdge>();
            }

            if (!groupedNodes.ContainsKey(edge.node2))
            {
                groupedNodes[edge.node2] = new HashSet<GraphEdge>();
            }

            groupedNodes[edge.node1].Add(edge);
            groupedNodes[edge.node2].Add(edge);
        }

        // return;

        Edges.Clear();
        Nodes.Clear();

        foreach (var edgeGroup in groupedEdges.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            AddEdge(shortestEdge);
        }

        foreach (var node in nodesBefore.Where(node => !Nodes.Contains(node)))
        {
            var shortestEdge = edgesBefore
                .Where(edge => edge.node1 == node || edge.node2 == node)
                .OrderBy(edge => edge.Weight)
                .First();

            shortestEdge.colorName = "Purple";
            AddEdge(shortestEdge);

            var hash1 = shortestEdge.Prefab1.GetHashCode();
            var hash2 = shortestEdge.Prefab2.GetHashCode();
            var hashcode = hash1 ^ hash2;

            var replacedEdge = groupedEdges[hashcode]
                .Where(edge => Edges.Contains(edge) && edge != shortestEdge);

            foreach (var edge in replacedEdge)
            {
                edge.colorName = "Yellow";
                // Edges.Remove(edge);
            }
        }

        // TODO: fix this invalid condition. Search for the nodes wich are not linked to an edge instead
        Log.Out($"{nodesBefore.Where(node => !Nodes.Contains(node)).Count()} missing nodes.");
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
