using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Graph
{
    public HashSet<GraphEdge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<GraphNode, HashSet<GraphEdge>> relatedEdges;

    public Graph(List<CavePrefab> prefabs)
    {
        Edges = new HashSet<GraphEdge>();
        Nodes = new HashSet<GraphNode>();
        relatedEdges = new Dictionary<GraphNode, HashSet<GraphEdge>>();

        var timer = CaveUtils.StartTimer();

        BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph : edges: {Edges.Count}, nodes: {Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

        Prune();

        Log.Out($"[Cave] secondary graph: edges: {Edges.Count}, nodes: {Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

    }

    private void AddEdge(GraphEdge edge)
    {
        edge.id = Edges.Count;

        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);

        if (!relatedEdges.ContainsKey(edge.node1))
        {
            relatedEdges[edge.node1] = new HashSet<GraphEdge>();
        }

        if (!relatedEdges.ContainsKey(edge.node2))
        {
            relatedEdges[edge.node2] = new HashSet<GraphEdge>();
        }

        relatedEdges[edge.node1].Add(edge);
        relatedEdges[edge.node2].Add(edge);
    }

    private void RemoveEdge(GraphEdge edge)
    {
        relatedEdges[edge.node1].Remove(edge);
        relatedEdges[edge.node2].Remove(edge);

        if (relatedEdges[edge.node1].Count == 0)
        {
            relatedEdges.Remove(edge.node1);
            Nodes.Remove(edge.node1);
        }

        if (relatedEdges[edge.node2].Count == 0)
        {
            relatedEdges.Remove(edge.node2);
            Nodes.Remove(edge.node2);
        }

        Edges.Remove(edge);
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
        relatedEdges.Clear();

        foreach (var edgeGroup in groupedEdges.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            AddEdge(shortestEdge);
        }

        int notFound = 0;

        foreach (var node in nodesBefore.Where(node => !Nodes.Contains(node)))
        {
            var eligibleEdges = groupedNodes[node].OrderBy(edge => edge.Weight);
            var found = false;

            foreach (var edge in eligibleEdges)
            {
                edge.colorName = "Purple";

                // AddEdge(edge);
                // continue;

                if (TryReplaceEdge(edge, groupedEdges))
                {
                    found = true;
                    AddEdge(edge);
                    break;
                }
            }

            if(!found) notFound++;
        }

        Log.Out($"{notFound} nodes not found");
        Log.Out($"{nodesBefore.Where(node => !Nodes.Contains(node)).Count()} missing nodes.");
    }

    private bool TryReplaceEdge(GraphEdge edge, Dictionary<int, HashSet<GraphEdge>> groupedEdges)
    {
        var hash1 = edge.Prefab1.GetHashCode();
        var hash2 = edge.Prefab2.GetHashCode();

        var hashCode = hash1 ^ hash2;

        var sisterEdges = groupedEdges[hashCode].Where(_edge => Edges.Contains(_edge));

        if (sisterEdges.Count() == 0)
        {
            return true;
        }

        CaveUtils.Assert(sisterEdges.Count() == 1, $"{sisterEdges.Count()} edges");

        var sisterEdge = sisterEdges.First();

        bool cond1 = relatedEdges[sisterEdge.node1].Count > 1 || sisterEdge.node1 == edge.node1 || sisterEdge.node1 == edge.node2;
        bool cond2 = relatedEdges[sisterEdge.node2].Count > 1 || sisterEdge.node2 == edge.node1 || sisterEdge.node2 == edge.node2;

        if (cond1 && cond2)
        {
            sisterEdge.colorName = "Yellow";
            RemoveEdge(sisterEdge);
            return true;
        }

        return false;
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
