using System;
using System.Collections.Generic;


public class BlockSelectionUtils
{
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

}