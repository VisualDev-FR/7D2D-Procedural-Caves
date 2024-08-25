using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CaveDebugConsoleCmd : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "cavedebug", "cd" };
    }

    public override string getDescription()
    {
        return "cavedebug cd => command line tools for cave debugging";
    }

    public override string getHelp()
    {
        return getDescription();
    }

    public static IEnumerable<Vector3i> BrowseSelectionPositions()
    {
        var selection = BlockToolSelection.Instance;

        var start = selection.m_selectionStartPoint;
        var end = selection.m_SelectionEndPoint;

        int y = start.y;
        while (true)
        {
            int x = start.x;
            while (true)
            {
                int z = start.z;
                while (true)
                {
                    yield return new Vector3i(x, y, z);

                    if (z == end.z) break;
                    z += Math.Sign(end.z - start.z);
                }
                if (x == end.x) break;
                x += Math.Sign(end.x - start.x);
            }
            if (y == end.y) break;
            y += Math.Sign(end.y - start.y);
        }

        yield break;
    }

    public static Vector3i ParseVector(string s)
    {
        string[] array = s.Split(',');
        if (array.Length != 3)
        {
            return Vector3i.zero;
        }

        return new Vector3i(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
    }

    public static List<Rect3D> FindClusters(Vector3 playerPos)
    {
        var position = new Vector3i(playerPos);
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(position, false);

        if (prefabInstance == null)
        {
            Log.Warning($"[Cluster] no prefab found at position [{position}]");
            return new List<Rect3D>();
        }

        var blocks = TTSReader.GetUndergroundObstacles(prefabInstance.location.FullPath, prefabInstance.prefab.yOffset);
        var clusters = TTSReader.ClusterizeBlocks(blocks.ToHashSet());

        Log.Out($"[Cluster] player: [{playerPos}], prefab: [{prefabInstance.boundingBoxPosition}], rotation: {prefabInstance.rotation}");

        if (clusters.Count == 0)
        {
            Log.Warning($"[Cluster] No cluster found for '{prefabInstance.name}'");
        }

        var result = new List<Rect3D>();

        foreach (var rect in clusters)
        {
            result.Add(rect.Transform(prefabInstance.boundingBoxPosition, prefabInstance.rotation, prefabInstance.prefab.size));
        }

        return result;
    }

    private static void ClusterCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var clusters = FindClusters(playerPos);

        if (clusters == null)
            return;

        foreach (var cluster in clusters)
        {
            Log.Out($"[Cluster] {cluster.start,18} | {cluster.end}");
        }

        if (_params.Count == 1)
            return;

        var index = int.Parse(_params[1]);
        var rectangle = clusters[index];
        var selection = BlockToolSelection.Instance;

        selection.SelectionStart = rectangle.start;
        selection.SelectionEnd = rectangle.end - Vector3i.one;
        selection.SelectionActive = true;
    }

    private void NotImplementedCommand(string commandName)
    {
        Log.Error($"Not implemented command: '{commandName}'");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            Log.Out(getDescription());
            return;
        }

        switch (_params[0].ToLower())
        {
            case "cluster":
                ClusterCommand(_params);
                break;

            default:
                Log.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}