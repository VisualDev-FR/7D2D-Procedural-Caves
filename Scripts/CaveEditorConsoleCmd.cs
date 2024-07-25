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
            marker: Add a cave marker into the selection.
        ";
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

        switch (_params[0].ToLower())
        {
            case "marker":
                AddCaveMarkerToSelection();
                break;

            default:
                break;
        }
    }

}