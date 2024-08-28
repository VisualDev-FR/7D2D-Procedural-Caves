using System.Collections.Generic;

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

    private static void ClusterCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos, false);

        if (prefabInstance == null)
        {
            Log.Warning($"[Cluster] no prefab found at position [{playerPos}]");
            return;
        }

        var clusters = TTSReader.Clusterize(prefabInstance);

        Log.Out($"[Cluster] player: [{playerPos}], prefab: [{prefabInstance.boundingBoxPosition}], rotation: {prefabInstance.rotation}, name: '{prefabInstance.name}'");

        if (clusters.Count == 0)
        {
            Log.Warning($"[Cluster] No cluster found.");
            return;
        }

        for (int i = 0; i < clusters.Count; i++)
        {
            clusters[i] = clusters[i].Transform(prefabInstance.boundingBoxPosition, prefabInstance.rotation, prefabInstance.prefab.size);
            Log.Out($"[Cluster] {clusters[i].start,18} | {clusters[i].size}");
        }

        if (_params.Count == 1)
            return;

        var index = int.Parse(_params[1]);
        var rectangle = clusters[index];
        var selection = BlockToolSelection.Instance;

        selection.SelectionStart = rectangle.start;
        selection.SelectionEnd = rectangle.start + rectangle.size - Vector3i.one;
        selection.SelectionActive = true;
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