using System;
using System.Collections.Generic;


public class BlockSelectionUtils
{
    public static readonly string selectionBoxCategory = "BlockSelectionUtils";

    public static readonly List<string> activeBoxNames = new List<string>();

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

    public static void SelectBox(BoundingBox box)
    {
        SelectBoxes(new List<BoundingBox>() { box });
    }

    public static SelectionCategory GetSelectionCategory()
    {
        var sbm = SelectionBoxManager.Instance;

        if (!sbm.categories.ContainsKey(selectionBoxCategory))
        {
            sbm.CreateCategory(
                _name: selectionBoxCategory,
                _colSelected: SelectionBoxManager.ColSelectionActive,
                _colUnselected: SelectionBoxManager.ColSelectionInactive,
                _colFaceSelected: SelectionBoxManager.ColSelectionFaceSel,
                _bCollider: false,
                _tag: null
            );
        }

        return sbm.categories[selectionBoxCategory];
    }

    public static void SelectBoxes(List<BoundingBox> boxes)
    {
        var selectionCat = GetSelectionCategory();

        foreach (var bb in boxes)
        {
            string boxName = bb.ToString();

            SelectionBox box = selectionCat.AddBox(boxName, bb.start, bb.size);
            box.SetVisible(true);
            box.SetSizeVisibility(_visible: true);

            selectionCat.SetVisible(true);
            // SelectionBoxManager.Instance.activate(selectionCat, box);

            activeBoxNames.Add(boxName);
        }
    }

    public static void ClearSelection()
    {
        var selectionCat = GetSelectionCategory();

        foreach (var name in activeBoxNames)
        {
            selectionCat.RemoveBox(name);
        }

        activeBoxNames.Clear();
    }

    public static Vector3i GetSelectionPosition()
    {
        return BlockToolSelection.Instance.m_selectionStartPoint;
    }

    public static Prefab.Marker GetSelectedMarker()
    {
        if (POIMarkerToolManager.currentSelectionBox != null && POIMarkerToolManager.currentSelectionBox.UserData is Prefab.Marker marker)
        {
            return marker;
        }

        return null;
    }

}