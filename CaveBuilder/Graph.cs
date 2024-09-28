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

    private void MergeEdge(GraphEdge edge1, GraphNode node)
    {
        if (edge1.Prefab1.Equals(node.prefab))
        {
            relatedEdges[edge1.node1].Remove(edge1);
            edge1.node1 = node;
        }
        else if (edge1.Prefab2.Equals(node.prefab))
        {
            relatedEdges[edge1.node2].Remove(edge1);
            edge1.node2 = node;
        }
        else
        {
            throw new Exception("prefab not on edge");
        }

        relatedEdges[node].Add(edge1);
        edge1.colorName = "White";
        edge1.pruned = false;
    }

    private void ReplaceEdge(GraphEdge replace, GraphEdge by)
    {
        replace.pruned = true;
        by.pruned = false;
        by.colorName = "Purple";
    }

    private IEnumerable<GraphNode> GetNodesAlone()
    {
        return Nodes.Where(node => IsNodeAlone(node));
    }

    private bool IsNodeAlone(GraphNode node)
    {
        return relatedEdges[node].Count(edge => !edge.pruned) == 0;
    }

    private void Prune()
    {
        foreach (var edgeGroup in relatedPrefabs.Values)
        {
            var shortestEdge = edgeGroup
                .OrderBy(edge => edge.Weight)
                .First();

            shortestEdge.pruned = false;
        }

        var prunedNodes = GetNodesAlone();
        int notFound = 0;

        foreach (var node in prunedNodes)
        {
            // continue;
            var sameNodeEdges = relatedEdges[node].OrderBy(edge => edge.Weight);
            var found = false;

            foreach (var edge in sameNodeEdges)
            {
                if (TryReplaceEdge(edge))
                {
                    found = true;
                    break;
                }
            }

            if (!found && !TryMergeEdgeAt(node))
            {
                // sameNodeEdges.First().pruned = false;
                // sameNodeEdges.First().colorName = "Yellow";
                notFound++;
            }
        }

        Log.Out($"{GetNodesAlone().Count()} pruned Nodes, {notFound} not found");

        foreach (var edge in Edges.Where(e => e.pruned).ToList())
        {
            RemoveEdge(edge);
        }
    }

    private bool TryMergeEdgeAt(GraphNode node)
    {
        var otherEdges = Edges.Where(edge => !edge.pruned && edge.IsRelatedToPrefab(node.prefab) && relatedEdges[edge.GetNode(node.prefab)].Count(e => !e.pruned) > 1);
        var mergedEdges = otherEdges
            .Select(e => new { replace = e, by = new GraphEdge(node, !e.Prefab1.Equals(node.prefab) ? e.node1 : e.node2) })
            .OrderBy(e => e.by.Weight);

        if (mergedEdges.Count() == 0)
        {
            return false;
        }

        MergeEdge(mergedEdges.First().replace, node);

        return true;
    }

    private bool TryMergeEdge(GraphEdge edge, GraphNode node)
    {
        var sisterEdges = relatedPrefabs[edge.PrefabHash].Where(e => !e.pruned);
        var sisterEdge = sisterEdges.First();

        CaveUtils.Assert(sisterEdges.Count() == 1, $"{sisterEdges.Count()} edges, expected: 1");

        var commonNode = sisterEdge.GetNode(node.prefab);

        if (relatedEdges[commonNode].Count(e => !e.pruned) > 1)
        {
            sisterEdge.colorName = "Purple";
            MergeEdge(sisterEdge, node);
            return true;
        }

        return false;
    }

    private bool TryReplaceEdge(GraphEdge edge)
    {
        var sisterEdges = relatedPrefabs[edge.PrefabHash].Where(e => !e.pruned);
        var sisterEdge = sisterEdges.First();

        // CaveUtils.Assert(sisterEdges.Count() == 1, $"{sisterEdges.Count()} edges, expected: 1");

        bool cond1 = relatedEdges[sisterEdge.node1].Count(e => !e.pruned) > 1 || sisterEdge.node1 == edge.node1 || sisterEdge.node1 == edge.node2;
        bool cond2 = relatedEdges[sisterEdge.node2].Count(e => !e.pruned) > 1 || sisterEdge.node2 == edge.node1 || sisterEdge.node2 == edge.node2;

        if (cond1 && cond2)
        {
            ReplaceEdge(sisterEdge, edge);
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
