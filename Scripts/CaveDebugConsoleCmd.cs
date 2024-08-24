using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

using Path = System.IO.Path;


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

    private void ObstaclesCommand()
    {
        var _world = GameManager.Instance.World;
        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        var prefabInstance = PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);
        int yOffset = prefabInstance.prefab.yOffset;
        int obstaclesCount = 0;

        foreach (var position in BrowseSelectionPositions())
        {
            var worldBlock = _world.GetBlock(position);

            if (position.y > 0 && position.y < -yOffset && TTSReader.IsObstacle(worldBlock))
            {
                obstaclesCount++;
            }
        }

        Log.Out($"{obstaclesCount} obstacles found in selection");
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
            case "obstacles":
            case "obs":
                ObstaclesCommand();
                break;

            default:
                Log.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}