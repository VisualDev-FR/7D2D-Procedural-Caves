using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Path = System.IO.Path;

/* TODO:
    - test: run a testing session with tunneling around the markers
    - invert: show negative view of the terrain
    - check: create a report of the requirements for getting a valid cave prefab.
    - procedural, proc [type]: Create a procedural volume into the selection box (min selection size = 10x10x10).
    - decorate: Decorate terrain with items specfied in config files.
    - tunnel [marker1] [marker2]: Create a tunnel between two specified cave markers.
    - extendselection, es [x] [y] [z]: extend the selection of x blocks in the x direction, etc ...
 */
public class CaveEditorConsoleCmd : ConsoleCmdAbstract
{
    private readonly Dictionary<string, byte> markerDirectionsMapping = new Dictionary<string, byte>()
    {
        {"n", 0},
        {"w", 1},
        {"s", 2},
        {"e", 3},
    };

    public override string[] getCommands()
    {
        return new string[] { "caveeditor", "ce" };
    }

    public override string getDescription()
    {
        return "caveeditor ce => additional command line tools for the prefab editor";
    }

    public override string getHelp()
    {
        return @"Cave prefab editor helpers:
            - create: creates a new empty prefab with the required tags
            - marker [direction]: Add a cave marker into the selection.
                * 'n': set marker direction to north
                * 's': set marker direction to south
                * 'e': set marker direction to east
                * 'w': set marker direction to west
            - replaceall, ra: replace all block in the selection box except air and water, by the selected item
            - replaceground, rg: replace all terrain blocks inside the selection box, which have air above them with the selected item.
            - replaceterrain, rt: Replace all terrain blocks in the selection with the selected item.
            - rename [name]: rename all files of the current prefab with the given new name
            - room [options]: create an empty room of selected item in the selection box.
                * 'empty': genrate a square empty room with wall width = 1 block.
                * 'proc': generate procedural cave room in the selection box
            - selectall, sa: add all the prefab volume to the selection box.
            - stmarkerdirection, smd [direction]: once a marker is selected:
                * 'n': set direction to north
                * 's': set direction to south
                * 'e': set direction to east
                * 'w': set direction to west
            - setwater, sw [mode]:
                * 'empty': set all water blocks of the selection to air.
                * 'fill': set all air blocks of the selection to water.
            - stalactite [height] Creates a procedural stalactite of the specified height at the start position of the selection box.
            - tags [type]: Add the required tags to get a valid cave prefab. Type is optional an accept the following keywords:
                * 'entrance' -> the prefab is a cave entrance
                * 'underground, ug' -> the prefab is an underground prefab
        ";
    }

