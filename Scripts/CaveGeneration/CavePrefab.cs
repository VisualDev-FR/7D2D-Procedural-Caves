using System.Collections.Generic;
using System.Linq;
using WorldGenerationEngineFinal;
using Random = System.Random;

public class CavePrefab
{
    public PrefabDataInstance prefabDataInstance;

    public Vector3i position;

    private Vector3i _size;

    public Vector3i Size
    {
        get => _size;

        set
        {
            _size = value;

            int dx = _size.x / 2;
            int dy = _size.y / 2;
            int dz = _size.z / 2;

            BoundingRadiusSqr = dx * dx + dy * dy + dz * dz;
        }
    }

    public int id;

    public byte rotation;

    public int BoundingRadiusSqr { get; internal set; }

    public string Name => prefabDataInstance?.prefab.Name;

    public FastTags<TagGroup.Poi> Tags => prefabDataInstance == null ? FastTags<TagGroup.Poi>.none : prefabDataInstance.prefab.Tags;

    public bool isEntrance => Tags.Test_AnySet(CaveConfig.tagCaveEntrance);

    public bool isRoom = false;

    public bool isBoundaryPrefab = false;

    public List<GraphNode> nodes;

    public List<Prefab.Marker> caveMarkers;

    public CavePrefab(int index)
    {
        id = index;
        nodes = new List<GraphNode>();
    }

    public CavePrefab(BoundingBox rectangle)
    {
        position = rectangle.start;
        Size = rectangle.size;
        caveMarkers = new List<Prefab.Marker>();
    }

    public CavePrefab(int index, PrefabDataInstance pdi, Vector3i offset)
    {
        id = index;
        rotation = pdi.rotation;
        prefabDataInstance = pdi;
        position = pdi.boundingBoxPosition + offset;
        Size = CaveUtils.GetRotatedSize(pdi.boundingBoxSize, rotation);

        UpdateMarkers(pdi);
    }

    public CavePrefab(int index, Vector3i position, Random rand, int markerCount)
    {
        this.position = position;

        id = index;
        nodes = new List<GraphNode>();

        int minPrefabSize = 8;
        int maxPrefabSize = 100;

        Size = new Vector3i(
            rand.Next(minPrefabSize, maxPrefabSize),
            rand.Next(minPrefabSize, maxPrefabSize),
            rand.Next(minPrefabSize, maxPrefabSize)
        );

        UpdateMarkers(rand, markerCount);
    }

    public Prefab.Marker RandomMarker(Random rand, int rotation, int xMax, int yMax, int zMax, bool aligned = true)
    {
        var markerType = Prefab.Marker.MarkerTypes.None;
        var tags = FastTags<TagGroup.Poi>.none;
        var groupName = "";

        // var maxMarkerSize = 10;
        int sizeX = Utils.FastMin(5, xMax); // rand.Next(Utils.FastMin(2, xMax), Utils.FastMin(maxMarkerSize, xMax));
        int sizeY = Utils.FastMin(5, yMax); // rand.Next(Utils.FastMin(2, yMax), Utils.FastMin(maxMarkerSize, yMax));
        int sizeZ = Utils.FastMin(5, zMax); // rand.Next(Utils.FastMin(2, zMax), Utils.FastMin(maxMarkerSize, zMax));

        int px = aligned ? Size.x / 2 : rand.Next(1, xMax);
        int pz = aligned ? Size.z / 2 : rand.Next(1, zMax);
        int py = Size.y / 2;

        switch (rotation)
        {
            case 0:
                pz = -1;
                break;

            case 1:
                pz = Size.z;
                break;

            case 2:
                px = -1;
                break;

            case 3:
                px = Size.x;
                break;
        }

        var markerStart = new Vector3i(px, py, pz);
        var markerSize = new Vector3i(sizeX, sizeY, sizeZ);

        return new Prefab.Marker(markerStart, markerSize, markerType, groupName, tags);
    }

