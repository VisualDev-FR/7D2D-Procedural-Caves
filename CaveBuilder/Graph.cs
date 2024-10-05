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

    private GraphEdge AddEdge(GraphNode node1, GraphNode node2)
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

        return edge;
    }

    private GraphEdge AddEdge(DelauneyPoint point1, DelauneyPoint point2)
    {
        if (point1.Prefab == point2.Prefab)
        {
            return null;
        }

        return AddEdge(point1.node, point2.node);
    }

    private GraphEdge GetEdge(GraphNode node1, GraphNode node2)
    {
        int hashcode = GraphEdge.GetHashCode(node1, node2);

        foreach (var edge in Edges)
        {
            if (edge.GetHashCode() == hashcode)
            {
                return edge;
            }
        }

        throw new KeyNotFoundException();
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
        var prefabs = Nodes.Select(node => node.prefab).ToHashSet();
        var notFound = 0;

        foreach (var prefab in prefabs)
        {
            if (!TryFindLocalGraph(prefab))
            {
                notFound++;
            }
        }

        foreach (var edgeGroup in relatedPrefabs.Values)
        {
            var edges = edgeGroup.Where(e => !e.pruned).ToList();
            int edgesCount = edges.Count;

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
                    edgesCount--;
                }
            }
        }

        foreach (var edgeGroup in relatedPrefabs.Values)
        {
            var edges = edgeGroup.Where(e => !e.pruned).ToList();
            int edgesCount = edges.Count;

            if (edgesCount == 1) continue;

            if (edgesCount == 2)
            {
                TryMergeEdges(edges[0], edges[1], "DarkRed");
            }
        }

        Log.Out($"{GetNodesAlone().Count()} pruned Nodes, {notFound} not found");

        foreach (var node in GetNodesAlone())
        {
            if (relatedEdges[node].Count == 0)
            {
                Log.Error($"Alone node without related edge at [{node.position}]");
            }

            var edge = relatedEdges[node].OrderBy(e => e.Weight).First();
            edge.colorName = "White";
            edge.pruned = false;
        }

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

    private bool TryMergeEdges(GraphEdge edge1, GraphEdge edge2, string mergedColor = "Yellow")
    {
        // TODO: refactor this dirty code

        GraphNode node1 = null;
        GraphNode node2 = null;

        if (edge1.node1.Equals(edge2.node1) || edge1.node1.Equals(edge2.node2) || edge1.node2.Equals(edge2.node1) || edge1.node2.Equals(edge2.node2))
        {
            return false;
        }

        if (relatedEdges[edge1.node1].Count(e => !e.pruned) == 1)
        {
            node1 = edge1.node1;
        }
        else if (relatedEdges[edge1.node2].Count(e => !e.pruned) == 1)
        {
            node1 = edge1.node2;
        }
        else
        {
            return false;
        }

        if (relatedEdges[edge2.node1].Count(e => !e.pruned) == 1)
        {
            node2 = edge2.node1;
        }
        else if (relatedEdges[edge2.node2].Count(e => !e.pruned) == 1)
        {
            node2 = edge2.node2;
        }
        else
        {
            return false;
        }

        if (node1.prefab.Equals(node2.prefab))
        {
            return false;
        }

        var edge = new GraphEdge(node1, node2);

        if (Edges.Contains(edge))
        {
            edge = GetEdge(node1, node2);
        }
        else
        {
            edge = AddEdge(node1, node2);
        }


        edge.colorName = mergedColor;
        edge.pruned = false;

        edge1.pruned = true;
        edge2.pruned = true;

        return true;
    }

    private bool TryFindLocalGraph(CavePrefab prefab)
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

            var weight = comb.Sum(e => e.Weight) * (1 + prefab.nodes.Count - nodes.Count);

            if (weight < minWeight)
            {
                bestComb = comb;
                minWeight = weight;
            }
        }

        if (bestComb.Count == 0)
        {
            // Log.Warning("No valid comb found");
            return false;
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