    public static PrefabInstance GetCurrentPrefab()
    {
        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        return PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);
    }

    public static BlockValue? GetSelectedBlock(bool allowAir = false)
    {
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair && !allowAir)
        {
            Logging.Error($"Invalid selected item: '{holdingItemItemValue.ItemClass.Name}'");
            return null;
        }

        return blockValue;
    }

    private void CaveMarkerCommand(List<string> args)
    {
        var logger = Logging.CreateLogger("CaveMarker");
        var selection = BlockToolSelection.Instance;
        var isActive = selection.SelectionActive;

        if (args.Count == 1)
        {
            logger.Error("Missing argument: direction (n, e, w, or s)");
            return;
        }

        var direction = args[1];

        if (!markerDirectionsMapping.ContainsKey(direction))
        {
            logger.Error($"Invalid direction: '{direction}', should be 'n', 'e', 'w' or 's'");
            return;
        }

        if (!isActive)
        {
            logger.Error("The selection is empty.");
            return;
        }

        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        PrefabInstance prefabInstance = PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);

        if (prefabInstance == null)
        {
            logger.Error("null prefabInstance");
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
            logger.Error($"x and z can't be upper to 1");
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
            _tags: CaveTags.tagCaveMarker,
            _type: Prefab.Marker.MarkerTypes.None,
            isSelected: false
        );

        var marker = BlockSelectionUtils.GetSelectedMarker();

        marker.rotations = markerDirectionsMapping[direction];

        PrefabEditModeManager.Instance.NeedsSaving = true;
        SelectionBoxManager.Instance.SetFacingDirection("POIMarker", marker.name, marker.rotations * -90);
        SelectionBoxManager.Instance.Deactivate();
    }

    private void ReplaceTerrainCommand()
    {
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Logging.Error($"Invalid filler block: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        var _gm = GameManager.Instance;

        foreach (var position in BlockSelectionUtils.BrowseSelectionPositions())
        {
            var worldBlock = _gm.World.GetBlock(position);
            var clusterIndex = _gm.World.ChunkCache.ClusterIdx;
            var _density = _gm.World.GetDensity(clusterIndex, position);

            BlockChangeInfo blockChangeInfo = new BlockChangeInfo(position, blockValue, _density);

            if (worldBlock.Block.shape.IsTerrain())
                list.Add(blockChangeInfo);
        }

        _gm.SetBlocksRPC(list);
    }

    private void ReplaceGroundCommand()
    {
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Logging.Error($"Invalid filler block: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        var _gm = GameManager.Instance;

        foreach (var position in BlockSelectionUtils.BrowseSelectionPositions())
        {
            var worldBlock = _gm.World.GetBlock(position);
            var upperBlock = _gm.World.GetBlock(position + Vector3i.up);
            var clusterIndex = _gm.World.ChunkCache.ClusterIdx;
            var _density = _gm.World.GetDensity(clusterIndex, position);

            BlockChangeInfo blockChangeInfo = new BlockChangeInfo(position, blockValue, _density);

            if (worldBlock.Block.shape.IsTerrain() && upperBlock.isair)
                list.Add(blockChangeInfo);
        }

        _gm.SetBlocksRPC(list);
    }

    private void SetWaterCommand(List<string> args)
    {
        if (args.Count < 2)
        {
            Logging.Error("Missing argument: 'fill' or 'empty'");
            return;
        }

        NetPackageWaterSet package = NetPackageManager.GetPackage<NetPackageWaterSet>();

        var _gm = GameManager.Instance;
        var waterValue = args[1].ToLower() == "fill" ? WaterValue.Full : WaterValue.Empty;

        foreach (var position in BlockSelectionUtils.BrowseSelectionPositions())
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
        PrefabEditModeManager.Instance.updatePrefabBounds();

        var selection = BlockToolSelection.Instance;
        var prefabInstanceId = PrefabEditModeManager.Instance.prefabInstanceId;
        var prefabInstance = PrefabSleeperVolumeManager.Instance.GetPrefabInstance(prefabInstanceId);

        var bbPos = prefabInstance.boundingBoxPosition;
        var bbSize = prefabInstance.boundingBoxSize;

        selection.SelectionStart = bbPos;
        selection.SelectionEnd = bbPos + bbSize - Vector3i.one;
        selection.SelectionActive = true;
    }

    private void RoomCommand(List<string> args)
    {
        var selection = BlockToolSelection.Instance;
        var isActive = selection.SelectionActive;

        if (!isActive)
        {
            Logging.Error("The selection is empty.");
            return;
        }

        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Logging.Error($"Invalid selected item: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        var _density = blockValue.Block.shape.IsTerrain() ? MarchingCubes.DensityTerrain : MarchingCubes.DensityAir;
        var start = selection.m_selectionStartPoint;
        var end = selection.m_SelectionEndPoint;

        if (start.y < 0 || end.y < 0)
        {
            Logging.Error("Start position height must be over 0.");
            return;
        }

        if (args.Count > 1 && args[1] == "proc")
        {
            var seed = DateTime.Now.GetHashCode();

            if (args.Count == 3)
            {
                seed = args[3].GetHashCode();
            }

            RoomCellular(seed, selection, blockValue, _density);
        }
        else
        {
            RoomEmpty(start, end, blockValue, _density);
        }
    }

    private void RoomCellular(int seed, BlockToolSelection selection, BlockValue blockValue, sbyte _density)
    {
        var start = selection.SelectionMin;
        var size = selection.SelectionSize;

        var list = new List<BlockChangeInfo>();

        var prefab = new CavePrefab(0)
        {
            Size = size,
            position = start,
        };
        prefab.UpdateMarkers(new System.Random(seed));
        var room = new CaveRoom(prefab, seed);

        foreach (var pos in room.GetBlocks(invert: true))
        {
            list.Add(new BlockChangeInfo(pos, blockValue, _density));
        }

        GameManager.Instance.SetBlocksRPC(list);
    }

    private void RoomEmpty(Vector3i start, Vector3i end, BlockValue blockValue, sbyte _density)
    {
        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        foreach (var pos in BlockSelectionUtils.BrowseSelectionPositions())
        {
            BlockChangeInfo blockChangeInfo = new BlockChangeInfo(pos, blockValue, _density)
            {
                // textureFull = _textureFull,
                bChangeTexture = true
            };

            bool bound_x = pos.x == start.x || pos.x == end.x;
            bool bound_y = pos.y == start.y || pos.y == end.y;
            bool bound_z = pos.z == start.z || pos.z == end.z;

            if (bound_x || bound_y || bound_z)
            {
                list.Add(blockChangeInfo);
            }
        }

        GameManager.Instance.SetBlocksRPC(list);
    }

    private void CreateCommand()
    {
        PrefabEditModeManager.Instance.NewVoxelPrefab();

        var prefabInstance = GetCurrentPrefab();

        prefabInstance.prefab.editorGroups.Add("cave");
        prefabInstance.prefab.Tags = CaveTags.tagCave;
        // prefabInstance.prefab.editorGroups.Add(playername);
    }

    private void TagsCommand(List<string> args)
    {
        var prefabInstance = GetCurrentPrefab();

        if (!prefabInstance.prefab.editorGroups.Contains("cave"))
        {
            prefabInstance.prefab.editorGroups.Add("cave");
        }

        prefabInstance.prefab.Tags |= CaveTags.tagCave;

        if (args.Count == 2)
        {

            switch (args[1].ToLower())
            {
                case "entrance":
                    prefabInstance.prefab.Tags |= CaveTags.tagCaveEntrance;
                    break;

                case "underground":
                case "ug":
                    prefabInstance.prefab.Tags |= CaveTags.tagUnderground;
                    break;

                default:
                    Logging.Warning($"invalid tag: '{args[1]}'");
                    break;
            }
        }

        Logging.Info($"cave prefab tag success: '{prefabInstance.prefab.tags}'");
    }

    private void StalactiteCommand(List<string> args)
    {
        var selection = BlockToolSelection.Instance;
        var start = selection.SelectionStart;

        if (!selection.SelectionActive)
        {
            Logging.Error("Selection box is empty");
            return;
        }

        if (args.Count == 1)
        {
            StalactiteGenerator.Generate(start);
        }
        else if (int.TryParse(args[1], out var height))
        {
            StalactiteGenerator.Generate(start, height);
        }
        else
        {
            Logging.Error($"Invalid height: '{args[1]}'");
            return;
        }
    }

    private void ReplaceAllCommand()
    {
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        ItemValue holdingItemItemValue = primaryPlayer.inventory.holdingItemItemValue;
        BlockValue blockValue = holdingItemItemValue.ToBlockValue();

        if (blockValue.isair)
        {
            Logging.Error($"Invalid filler block: '{holdingItemItemValue.ItemClass.Name}'");
            return;
        }

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        var _gm = GameManager.Instance;

        foreach (var position in BlockSelectionUtils.BrowseSelectionPositions())
        {
            var worldBlock = _gm.World.GetBlock(position);

            if (worldBlock.isWater || worldBlock.isair)
                continue;

            var clusterIndex = _gm.World.ChunkCache.ClusterIdx;
            var _density = _gm.World.GetDensity(clusterIndex, position);

            BlockChangeInfo blockChangeInfo = new BlockChangeInfo(position, blockValue, MarchingCubes.DensityTerrain);

            list.Add(blockChangeInfo);
        }

        _gm.SetBlocksRPC(list);
    }

    private void RenameCommand(List<string> args)
    {
        if (args.Count < 2)
        {
            Logging.Error("[RenameCommand] No name was given");
            return;
        }

        var pattern = @"[0-9a-zA-Z_-]+_[0-9a-zA-Z_-]+_[0-9a-zA-Z_-]+";
        var newName = args[1];

        if (!Regex.IsMatch(newName, pattern))
        {
            Logging.Warning("[RenameCommand] Naming convention not respected: <author>_<prefab type>_<identifier>");
        }

        var prefab = GetCurrentPrefab().prefab;
        var currentName = prefab.PrefabName;
        var dirName = prefab.location.Folder;
        var newLocation = new PathAbstractions.AbstractedLocation(
            _type: PathAbstractions.EAbstractedLocationType.UserDataPath,
            _name: newName,
            _fullPath: $"{dirName}/{newName}.tts",
            _relativePath: "",
            _isFolder: false
        );

        if (File.Exists(newLocation.FullPath))
        {
            Logging.Error($"[RenameCommand] A prefab named '{newName}' already exists.");
            return;
        }

        foreach (var path in Directory.GetFiles(dirName))
        {
            var filename = Path.GetFileName(path).Split('.')[0];
            var extension = Path.GetFileName(path).Replace(filename, "");
            var newPath = $"{dirName}/{newName}{extension}";

            if (filename.StartsWith(currentName))
            {
                Logging.Info($"{path} -> {newPath}");
                File.Move(path, newPath);
            }
        }

        var loadedPrefabs = PrefabEditModeManager.Instance.loadedPrefabHeaders;

        loadedPrefabs[newLocation] = prefab;
        prefab.location = newLocation;

        if (loadedPrefabs.ContainsKey(prefab.location))
            loadedPrefabs.Remove(prefab.location);

    }

    private void NotImplementedCommand(string commandName)
    {
        Logging.Error($"Not implemented command: '{commandName}'");
    }

    private void SetMarkerDirectionCommand(List<string> args)
    {
        var logger = Logging.CreateLogger("SetMarkerDirection");
        var marker = BlockSelectionUtils.GetSelectedMarker();

        if (marker is null)
        {
            logger.Warning("No marker is selected");
            return;
        }

        if (args.Count == 0)
        {
            logger.Error("Missing argument: direction");
            return;
        }

        var direction = args[1][0].ToString().ToLower();

        if (!markerDirectionsMapping.ContainsKey(direction))
        {
            logger.Error($"Invalid direction '{direction}', should be 'n', 's', 'e' or 'w'");
            return;
        }

        marker.rotations = markerDirectionsMapping[direction];
        SelectionBoxManager.Instance.SetFacingDirection("POIMarker", marker.name, marker.rotations * -90);
        PrefabEditModeManager.Instance.NeedsSaving = true;

        logger.Info("set marker direction!");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (!PrefabEditModeManager.Instance.IsActive())
        {
            Logging.Info("Command available in prefab editor only.");
            return;
        }

        if (_params.Count == 0)
        {
            Logging.Info(getDescription());
            return;
        }

        var command = _params[0];

        switch (_params[0].ToLower())
        {
            case "marker":
            case "mark":
            case "cavemarker":
            case "cm":
                CaveMarkerCommand(_params);
                break;

            case "replaceterrain":
            case "rt":
                ReplaceTerrainCommand();
                break;

            case "replaceground":
            case "rg":
                ReplaceGroundCommand();
                break;

            case "check":
                NotImplementedCommand(command);
                break;

            case "tag":
            case "tags":
                TagsCommand(_params);
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
                StalactiteCommand(_params);
                break;

            case "room":
                RoomCommand(_params);
                break;

            case "extend":
                NotImplementedCommand(command);
                break;

            case "selectall":
            case "sa":
                SelectAllCommand();
                break;

            case "replaceall":
            case "ra":
                ReplaceAllCommand();
                break;

            case "rename":
                RenameCommand(_params);
                break;

            case "setwater":
            case "sw":
                SetWaterCommand(_params);
                break;

            case "setmarkerdirection":
            case "smd":
                SetMarkerDirectionCommand(_params);
                break;

            case "create":
                CreateCommand();
                break;

            default:
                Logging.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}