    public void UpdateMarkers(PrefabDataInstance pdi)
    {
        nodes = new List<GraphNode>();
        caveMarkers = new List<Prefab.Marker>();

        foreach (var marker in pdi.prefab.RotatePOIMarkers(true, rotation))
        {
            if (!marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
                continue;

            caveMarkers.Add(marker);
            nodes.Add(new GraphNode(marker, this));
        }
    }

    public void UpdateMarkers(Random rand, int markerCount)
    {
        caveMarkers = new List<Prefab.Marker>();

        var positions = new HashSet<Vector3i>();
        var addedMarkers = 0;

        while (addedMarkers < markerCount)
        {
            int xMax = 0;
            int zMax = 0;
            int rotation = rand.Next(4);

            switch (rotation)
            {
                case 0:
                case 1:
                    xMax = Size.x - 2;
                    zMax = 1;
                    break;

                case 2:
                case 3:
                    xMax = 1;
                    zMax = Size.z - 2;
                    break;
            }

            var marker = RandomMarker(rand, rotation, xMax, Size.y, zMax, false);

            if (!positions.Contains(marker.start))
            {
                positions.Add(marker.Start);
                caveMarkers.Add(marker);
                addedMarkers++;
            }
        }

        UpdateMarkers(caveMarkers);
    }

    public void UpdateMarkers(Random rand)
    {
        caveMarkers = new List<Prefab.Marker>(){
            RandomMarker(rand, 0, Size.x - 2, Size.y, 1),
            RandomMarker(rand, 1, Size.x - 2, Size.y, 1),
            RandomMarker(rand, 2, 1, Size.y, Size.z - 2),
            RandomMarker(rand, 3, 1, Size.y, Size.z - 2),
        };

        UpdateMarkers(caveMarkers);
    }

    public void UpdateMarkers(List<Prefab.Marker> markers)
    {
        nodes = new List<GraphNode>();

        foreach (var marker in markers)
        {
            CaveUtils.Assert(marker != null, "null marker");
            CaveUtils.Assert(marker.size != null, "null marker size");

            nodes.Add(new GraphNode(marker, this));
        }
    }

    public void RemoveMarker(Direction direction)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].direction.Equals(direction))
            {
                nodes.RemoveAt(i);
            }
        }
    }

    public HashSet<Vector3i> GetMarkerPoints()
    {
        var result = new HashSet<Vector3i>();

        for (int i = 0; i < caveMarkers.Count; i++)
        {
            var marker = caveMarkers[i];
            var markerPoints = CaveUtils.GetPointsInside(position + marker.start, position + marker.start + marker.size);

            result.UnionWith(markerPoints);
        }

        return result;
    }

    public void SetRandomPosition(RawHeightMap heightMap, Random rand, int mapSize)
    {
        int offset = CaveConfig.radiationSize + CaveConfig.radiationZoneMargin;

        position = new Vector3i(
            rand.Next(offset, mapSize - offset - Size.x),
            0,
            rand.Next(offset, mapSize - offset - Size.z)
        );

        position.y = rand.Next(CaveConfig.bedRockMargin, (int)(heightMap.GetHeight(position.x, position.y) - Size.y));

        foreach (var node in nodes)
        {
            node.position = position + node.marker.Start;
        }
    }

    public bool OverLaps2D(CavePrefab other, int margin)
    {
        return CaveUtils.OverLaps2D(position, Size, other.position, other.Size, margin);
    }

    public bool OverLaps2D(List<CavePrefab> others, int margin)
    {
        foreach (var prefab in others)
        {
            if (OverLaps2D(prefab, margin))
                return true;
        }

        return false;
    }

    public bool Intersect2D(Vector3i point)
    {
        if (point.x < position.x)
            return false;

        if (point.x >= position.x + Size.x)
            return false;

        if (point.z < position.z)
            return false;

        if (point.z >= position.z + Size.z)
            return false;

        return true;
    }

    public bool Intersect3D(Vector3i pos)
    {
        if (!Intersect2D(pos))
            return false;

        if (pos.y < position.y)
            return false;

        if (pos.y >= position.y + Size.z)
            return false;

        return true;
    }

    private List<Vector3i> GetBoundingPoints()
    {
        var points = new HashSet<Vector3i>();

        int x0 = position.x;
        int y0 = position.y;
        int z0 = position.z;

        int x1 = x0 + Size.x;
        int y1 = y0 + Size.y;
        int z1 = z0 + Size.z;

        for (int x = x0; x < x1; x++)
        {
            for (int y = y0; y < y1; y++)
            {
                points.Add(new Vector3i(x, y, z0));
                points.Add(new Vector3i(x, y, z1));
            }
        }

        for (int y = y0; y < y1; y++)
        {
            for (int z = z0; z < z1; z++)
            {
                points.Add(new Vector3i(x0, y, z));
                points.Add(new Vector3i(x1, y, z));
            }
        }

        for (int z = z0; z < z1; z++)
        {
            for (int x = x0; x < x1; x++)
            {
                points.Add(new Vector3i(x, y0, z));
                points.Add(new Vector3i(x, y1, z));
            }
        }

        return points.ToList();
    }

    public Vector3i GetCenter()
    {
        return new Vector3i(
            position.x + Size.x / 2,
            position.y + Size.y / 2,
            position.z + Size.z / 2
        );
    }

    public IEnumerable<int> GetOverlappingChunkHashes()
    {
        var x0chunk = position.x >> 4;
        var z0chunk = position.z >> 4;
        var x1Chunk = (position.x + Size.x - 1) >> 4;
        var z1Chunk = (position.z + Size.z - 1) >> 4;

        for (int x = x0chunk; x <= x1Chunk; x++)
        {
            for (int z = z0chunk; z <= z1Chunk; z++)
            {
                yield return CavePrefabManager.GetChunkHash(x, z);
            }
        }
    }

    public bool IntersectMarker(int x, int y, int z)
    {
        bool posIsNotOnBounds =
            x != position.x - 1 && x != position.x + Size.x &&
            z != position.z - 1 && z != position.z + Size.z;

        if (posIsNotOnBounds)
            return false;

        foreach (var marker in caveMarkers)
        {
            var start = position + marker.start;

            if (CaveUtils.Intersect3D(x, y, z, start, marker.size))
            {
                return true;
            }
        }

        return false;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var other = (CavePrefab)obj;

        return GetHashCode() == other.GetHashCode();
    }

    public IEnumerable<DelauneyPoint> DelauneyPoints()
    {
        foreach (var node in nodes)
        {
            yield return new DelauneyPoint(node);
        }
    }

}
