using System.Collections.Generic;
using System.Linq;

public class BoundingBox
{
    public BoundingBox parent;

    public Vector3i start;

    public Vector3i size;

    public int blocksCount;

    public float Density => (float)blocksCount / (size.x * size.y * size.z);

    public BoundingBox(BoundingBox parent, Vector3i start, Vector3i size)
    {
        this.parent = parent;
        this.start = start;
        this.size = size;
    }

    public BoundingBox(Vector3i start, Vector3i size)
    {
        this.start = start;
        this.size = size;
    }

    public BoundingBox(Vector3i start, Vector3i size, int blocksCount)
    {
        this.start = start;
        this.size = size;
        this.blocksCount = blocksCount;
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
            new BoundingBox(this, start, halfSize),
            new BoundingBox(this, new Vector3i(start.x + halfSize.x, start.y, start.z), new Vector3i(remainder.x, halfSize.y, halfSize.z)),
            new BoundingBox(this, new Vector3i(start.x, start.y + halfSize.y, start.z), new Vector3i(halfSize.x, remainder.y, halfSize.z)),
            new BoundingBox(this, new Vector3i(start.x + halfSize.x, start.y + halfSize.y, start.z), new Vector3i(remainder.x, remainder.y, halfSize.z)),
            new BoundingBox(this, new Vector3i(start.x, start.y, start.z + halfSize.z), new Vector3i(halfSize.x, halfSize.y, remainder.z)),
            new BoundingBox(this, new Vector3i(start.x + halfSize.x, start.y, start.z + halfSize.z), new Vector3i(remainder.x, halfSize.y, remainder.z)),
            new BoundingBox(this, new Vector3i(start.x, start.y + halfSize.y, start.z + halfSize.z), new Vector3i(halfSize.x, remainder.y, remainder.z)),
            new BoundingBox(this, new Vector3i(start.x + halfSize.x, start.y + halfSize.y, start.z + halfSize.z), remainder)
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

    public int MaxSize()
    {
        if (size.x > size.y && size.x > size.z)
            return size.x;

        if (size.y > size.z)
            return size.y;

        return size.z;
    }

    public int MinSize()
    {
        if (size.x < size.y && size.x < size.z)
            return size.x;

        if (size.y < size.z)
            return size.y;

        return size.z;
    }

    public override int GetHashCode()
    {
        return start.GetHashCode() + size.GetHashCode();
    }

    public override string ToString()
    {
        return $"start: [{start}], size: [{size}]";
    }

    public Vector3i RotateCoords(Vector3i coord, int rotation, Vector3i parentSize)
    {
        // TODO: switch to Prefab.RotatePOIMarkers if needed

        var _x = coord.x;
        var _z = coord.z;

        switch (rotation)
        {
            case 3:
                {
                    int num = _x;
                    _x = _z;
                    _z = parentSize.x - num - 1;
                    break;
                }
            case 2:
                _x = parentSize.x - _x - 1;
                _z = parentSize.z - _z - 1;
                break;
            case 1:
                {
                    int num = _x;
                    _x = parentSize.z - _z - 1;
                    _z = num;
                    break;
                }
        }

        return new Vector3i(_x, coord.y, _z);
    }

    public BoundingBox Transform(Vector3i position, byte rotation, Vector3i parentSize)
    {
        var start = RotateCoords(this.start, rotation, parentSize) + position;
        var end = RotateCoords(this.start + size, rotation, parentSize) + position;

        return new BoundingBox(null, start, end - start);
    }

}
