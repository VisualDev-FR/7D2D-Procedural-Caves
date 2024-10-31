using System;
using System.Collections.Generic;
using System.Linq;

public class BoundingBox
{
    public BoundingBox parent;

    public Vector3i start;

    public Vector3i size;

    public bool Overlaps(BoundingBox other)
    {
        bool noOverlapX = start.x + size.x <= other.start.x || other.start.x + other.size.x <= start.x;
        bool noOverlapY = start.y + size.y <= other.start.y || other.start.y + other.size.y <= start.y;
        bool noOverlapZ = start.z + size.z <= other.start.z || other.start.z + other.size.z <= start.z;

        return !(noOverlapX || noOverlapY || noOverlapZ);
    }

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

    public BoundingBox(BoundingBox other)
    {
        start = new Vector3i(other.start);
        size = new Vector3i(other.size);
        parent = other.parent;
        blocksCount = other.blocksCount;
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

    public BoundingBox Transform(PrefabInstance prefabInstance)
    {
        return Transform(
            prefabInstance.boundingBoxPosition,
            prefabInstance.rotation,
            prefabInstance.prefab.size
        );
    }

    public BoundingBox Transform(Vector3i position, byte rotation, Vector3i parentSize)
    {
        // NOTE: see ./previews/doc_bb_rotation.png

        var bb = new BoundingBox(this);

        var px = parentSize.x;
        var pz = parentSize.z;

        var x = start.x;
        var z = start.z;

        var sx = size.x;
        var sz = size.z;

        switch (rotation)
        {
            case 0:
                break;

            case 1:
                pz = parentSize.x;
                bb.start.x = pz - z;
                bb.start.z = x;
                bb.size.x = -sz;
                bb.size.z = sx;
                break;

            case 2:
                bb.start.x = px - x;
                bb.start.z = pz - z;
                bb.size.x = -sx;
                bb.size.z = -sz;
                break;

            case 3:
                px = parentSize.z;
                bb.start.x = z;
                bb.start.z = px - x;
                bb.size.x = sz;
                bb.size.z = -sx;
                break;

            default:
                throw new Exception($"Invalid rotation: '{rotation}'");
        }

        bb.start += position;

        return bb;
    }

}
