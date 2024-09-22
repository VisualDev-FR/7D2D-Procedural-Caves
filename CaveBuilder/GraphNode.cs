using System.Collections.Generic;
using System.Linq;

public class GraphNode
{
    public Vector3i position;

    public CavePrefab prefab;

    public Direction direction;

    public Prefab.Marker marker;

    public int PrefabID => prefab.id;

    public int NodeRadius;

    public int sqrRadius;

    public GraphNode(Prefab.Marker marker, CavePrefab prefab)
    {
        this.marker = marker;
        this.prefab = prefab;

        CaveUtils.Assert(marker != null, $"null marker");
        CaveUtils.Assert(marker.size != null, $"null marker size");

        NodeRadius = CaveUtils.FastMax(1, CaveUtils.FastMin(CaveUtils.FastMax(marker.size.x, marker.size.z), marker.size.y) / 2);

        sqrRadius = NodeRadius * NodeRadius;

        // TODO: find a way to ensure that the node is in the marker volume
        position = new Vector3i(
            (int)(prefab.position.x + marker.start.x + marker.size.x / 2f),
            (int)(prefab.position.y + marker.start.y + marker.size.y / 2f),
            (int)(prefab.position.z + marker.start.z + marker.size.z / 2f)
        );
        direction = GetDirection();

        CaveUtils.Assert(direction != Direction.None, $"None direction: {prefab.Name}, marker start: [{marker.start}], prefab size:[{prefab.Size}]");
    }

    public static Vector3i MarkerCenter(Prefab.Marker marker)
    {
        return new Vector3i(
            (int)(marker.start.x + marker.size.x / 2f),
            (int)(marker.start.y + marker.size.y / 2f),
            (int)(marker.start.z + marker.size.z / 2f)
        );
    }

    private Direction GetDirection()
    {
        if (marker.start.x == -1)
            return Direction.North;

        if (marker.start.x == prefab.Size.x)
            return Direction.South;

        if (marker.start.z == -1)
            return Direction.West;

        if (marker.start.z == prefab.Size.z)
            return Direction.East;

        return Direction.None;
    }

    public Vector3i Normal(int distance)
    {
        CaveUtils.Assert(direction != Direction.None, $"Direction sould not be None");

        return position + direction.Vector * distance;
    }

    public IEnumerable<Vector3i> GetMarkerPoints()
    {
        var p1 = prefab.position + marker.start;
        var p2 = p1 + marker.size;

        for (int x = p1.x; x < p2.x; x++)
        {
            for (int y = p1.y; y < p2.y; y++)
            {
                for (int z = p1.z; z < p2.z; z++)
                {
                    yield return new Vector3i(x, y, z);
                }
            }
        }
    }

    public IEnumerable<CaveBlock> GetSphere()
    {
        var center = position;
        var queue = new HashSet<Vector3i>() { center };
        var visited = new HashSet<Vector3i>();
        var index = 100_000;

        var markerStart = prefab.position + marker.start;
        var markerEnd = markerStart + marker.size;
        var radius = CaveUtils.FastMax(5, CaveUtils.FastMax(marker.size.x, marker.size.z, marker.size.y) / 2);
        var sqrRadius = radius * radius;

        CaveUtils.Assert(radius >= 5, $"marker radius should be over 5: {radius}");
        CaveUtils.Assert(!prefab.Intersect3D(center), $"Marker {marker.start} intersect with prefab {prefab.Name}");

        while (queue.Count > 0 && index-- > 0)
        {
            Vector3i currentPosition = queue.First();

            visited.Add(currentPosition);
            queue.Remove(currentPosition);

            yield return new CaveBlock(currentPosition);

            // TODO: use tunnels.GetSphere
            foreach (Vector3i offset in CaveUtils.offsets)
            {
                Vector3i pos = currentPosition + offset;

                bool shouldContinue =
                    visited.Contains(pos)
                    || prefab.Intersect2D(pos)
                    || pos.y < markerStart.y || pos.y >= markerEnd.y
                    || (direction.Vector.x == 0 && (pos.x < markerStart.x || pos.x >= markerEnd.x))
                    || (direction.Vector.z == 0 && (pos.z < markerStart.z || pos.z >= markerEnd.z))
                    || CaveUtils.SqrEuclidianDist(pos, center) >= sqrRadius;

                if (!shouldContinue)
                {
                    queue.Add(pos);
                }
            }
        }

        CaveUtils.Assert(queue.Count == 0, "Infinite loop detected.");

        yield break;
    }

    public override string ToString()
    {
        return position.ToString();
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        GraphNode other = (GraphNode)obj;
        return position.Equals(other.position);
    }

}
