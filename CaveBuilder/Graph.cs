using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class Graph
{
    public HashSet<GraphEdge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<GraphNode, HashSet<GraphEdge>> relatedEdges;

    public Dictionary<int, HashSet<GraphEdge>> relatedPrefabs;

    public Graph(List<CavePrefab> prefabs)
    {
        Edges = new HashSet<GraphEdge>();
        Nodes = new HashSet<GraphNode>();
        relatedEdges = new Dictionary<GraphNode, HashSet<GraphEdge>>();
        relatedPrefabs = new Dictionary<int, HashSet<GraphEdge>>();

        var timer = CaveUtils.StartTimer();

        BuildDelauneyGraph(prefabs);

        Log.Out($"[Cave] primary graph : edges: {Edges.Count}, nodes: {Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

        Prune();

        Log.Out($"[Cave] secondary graph: edges: {Edges.Count}, nodes: {Nodes.Count}, timer: {timer.ElapsedMilliseconds:N0}ms");

    }

    private Graph(Graph other)
    {
        relatedPrefabs = new Dictionary<int, HashSet<GraphEdge>>();
        relatedEdges = new Dictionary<GraphNode, HashSet<GraphEdge>>();
        Nodes = other.Nodes.ToHashSet();
        Edges = other.Edges.ToHashSet();

        other.relatedPrefabs.CopyTo(relatedPrefabs);
        other.relatedEdges.CopyTo(relatedEdges);
    }

    private void Clear()
    {
        Edges.Clear();
        Nodes.Clear();
        relatedEdges.Clear();
        relatedPrefabs.Clear();
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

        var prefabHash = edge.PrefabHash;

        if (!relatedPrefabs.ContainsKey(prefabHash))
        {
            relatedPrefabs[prefabHash] = new HashSet<GraphEdge>();
        }

        relatedPrefabs[prefabHash].Add(edge);
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

        var prefabHash = edge.PrefabHash;

        relatedPrefabs[prefabHash].Remove(edge);

        if (relatedPrefabs[prefabHash].Count == 0)
        {
            relatedPrefabs.Remove(prefabHash);
        }

        Edges.Remove(edge);
    }

    private void MergeEdge(GraphEdge edge1, GraphEdge edge2, GraphNode node)
    {
        // edge1.SetNode(node.prefab, node);

        if (edge1.Prefab1.Equals(node.prefab))
        {
            edge1.node1 = node;
            return;
        }
        else if (edge1.Prefab2.Equals(node.prefab))
        {
            edge1.node2 = node;
            return;
        }
        else
        {
            throw new Exception("prefab not on edge");
        }
    }

    private IEnumerable<GraphNode> GetPrunedNodes()
    {
        return Nodes.Where(node => relatedEdges[node].Count(edge => !edge.prune) == 0);
    }

    private void Prune()
    {
        // return;
        foreach (var edgeGroup in relatedPrefabs.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            shortestEdge.prune = false;
        }

        var prunedNodes = GetPrunedNodes();
        int notFound = 0;

        foreach (var node in prunedNodes)
        {
            var eligibleEdges = relatedEdges[node].OrderBy(edge => edge.Weight);
            var found = false;

            foreach (var edge in eligibleEdges)
            {
                edge.colorName = "Purple";

                if (TryReplaceEdge(edge))
                {
                    found = true;
                    edge.prune = false;
                    break;
                }
                else if (TryMergeEdge(edge, node))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // eligibleEdges.First().prune = false;
                // eligibleEdges.First().colorName = "Yellow";
                notFound++;
            }
        }

        Log.Out($"{GetPrunedNodes().Count()} pruned Nodes, {notFound} not found");

        foreach (var edge in Edges.Where(e => e.prune).ToList())
        {
            RemoveEdge(edge);
        }
    }

    private bool TryMergeEdge(GraphEdge edge, GraphNode node)
    {
        var sisterEdges = relatedPrefabs[edge.PrefabHash].Where(e => !e.prune);
        var sisterEdge = sisterEdges.First();

        CaveUtils.Assert(sisterEdges.Count() == 1, $"{sisterEdges.Count()} edges, expected: 1");

        var commonNode = sisterEdge.GetNode(node.prefab);

        if (relatedEdges[commonNode].Count(e => !e.prune) > 1)
        {
            sisterEdge.colorName = "Green";
            MergeEdge(sisterEdge, edge, node);
            return true;
        }

        return false;
    }

    private bool TryReplaceEdge(GraphEdge edge)
    {
        var sisterEdges = relatedPrefabs[edge.PrefabHash].Where(e => !e.prune);
        var sisterEdge = sisterEdges.First();

        // CaveUtils.Assert(sisterEdges.Count() == 1, $"{sisterEdges.Count()} edges, expected: 1");

        bool cond1 = relatedEdges[sisterEdge.node1].Count(e => !e.prune) > 1 || sisterEdge.node1 == edge.node1 || sisterEdge.node1 == edge.node2;
        bool cond2 = relatedEdges[sisterEdge.node2].Count(e => !e.prune) > 1 || sisterEdge.node2 == edge.node1 || sisterEdge.node2 == edge.node2;

        if (cond1 && cond2)
        {
            sisterEdge.colorName = "Yellow";
            sisterEdge.prune = true;
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
