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

    public BoundingBox[] Octree(int minSize)
    {
        Vector3i halfSize = new Vector3i(
            size.x > minSize ? size.x / 2 : size.x,
            size.y > minSize ? size.y / 2 : size.y,
            size.z > minSize ? size.z / 2 : size.z
        );

        List<BoundingBox> octants = new List<BoundingBox>
        {
            new BoundingBox(start, halfSize),
            new BoundingBox(new Vector3i(start.x + (size.x > minSize ? halfSize.x : 0), start.y, start.z), halfSize),
            new BoundingBox(new Vector3i(start.x, start.y + (size.y > minSize ? halfSize.y : 0), start.z), halfSize),
            new BoundingBox(new Vector3i(start.x + (size.x > minSize ? halfSize.x : 0), start.y + (size.y > minSize ? halfSize.y : 0), start.z), halfSize),
            new BoundingBox(new Vector3i(start.x, start.y, start.z + (size.z > minSize ? halfSize.z : 0)), halfSize),
            new BoundingBox(new Vector3i(start.x + (size.x > minSize ? halfSize.x : 0), start.y, start.z + (size.z > minSize ? halfSize.z : 0)), halfSize),
            new BoundingBox(new Vector3i(start.x, start.y + (size.y > minSize ? halfSize.y : 0), start.z + (size.z > minSize ? halfSize.z : 0)), halfSize),
            new BoundingBox(new Vector3i(start.x + (size.x > minSize ? halfSize.x : 0), start.y + (size.y > minSize ? halfSize.y : 0), start.z + (size.z > minSize ? halfSize.z : 0)), halfSize)
        };

        return octants
            .Where(octant => octant.size.x >= minSize && octant.size.y >= minSize && octant.size.z >= minSize)
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
