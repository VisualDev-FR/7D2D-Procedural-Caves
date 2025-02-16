using System;
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
        return @"Cave debug commands:
            - sgms [value]: set god mode speed, from the given float value
        ";
    }

    private static void ClusterCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos, false);

        if (prefabInstance == null)
        {
            Logging.Warning($"[Cluster] no prefab found at position [{playerPos}]");
            return;
        }

        var clusters = BlockClusterizer.Clusterize(prefabInstance);

        Logging.Info($"[Cluster] player: [{playerPos}], prefab: [{prefabInstance.boundingBoxPosition}], rotation: {prefabInstance.rotation}, name: '{prefabInstance.name}'");

        if (clusters.Count == 0)
        {
            Logging.Warning($"[Cluster] No cluster found.");
            return;
        }

        for (int i = 0; i < clusters.Count; i++)
        {
            clusters[i] = clusters[i].Transform(prefabInstance);
            Logging.Info($"[Cluster] {clusters[i].start,18} | {clusters[i].size}");
        }
        Logging.Info($"[Cluster] {clusters.Count} clusters found.");

        BlockSelectionUtils.SelectBoxes(clusters);
    }

    private static void PrefabCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos, false);

        if (prefabInstance == null)
        {
            Logging.Warning($"[Prefab] no prefab found at position [{playerPos}]");
            return;
        }

        var bb = new BoundingBox(prefabInstance.boundingBoxPosition, prefabInstance.boundingBoxSize);

        Logging.Info($"[Prefab] '{prefabInstance.name}', start: [{bb.start}], size: [{bb.size}], rotation: {prefabInstance.rotation}");

        BlockSelectionUtils.SelectBox(bb);
    }

    private static void DecorateCommand(List<string> _params)
    {
        var worldPos = BlockSelectionUtils.GetSelectionPosition();

        if (worldPos.Equals(Vector3i.zero))
        {
            Logging.Warning("empty selection.");
            return;
        }

        string blockName = GameManager.Instance.World.GetBlock(worldPos).Block.blockName;
        bool isChild = GameManager.Instance.World.GetBlock(worldPos).ischild;
        bool isMultiBlock = GameManager.Instance.World.GetBlock(worldPos).Block.isMultiBlock;

        Logging.Info($"'{worldPos}' : isChild: {isChild}, isMulti: {isMultiBlock}, name: {blockName}");

        if (_params.Count == 1)
            return;

        int.TryParse(_params[2], out int rotation);

        var blockID = int.Parse(_params[1]);
        var blockValue = Block.GetBlockValue(blockID);
        var chunk = GameManager.Instance.World.GetChunkFromWorldPos(worldPos) as Chunk;
        var localChunkPos = World.toBlock(worldPos);

        blockValue.rotation = (byte)rotation;

        chunk.SetBlock(
            GameManager.Instance.World,
            localChunkPos.x,
            localChunkPos.y,
            localChunkPos.z,
            blockValue,
            _notifyAddChange: true
        );
    }

    private static void SpawnCommand(List<string> _params)
    {
        CaveConfig.enableCaveSpawn = !CaveConfig.enableCaveSpawn;

        var enabled = CaveConfig.enableCaveSpawn ? "enabled" : "disabled";

        Logging.Info($"cave spawn {enabled}");
    }

    private static void SetGodModeSpeed(List<string> _params)
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        player.GodModeSpeedModifier = float.Parse(_params[1]);
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            Logging.Info(getDescription());
            return;
        }

        switch (_params[0].ToLower())
        {
            case "cluster":
                ClusterCommand(_params);
                break;

            case "prefab":
                PrefabCommand(_params);
                break;

            case "deco":
                DecorateCommand(_params);
                break;

            case "spawn":
                SpawnCommand(_params);
                break;

            case "sgms":
                SetGodModeSpeed(_params);
                break;

            default:
                Logging.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}