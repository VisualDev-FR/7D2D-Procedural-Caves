using System;
using System.Collections.Generic;
using System.Linq;

public class BoundingBox
{
    public Vector3i start;
    public Vector3i size;

    public BoundingBox(Vector3i start, Vector3i size)
    {
        this.start = start;
        this.size = size;
    }

    public BoundingBox[] Octree()
    {
        Vector3i halfSize = new Vector3i(
            size.x > 1 ? size.x / 2 : 1,
            size.y > 1 ? size.y / 2 : 1,
            size.z > 1 ? size.z / 2 : 1
        );

        Vector3i remainder = new Vector3i(
            size.x > 1 ? size.x - halfSize.x : 0,
            size.y > 1 ? size.y - halfSize.y : 0,
            size.z > 1 ? size.z - halfSize.z : 0
        );

        List<BoundingBox> octants = new List<BoundingBox>
        {
            new BoundingBox(start, halfSize),
            new BoundingBox(new Vector3i(start.x + halfSize.x, start.y, start.z), new Vector3i(remainder.x, halfSize.y, halfSize.z)),
            new BoundingBox(new Vector3i(start.x, start.y + halfSize.y, start.z), new Vector3i(halfSize.x, remainder.y, halfSize.z)),
            new BoundingBox(new Vector3i(start.x + halfSize.x, start.y + halfSize.y, start.z), new Vector3i(remainder.x, remainder.y, halfSize.z)),
            new BoundingBox(new Vector3i(start.x, start.y, start.z + halfSize.z), new Vector3i(halfSize.x, halfSize.y, remainder.z)),
            new BoundingBox(new Vector3i(start.x + halfSize.x, start.y, start.z + halfSize.z), new Vector3i(remainder.x, halfSize.y, remainder.z)),
            new BoundingBox(new Vector3i(start.x, start.y + halfSize.y, start.z + halfSize.z), new Vector3i(halfSize.x, remainder.y, remainder.z)),
            new BoundingBox(new Vector3i(start.x + halfSize.x, start.y + halfSize.y, start.z + halfSize.z), remainder)
        };

        return octants
            .Where(bb => bb.size.x > 0 && bb.size.y > 0 && bb.size.z > 0)
            .ToArray();
    }

    public IEnumerable<Vector3i> IteratePoints()
    {
        for (int x = start.x; x < start.x + size.x; x++)
        {
            for (int y = start.y; y < start.y + size.y; y++)
            {
                for (int z = start.z; z < start.z + size.z; z++)
                {
                    yield return new Vector3i(x, y, z);
                }
            }

        }
    }

}
