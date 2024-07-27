using System;
using System.Collections.Generic;
using UnityEngine;


public class CaveEditorConsoleCmd : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "caveeditor", "ce" };
    }

    public override string getDescription()
    {
        return @"Cave prefab editor helpers:
            - create: creates a new empty prefab with the required tags
            - marker: Add a cave marker into the selection.
            - replaceterrain, rt: Replace all terrain blocks in the selection with the selected item.
            - selectall, sa: add all the prefab volume to the selection box.
            - room: create an empty room of selected item in the selection box.
            - setwater, sw [mode]:
                * 'empty': set all water blocks of the selection to air.
                * 'fill': set all air blocks of the selection to water.

            Incoming:
            - save: special save method.
            - test: run a testing session with tunneling around the markers
            - tags [type]: Add the required tags to get a valid cave prefab. Type is optional an accept the following keywords:
                * 'entrance' -> the prefab is a cave entrance
                * 'underwater' -> the prefab is an underwater entrance
            - invert: show negative view of the terrain
            - check: create a report of the requirements for getting a valid cave prefab.
            - bubble: Create a procedural volume into the selection box (min selection size = 10x10x10).
            - decorate: Decorate terrain with items specfied in config files.
            - tunnel [marker1] [marker2]: Create a tunnel between two specified cave markers.
            - stalactite [height] Creates a procedural stalactite of the specified height at the start position of the selection.
            - extend [x] [y] [z]: extend the selection of x blocks in the x direction, etc ...
        ";
    }

    public override string getHelp()
    {
        return getDescription();
    }

    private static IEnumerable<Vector3i> BrowseSelectionPositions()
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

    private static PrefabInstance GetCurrentPrefab()
    {
        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        return PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);
    }

    private void CaveMarkerCommand()
    {
        var selection = BlockToolSelection.Instance;
        var isActive = selection.SelectionActive;

        if (!isActive)
        {
            Log.Error("The selection is empty.");
            return;
        }

        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        PrefabInstance prefabInstance = PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);

        if (prefabInstance == null)
        {
            Log.Error("null prefabInstance");
            return;
        }

        var selectionStart = selection.SelectionStart;
        var selectionEnd = selection.SelectionEnd;
        var size = new Vector3i(
            Mathf.Abs(selectionStart.x - selectionEnd.x) + 1,
            Mathf.Abs(selectionStart.y - selectionEnd.y) + 1,
            Mathf.Abs(selectionStart.z - selectionEnd.z) + 1
        );

        if (size.x > 1 && size.z > 1)
        {
            Log.Error($"x and z can't be upper to 1");
            return;
        }

        var startPoint = selection.SelectionMin;
        var start = startPoint - prefabInstance.boundingBoxPosition;

        prefabInstance.prefab.AddNewPOIMarker(
            _prefabInstanceName: prefabInstance.name,
            bbPos: prefabInstance.boundingBoxPosition,
            _start: start,
            _size: size,
            _group: "cave",
            _tags: CavePrefab.tagCaveMarker,
            _type: Prefab.Marker.MarkerTypes.None,
            isSelected: false
        );

        SelectionBoxManager.Instance.Deactivate();
    }

    private void ReplaceTerrainCommand()
    {
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Log.Error($"Invalid filler block: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        var _gm = GameManager.Instance;
        var _density = blockValue.Block.shape.IsTerrain() ? MarchingCubes.DensityTerrain : MarchingCubes.DensityAir;
        var _textureFull = holdingItemItemValue.Texture;

        foreach (var position in BrowseSelectionPositions())
        {
            var worldBlock = _gm.World.GetBlock(position);

            BlockChangeInfo blockChangeInfo = new BlockChangeInfo(position, blockValue, _density)
            {
                textureFull = _textureFull,
                bChangeTexture = true
            };

            if (worldBlock.Block.shape.IsTerrain())
                list.Add(blockChangeInfo);
        }

        _gm.SetBlocksRPC(list);
    }

    private void SetWaterCommand(List<string> args)
    {
        if (args.Count < 2)
        {
            Log.Error("Missing argument: 'fill' or 'empty'");
            return;
        }

        NetPackageWaterSet package = NetPackageManager.GetPackage<NetPackageWaterSet>();

        var _gm = GameManager.Instance;
        var waterValue = args[1].ToLower() == "fill" ? WaterValue.Full : WaterValue.Empty;

        foreach (var position in BrowseSelectionPositions())
        {
            var worldBlock = _gm.World.GetBlock(position);

            if (WaterUtils.CanWaterFlowThrough(worldBlock))
            {
                package.AddChange(position, waterValue);
            }
        }

        _gm.SetWaterRPC(package);
    }

    private void SelectAllCommand()
    {
        var selection = BlockToolSelection.Instance;
        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        var prefabInstance = PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);

        var bbPos = prefabInstance.boundingBoxPosition;
        var bbSize = prefabInstance.boundingBoxSize;

        selection.SelectionStart = bbPos;
        selection.SelectionEnd = bbPos + bbSize - Vector3i.one;
        selection.SelectionActive = true;
    }

    private void RoomCommand()
    {
        var selection = BlockToolSelection.Instance;
        var isActive = selection.SelectionActive;

        if (!isActive)
        {
            Log.Error("The selection is empty.");
            return;
        }

        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Log.Error($"Invalid selected item: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        var _gm = GameManager.Instance;
        var _density = blockValue.Block.shape.IsTerrain() ? MarchingCubes.DensityTerrain : MarchingCubes.DensityAir;
        var _textureFull = holdingItemItemValue.Texture;

        var start = selection.m_selectionStartPoint;
        var end = selection.m_SelectionEndPoint;

        if (start.y < 0 || end.y < 0)
        {
            Log.Error("Start position height must be over 0.");
            return;
        }

        int y = start.y;
        while (true)
        {
            int x = start.x;
            while (true)
            {
                int z = start.z;
                while (true)
                {
                    var position = new Vector3i(x, y, z);

                    BlockChangeInfo blockChangeInfo = new BlockChangeInfo(position, blockValue, _density)
                    {
                        textureFull = _textureFull,
                        bChangeTexture = true
                    };

                    bool bound_x = x == start.x || x == end.x;
                    bool bound_y = y == start.y || y == end.y;
                    bool bound_z = z == start.z || z == end.z;

                    if (bound_x || bound_y || bound_z)
                    {
                        list.Add(blockChangeInfo);
                    }

                    if (z == end.z) break;
                    z += Math.Sign(end.z - start.z);
                }
                if (x == end.x) break;
                x += Math.Sign(end.x - start.x);
            }
            if (y == end.y) break;
            y += Math.Sign(end.y - start.y);
        }

        _gm.SetBlocksRPC(list);
    }

    private void CreateCommand()
    {
        PrefabEditModeManager.Instance.NewVoxelPrefab();

        var prefabInstance = GetCurrentPrefab();

        prefabInstance.prefab.editorGroups.Add("cave");
        prefabInstance.prefab.Tags = CavePrefab.tagCave;
    }

    private void NotImplementedCommand(string commandName)
    {
        Log.Error($"Not implemented command: '{commandName}'");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (!PrefabEditModeManager.Instance.IsActive())
        {
            Log.Out("Command available in prefab editor only.");
            return;
        }

        if (_params.Count == 0)
        {
            Log.Out(getDescription());
            return;
        }

        var command = _params[0];

        switch (_params[0].ToLower())
        {
            case "marker":
            case "mark":
            case "cavemarker":
            case "cm":
                CaveMarkerCommand();
                break;

            case "replace":
            case "replaceterrain":
            case "rt":
                ReplaceTerrainCommand();
                break;

            case "save":
                NotImplementedCommand(command);
                break;

            case "check":
                NotImplementedCommand(command);
                break;

            case "tag":
            case "tags":
                NotImplementedCommand(command);
                break;

            case "procfill":
                NotImplementedCommand(command);
                break;

            case "water":
            case "fillwater":
            case "waterfill":
                NotImplementedCommand(command);
                break;

            case "decorate":
                NotImplementedCommand(command);
                break;

            case "tunnel":
                NotImplementedCommand(command);
                break;

            case "stalactite":
            case "stalagmite":
            case "stal":
                NotImplementedCommand(command);
                break;

            case "room":
                RoomCommand();
                break;

            case "extend":
                NotImplementedCommand(command);
                break;

            case "selectall":
            case "sa":
                SelectAllCommand();
                break;

            case "setwater":
            case "sw":
                SetWaterCommand(_params);
                break;

            case "create":
                CreateCommand();
                break;

            default:
                Log.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}