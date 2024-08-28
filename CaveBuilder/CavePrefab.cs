using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public List<GraphNode> nodes;

    public List<Prefab.Marker> caveMarkers;

    public CavePrefab(int index)
    {
        id = index;
        nodes = new List<GraphNode>();
    }

    public CavePrefab(int index, Random rand)
    {
        id = index;
        nodes = new List<GraphNode>();

        Size = new Vector3i(
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE)
        );
    }

    public CavePrefab(BoundingBox rectangle)
    {
        position = rectangle.start;
        Size = rectangle.size;
    }

    public Prefab.Marker RandomMarker(Random rand, int rotation, int xMax, int yMax, int zMax)
    {
        var markerType = Prefab.Marker.MarkerTypes.None;
        var tags = FastTags<TagGroup.Poi>.none;
        var groupName = "";
        var maxMarkerSize = 10;

        int sizeX = rand.Next(CaveUtils.FastMin(2, xMax), CaveUtils.FastMin(maxMarkerSize, xMax));
        int sizeY = rand.Next(CaveUtils.FastMin(2, yMax), CaveUtils.FastMin(maxMarkerSize, yMax));
        int sizeZ = rand.Next(CaveUtils.FastMin(2, zMax), CaveUtils.FastMin(maxMarkerSize, zMax));

        int px = rand.Next(Size.x - sizeX);
        int py = rand.Next(Size.y - sizeY);
        int pz = rand.Next(Size.z - sizeZ);

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

    public CavePrefab(int index, PrefabDataInstance pdi, Vector3i offset)
    {
        id = index;
        rotation = pdi.rotation;
        prefabDataInstance = pdi;
        position = pdi.boundingBoxPosition + offset;
        Size = CaveUtils.GetRotatedSize(pdi.boundingBoxSize, rotation);

        CaveUtils.Assert(position.x > 0, $"offset: {offset}");
        CaveUtils.Assert(position.y > 0, $"offset: {offset}");
        CaveUtils.Assert(position.z > 0, $"offset: {offset}");

        UpdateMarkers(pdi);
    }

    public void UpdateMarkers(PrefabDataInstance pdi)
    {
        nodes = new List<GraphNode>();
        caveMarkers = new List<Prefab.Marker>();

        // if (pdi.prefab.POIMarkers.Count > 0 && pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
        // {
        //     Log.Warning($"prefab {pdi.prefab.Name} has not cave marker.");
        // }

        foreach (var marker in pdi.prefab.RotatePOIMarkers(true, rotation))
        {
            if (!marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
                continue;

            caveMarkers.Add(marker);
            nodes.Add(new GraphNode(marker, this));
        }
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

    public void SetRandomPosition(Random rand, int mapSize)
    {
        int offset = CaveBuilder.radiationSize + CaveBuilder.radiationZoneMargin;

        position = new Vector3i(
            rand.Next(offset, mapSize - offset - Size.x),
            0,
            rand.Next(offset, mapSize - offset - Size.z)
        );

        position.y = rand.Next(CaveBuilder.bedRockMargin, (int)(WorldBuilder.Instance.GetHeight(position.x, position.y) - Size.y));

        UpdateMarkers(rand);
    }

    public bool OverLaps2D(CavePrefab other)
    {
        return CaveUtils.OverLaps2D(position, Size, other.position, other.Size);
    }

    public bool OverLaps2D(List<CavePrefab> others)
    {
        foreach (var prefab in others)
        {
            if (OverLaps2D(prefab))
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

    public int CountIntersections(Segment segment)
    {
        int intersectionsCount = 0;

        int x0 = position.x;
        int z0 = position.z;

        int x1 = x0 + Size.x;
        int z1 = z0 + Size.z;

        var edges = new List<Segment>(){
            new Segment(x0, z0, x0, z1),
            new Segment(x0, z0, x1, z0),
            new Segment(x1, z1, x0, z1),
            new Segment(x1, z1, x1, z0),
        };

        foreach (var edge in edges)
        {
            if (segment.Intersect(edge))
            {
                intersectionsCount++;
            }
        }

        return intersectionsCount;
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

    public HashSet<Vector3i> CreateBoundNoise(Vector3i center, int radius)
    {
        var queue = new HashSet<Vector3i>() { center };
        var visited = new HashSet<Vector3i>();

        while (queue.Count > 0)
        {
            foreach (var pos in queue.ToArray())
            {
                queue.Remove(pos);

                if (visited.Contains(pos))
                    continue;

                if (Intersect3D(pos))
                    continue;

                visited.Add(pos);

                if (CaveUtils.SqrEuclidianDist(pos, center) >= radius)
                    continue;

                queue.UnionWith(CaveUtils.GetValidNeighbors(pos));
            }
        }

        return visited;
    }

    public HashSet<Vector3i> GetBoundingNoise()
    {
        var boundingPoints = GetBoundingPoints();
        var coveredPoints = boundingPoints.ToHashSet();
        var noiseMap = new HashSet<Vector3i>();

        int rolls = 0;

        while (coveredPoints.Count > 0)
        {
            rolls++;

            int index = CaveBuilder.rand.Next(boundingPoints.Count);
            int radius = CaveBuilder.rand.Next(5, 10);

            Vector3i center = boundingPoints[index];

            var noise = CreateBoundNoise(center, radius); // CaveBuilder.ParseCircle(center, radius)

            noiseMap.UnionWith(noise);
            coveredPoints.ExceptWith(noise);
        }

        Log.Out($"{rolls} iterations");

        return noiseMap;
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
                yield return PrefabCache.GetChunkHash(x, z);
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

}
