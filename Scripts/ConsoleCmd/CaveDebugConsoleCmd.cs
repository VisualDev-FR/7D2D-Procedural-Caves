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

        var clusters = BlockClusterizer.Clusterize(prefabInstance);

        Log.Out($"[Cluster] player: [{playerPos}], prefab: [{prefabInstance.boundingBoxPosition}], rotation: {prefabInstance.rotation}, name: '{prefabInstance.name}'");

        if (clusters.Count == 0)
        {
            Log.Warning($"[Cluster] No cluster found.");
            return;
        }

        for (int i = 0; i < clusters.Count; i++)
        {
            clusters[i] = clusters[i].Transform(prefabInstance);
            Log.Out($"[Cluster] {clusters[i].start,18} | {clusters[i].size}");
        }
        Log.Out($"[Cluster] {clusters.Count} clusters found.");

        BlockSelectionUtils.SelectBoxes(clusters);
    }

    private static void PrefabCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos, false);

        if (prefabInstance == null)
        {
            Log.Warning($"[Prefab] no prefab found at position [{playerPos}]");
            return;
        }

        var bb = new BoundingBox(prefabInstance.boundingBoxPosition, prefabInstance.boundingBoxSize);

        Log.Out($"[Prefab] '{prefabInstance.name}', start: [{bb.start}], size: [{bb.size}], rotation: {prefabInstance.rotation}");

        BlockSelectionUtils.SelectBox(bb);
    }

    private static void LightCommand(List<string> _params)
    {
        throw new NotImplementedException();
    }

    private static void MoonScaleCommand(List<string> _params)
    {
        if (_params.Count == 0)
        {
            Log.Error($"[Cave] Missing argument: 'scale' (float)");
            return;
        }

        if (!float.TryParse(_params[1], out var scale))
        {
            Log.Error($"[Cave] Invalid argument: '{_params[1]}'");
            return;
        }

        CaveConfig.CaveLightConfig.moonLightScale = scale;
    }

    private static void DecorateCommand(List<string> _params)
    {
        var worldPos = BlockSelectionUtils.GetSelectionPosition();

        if (worldPos.Equals(Vector3i.zero))
        {
            Log.Warning("[Cave] empty selection.");
            return;
        }

        string blockName = GameManager.Instance.World.GetBlock(worldPos).Block.blockName;
        bool isChild = GameManager.Instance.World.GetBlock(worldPos).ischild;
        bool isMultiBlock = GameManager.Instance.World.GetBlock(worldPos).Block.isMultiBlock;

        Log.Out($"'{worldPos}' : isChild: {isChild}, isMulti: {isMultiBlock}, name: {blockName}");

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

            case "prefab":
                PrefabCommand(_params);
                break;

            case "light":
                LightCommand(_params);
                break;

            case "moon":
                MoonScaleCommand(_params);
                break;

            case "deco":
                DecorateCommand(_params);
                break;

            default:
                Log.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}