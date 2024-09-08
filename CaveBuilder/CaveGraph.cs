using System.Collections.Generic;
using System.IO;

public class CaveGraph
{
    public Dictionary<int, HashSet<int>> graph;

    public Dictionary<int, HashSet<int>> tunnels;

    public Dictionary<int, string> prefabs;

    public CaveGraph(string filename)
    {
        graph = new Dictionary<int, HashSet<int>>();
        tunnels = new Dictionary<int, HashSet<int>>();
        prefabs = new Dictionary<int, string>();

        using (var stream = new StreamReader(filename))
        {
            int edgesCount = int.Parse(stream.ReadLine());

            for (int i = 0; i < edgesCount; i++)
            {
                int edgeID = int.Parse(stream.ReadLine());
                int pdi1 = int.Parse(stream.ReadLine());
                int pdi2 = int.Parse(stream.ReadLine());
                string pdiName1 = stream.ReadLine();
                string pdiName2 = stream.ReadLine();

                AddEdge(pdi1, pdiName1, edgeID);
                AddEdge(pdi2, pdiName2, edgeID);
                AddTunnel(edgeID, pdi1, pdi2);
            }
        }
    }

    public void AddTunnel(int edgeID, int prefab1, int prefab2)
    {
        if (!tunnels.ContainsKey(edgeID))
        {
            tunnels[edgeID] = new HashSet<int>();
        }

        tunnels[edgeID].Add(prefab1);
        tunnels[edgeID].Add(prefab2);
    }

    public void AddEdge(int prefabID, string prefabName, int tunnelID)
    {
        prefabs[prefabID] = prefabName;

        if (!graph.ContainsKey(prefabID))
        {
            graph[prefabID] = new HashSet<int>();
        }

        graph[prefabID].Add(tunnelID);
    }
}