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
            - marker: Add a cave marker into the selection.
            - replaceterrain, rt: Replace all terrain blocks in the selection with the selected item.
            - save: special save method which will store all air blocks as caveAir blocks.
            - check: create a report of the requirements for getting a valid cave prefab.
            - tags <type>: Add the required tags to get a valid cave prefab. Type is optional an accept the following keywords:
                * entrance -> the prefab is a cave entrance
                * underwater -> the prefab is an underwater entrance
            - procfill: Create a procedural cave volume into the selection (min selection size = 20x20x20).
            - water: auto fill terrain with water, with selection as start.
            - decorate: Decorate terrain with items specfied in config files.
            - tunnel <marker1> <marker2>: Create a tunnel between two specified cave markers.
            - stalactite <height>: Creates a procedural stalactite of the specified height at the start position of the selection.
            - extend <x> <y> <z>: extend the selection of x blocks in the x direction, etc ...
            - selectall, sa: add all the prefab volume to the selection.
        ";
    }

    public override string getHelp()
    {
        return getDescription();
    }

    private void AddCaveMarkerToSelection()
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

    private void ReplaceTerrainWithSelectedItem()
    {
        Log.Error("Not Implemented.");
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
                AddCaveMarkerToSelection();
                break;

            case "replace":
            case "replaceterrain":
            case "rt":
                ReplaceTerrainWithSelectedItem();
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

            case "extend":
                NotImplementedCommand(command);
                break;

            case "selectall":
            case "sa":
                NotImplementedCommand(command);
                break;

            default:
                Log.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}