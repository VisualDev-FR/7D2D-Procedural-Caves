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

    private void AddEdge(GraphNode node1, GraphNode node2)
    {
        var edge = new GraphEdge(Edges.Count, node1, node2);

        Edges.Add(edge);
        Nodes.Add(node1);
        Nodes.Add(node2);

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
        if (point1.Prefab == point2.Prefab)
        {
            return;
        }

        AddEdge(point1.node, point2.node);
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
        // return;
        var notFound = 0;

        foreach (var node in Nodes)
        {
            if (!TryMergeEdgeAt(node.prefab))
            {
                notFound++;
            }
        }

        foreach (var edgeGroup in relatedPrefabs.Values)
        {
            var edges = edgeGroup.Where(e => !e.pruned).ToList();
            int edgesCount = edges.Count;

            if (edgesCount < 2) continue;

            if (edgesCount > 2)
            {
                Log.Warning($"edge count: {edgesCount}");
            }

            foreach (var edge in edges)
            {
                if (edgesCount == 1)
                {
                    break;
                }

                bool cond1 = relatedEdges[edge.node1].Count(e => !e.pruned) > 1;
                bool cond2 = relatedEdges[edge.node2].Count(e => !e.pruned) > 1;

                if (cond1 && cond2)
                {
                    edge.pruned = true;
                    edge.colorName = "Yellow";
                    edgesCount--;
                }
            }

            if(edgesCount == 2)
            {
                edges[0].colorName = "Yellow";
                edges[1].colorName = "Yellow";
            }
        }

        /* foreach (var edgeGroup in relatedPrefabs.Values)
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

            if (!found && !TryMergeEdgeAt(node.prefab))
            {
                // sameNodeEdges.First().pruned = false;
                // sameNodeEdges.First().colorName = "Yellow";
                notFound++;
            }
        } */

        Log.Out($"{GetNodesAlone().Count()} pruned Nodes, {notFound} not found");

        foreach (var edge in Edges.ToList())
        {
            if (edge.pruned)
            {
                // RemoveEdge(edge);
                edge.colorName = "DarkGray";
                edge.width = 1;
                edge.opacity = 50;
            }
            else
            {
                // edge.colorName = "DarkRed";
            }
        }
    }

    private bool TryMergeEdgeAt(CavePrefab prefab)
    {
        var otherEdges = Edges.Where(e => e.IsRelatedToPrefab(prefab));
        var groupedEdges = new Dictionary<int, List<GraphEdge>>();

        foreach (var edge in otherEdges)
        {
            var hashcode = edge.PrefabHash;

            if (!groupedEdges.ContainsKey(hashcode))
            {
                groupedEdges[hashcode] = new List<GraphEdge>();
            }

            groupedEdges[hashcode].Add(edge);
        }

        var combinations = GenerateCombinations(prefab.nodes, groupedEdges.Values.ToList(), new GraphEdge[groupedEdges.Count], 0).ToList();
        var bestComb = new List<GraphEdge>();
        var minWeight = float.MaxValue;

        foreach (var comb in combinations)
        {
            var nodes = comb.Select(e => e.GetNode(prefab)).ToHashSet();

            // Log.Out($"{nodes.Count()} < {prefab.nodes.Count}");

            if (nodes.Count() < prefab.nodes.Count)
            {
                continue;
            }

            var weight = comb.Sum(e => e.Weight);

            if (weight < minWeight)
            {
                bestComb = comb;
                minWeight = weight;
            }
        }

        if (bestComb == null)
        {
            Log.Warning("No valid comb found");
        }

        foreach (var edge in bestComb)
        {
            // edge.colorName = "White";
            edge.pruned = false;
        }

        return true;
    }

    private IEnumerable<List<GraphEdge>> GenerateCombinations(List<GraphNode> nodes, List<List<GraphEdge>> edges, GraphEdge[] currentCombination, int depth)
    {
        // Log.Out($"depth: {depth}, nodes: {nodes.Count}, edges: {edges.Count}");

        if (depth == edges.Count)
        {
            yield return currentCombination.ToList();
            yield break;
        }

        var edgesSelection = edges[depth];

        foreach (var edge in edgesSelection)
        {
            currentCombination[depth] = edge;

            foreach (var comb in GenerateCombinations(nodes, edges, currentCombination.ToArray(), depth + 1))
            {
                yield return comb;
            }
        }
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
