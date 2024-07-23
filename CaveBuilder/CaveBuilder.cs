#pragma warning disable CS0162, CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305, IDE0035


using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using WorldGenerationEngineFinal;

using Random = System.Random;
using System.IO;
using System.Numerics;


public class PlaneEquation
{
    private int A, B, C, D;

    public PlaneEquation(Vector3i p1, Vector3i p2, Vector3i p3)
    {
        var v1 = new Vector3i(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
        var v2 = new Vector3i(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z);

        A = v1.y * v2.z - v1.z * v2.y;
        B = v1.z * v2.x - v1.x * v2.z;
        C = v1.x * v2.y - v1.y * v2.x;
        D = -(A * p1.x + B * p1.y + C * p1.z);
    }

    public int GetHeight(int x, int z)
    {
        return -(A * x + C * z + D) / B;
    }
}


public struct Segment
{
    public Vector3i P1;
    public Vector3i P2;

    public Segment(Vector3i p1, Vector3i p2)
    {
        P1 = p1;
        P2 = p2;
    }

    public Segment(int x0, int z0, int x1, int z1)
    {
        P1 = new Vector3i(x0, 0, z0);
        P2 = new Vector3i(x1, 0, z1);
    }

    public bool Intersect(Segment other)
    {
        Vector3i p1 = P1, q1 = P2;
        Vector3i p2 = other.P1, q2 = other.P2;

        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        if (o1 != o2 && o3 != o4)
            return true;

        if (o1 == 0 && OnSegment(p1, p2, q1))
            return true;

        if (o2 == 0 && OnSegment(p1, q2, q1))
            return true;

        if (o3 == 0 && OnSegment(p2, p1, q2))
            return true;

        if (o4 == 0 && OnSegment(p2, q1, q2))
            return true;

        return false;
    }

    private static int Orientation(Vector3i p, Vector3i q, Vector3i r)
    {
        double val = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);

        if (val == 0)
            return 0;

        return (val > 0) ? 1 : 2;
    }

    private static bool OnSegment(Vector3i p, Vector3i q, Vector3i r)
    {
        if (q.x <= CaveUtils.FastMax(p.x, r.x) && q.x >= CaveUtils.FastMin(p.x, r.x) &&
            q.z <= CaveUtils.FastMax(p.z, r.z) && q.z >= CaveUtils.FastMin(p.z, r.z))
            return true;

        return false;
    }
}


public static class CaveUtils
{
    public static void Assert(bool condition, string message = "")
    {
        if (!condition)
            throw new Exception($"Assertion error: {message}");
    }

    public static int FastMin(int a, int b)
    {
        return a < b ? a : b;
    }

    public static int FastMax(int a, int b)
    {
        return a > b ? a : b;
    }

    public static int FastMax(int a, int b, int c)
    {
        if (a > b && a > c)
            return a;

        if (b > c)
            return b;

        return c;
    }

    public static int FastMin(int a, int b, int c)
    {
        if (a < b && a < c)
            return a;

        if (b < c)
            return b;

        return c;
    }

    public static string TimeFormat(Stopwatch timer, string format = @"hh\:mm\:ss")
    {
        return TimeSpan.FromSeconds(timer.ElapsedMilliseconds / 1000).ToString(format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Node nodeA, Node nodeB)
    {
        return SqrEuclidianDist(nodeA.position, nodeB.position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrEuclidianDist(Vector3i p1, Vector3i p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return dx * dx + dy * dy + dz * dz;
    }

    public static float SqrEuclidianDist2D(Vector3i p1, Vector3i p2)
    {
        float dx = p1.x - p2.x;
        float dz = p1.z - p2.z;

        return dx * dx + dz * dz;
    }

    public static int FastAbs(int value)
    {
        if (value > 0)
            return value;

        return -value;
    }

    public static float EuclidianDist(Vector3i p1, Vector3i p2)
    {
        return (float)Math.Sqrt(SqrEuclidianDist(p1, p2));
    }

    public static List<Vector3i> GetValidNeighbors(Vector3i position)
    {
        var neighbors = new List<Vector3i>();

        if (position.x > 0)
            neighbors.Add(new Vector3i(position.x - 1, position.y, position.z));

        if (position.x < CaveBuilder.worldSize)
            neighbors.Add(new Vector3i(position.x + 1, position.y, position.z));

        if (position.z > 0)
            neighbors.Add(new Vector3i(position.x, position.y, position.z - 1));

        if (position.z < CaveBuilder.worldSize)
            neighbors.Add(new Vector3i(position.x, position.y, position.z + 1));

        if (position.y > CaveBuilder.bedRockMargin)
            neighbors.Add(new Vector3i(position.x, position.y - 1, position.z));

        if (position.y + CaveBuilder.terrainMargin < WorldBuilder.Instance.GetHeight(position.x, position.z))
            neighbors.Add(new Vector3i(position.x, position.y + 1, position.z));

        return neighbors;
    }

    public static Vector3i RandomVector3i(Random rand, int xMax, int yMax, int zMax)
    {
        int x = rand.Next(xMax);
        int y = rand.Next(yMax);
        int z = rand.Next(zMax);

        return new Vector3i(x, y, z);
    }

    public static List<Vector3i> GetPointsInside(Vector3i p1, Vector3i p2)
    {
        var result = new List<Vector3i>();

        for (int x = p1.x; x < p2.x; x++)
        {
            for (int y = p1.y; y < p2.y; y++)
            {
                for (int z = p1.z; z < p2.z; z++)
                {
                    result.Add(new Vector3i(x, y, z));
                }
            }
        }

        return result;
    }
}


public class CaveNoise
{
    public FastNoiseLite noise;

    public bool invert;

    public float threshold;

    public static CaveNoise defaultNoise = new CaveNoise(
        seed: CaveBuilder.SEED,
        octaves: 1,
        frequency: 0.15f,
        threshold: 0.4f,
        invert: true,
        noiseType: FastNoiseLite.NoiseType.Perlin,
        fractalType: FastNoiseLite.FractalType.None
    );

    public CaveNoise(int seed, int octaves, float frequency, float threshold, bool invert, FastNoiseLite.NoiseType noiseType, FastNoiseLite.FractalType fractalType)
    {
        this.invert = invert;
        this.threshold = threshold;

        noise = new FastNoiseLite(seed != -1 ? seed : CaveBuilder.SEED);
        noise.SetFractalType(fractalType);
        noise.SetNoiseType(noiseType);
        noise.SetFractalOctaves(octaves);
        noise.SetFrequency(frequency);
    }

    public void SetSeed(int seed)
    {
        noise.SetSeed(seed);
    }

    public bool IsTerrain(int x, int y, int z)
    {
        if (invert)
            return GetNormalizedNoise(x, y, z) > threshold;

        return GetNormalizedNoise(x, y, z) < threshold;
    }

    public bool IsTerrain(int x, int z)
    {
        if (invert)
            return GetNormalizedNoise(x, z) > threshold;

        return GetNormalizedNoise(x, z) < threshold;
    }

    public bool IsCave(int x, int y, int z)
    {
        return !IsTerrain(x, y, z);
    }

    public bool IsCave(int x, int z)
    {
        return !IsTerrain(x, z);
    }

    public float GetNormalizedNoise(int x, int y, int z)
    {
        return 0.5f * (1 + noise.GetNoise(x, y, z));
    }

    public float GetNormalizedNoise(int x, int z)
    {
        return 0.5f * (1 + noise.GetNoise(x, z));
    }
}


public class CavePrefab
{
    public static FastTags<TagGroup.Poi> tagCaveNode = FastTags<TagGroup.Poi>.Parse("cavenode");

    public static FastTags<TagGroup.Poi> tagCaveEntrance = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> tagCave = FastTags<TagGroup.Poi>.Parse("cave");

    public PrefabDataInstance prefabDataInstance;

    public Vector3i position;

    private Vector3i _size;

    public Vector3i size
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

    public string Name => prefabDataInstance.prefab.Name;

    public List<GraphNode> nodes;

    public List<Prefab.Marker> markers;

    public CavePrefab(int index)
    {
        id = index;
        nodes = new List<GraphNode>();
    }

    public CavePrefab(int index, Random rand)
    {
        id = index;
        nodes = new List<GraphNode>();

        size = new Vector3i(
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE)
        );
    }

    private Prefab.Marker RandomMarker(Random rand, int rotation, int xMax, int yMax, int zMax)
    {
        var markerType = Prefab.Marker.MarkerTypes.None;
        var tags = FastTags<TagGroup.Poi>.none;
        var groupName = "";

        int sizeX = rand.Next(1, xMax);
        int sizeY = rand.Next(1, yMax);
        int sizeZ = rand.Next(1, zMax);

        int px = rand.Next(size.x - sizeX);
        int py = rand.Next(size.y - sizeY);
        int pz = rand.Next(size.z - sizeZ);

        switch (rotation)
        {
            case 0:
                pz = -1;
                break;

            case 1:
                pz = size.z;
                break;

            case 2:
                px = -1;
                break;

            case 3:
                px = size.x;
                break;
        }

        var markerPos = position + new Vector3i(px, py, pz);
        var markerSize = new Vector3i(sizeX, sizeY, sizeZ);

        return new Prefab.Marker(markerPos, markerSize, markerType, groupName, tags);
    }

    public CavePrefab(int index, PrefabDataInstance pdi, Vector3i offset)
    {
        id = index;
        prefabDataInstance = pdi;
        position = pdi.boundingBoxPosition + offset;
        size = pdi.boundingBoxSize;
        rotation = pdi.rotation;

        CaveUtils.Assert(position.x > 0, $"offset: {offset}");
        CaveUtils.Assert(position.y > 0, $"offset: {offset}");
        CaveUtils.Assert(position.z > 0, $"offset: {offset}");

        UpdateNodes(pdi);
    }

    public void UpdateNodes(PrefabDataInstance prefab)
    {
        nodes = new List<GraphNode>();

        CaveUtils.Assert(prefab.prefab.POIMarkers.Count > 0);

        foreach (var marker in prefab.prefab.POIMarkers)
        {
            if (!marker.tags.Test_AnySet(tagCaveNode))
                continue;

            nodes.Add(new GraphNode(marker, this));
        }
    }

    public void UpdateNodes(Random rand)
    {
        markers = new List<Prefab.Marker>(){
            RandomMarker(rand, 0, size.x - 2, size.y, 1),
            RandomMarker(rand, 1, size.x - 2, size.y, 1),
            RandomMarker(rand, 2, 1, size.y, size.z - 2),
            RandomMarker(rand, 3, 1, size.y, size.z - 2),
        };

        nodes = new List<GraphNode>();

        foreach (var marker in markers)
        {
            nodes.Add(new GraphNode(marker, this));
        }
    }

    public List<List<Vector3i>> GetMarkerPoints()
    {
        var result = new List<Vector3i>[markers.Count];

        for (int i = 0; i < markers.Count; i++)
        {
            var marker = markers[i];

            result[i] = CaveUtils.GetPointsInside(position + marker.start, position + marker.start + marker.size);
        }

        return result.ToList();
    }

    public void SetRandomPosition(Random rand, int mapSize)
    {
        int offset = CaveBuilder.radiationSize + CaveBuilder.radiationZoneMargin;

        position = new Vector3i(
            rand.Next(offset, mapSize - offset - size.x),
            rand.Next(CaveBuilder.bedRockMargin, 255 - size.y),
            rand.Next(offset, mapSize - offset - size.z)
        );

        UpdateNodes(rand);
    }

    public bool OverLaps2D(CavePrefab other)
    {
        int overlapMargin = CaveBuilder.overLapMargin;

        if (position.x + size.x + overlapMargin < other.position.x || other.position.x + other.size.x + overlapMargin < position.x)
            return false;

        if (position.z + size.z + overlapMargin < other.position.z || other.position.z + other.size.z + overlapMargin < position.z)
            return false;

        return true;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersect2D(Vector3i point)
    {
        if (point.x < position.x)
            return false;

        if (point.x >= position.x + size.x)
            return false;

        if (point.z < position.z)
            return false;

        if (point.z >= position.z + size.z)
            return false;

        return true;
    }

    public int CountIntersections(Segment segment)
    {
        int intersectionsCount = 0;

        int x0 = position.x;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int z1 = z0 + size.z;

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

    public List<Vector3i> Get2DEdges()
    {
        List<Vector3i> points = new List<Vector3i>();

        int x0 = position.x;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int z1 = z0 + size.z;

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z0));
        }

        for (int x = x0; x <= x1; x++)
        {
            points.Add(new Vector3i(x, position.y, z1));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x0, position.y, z));
        }

        for (int z = z0; z <= z1; z++)
        {
            points.Add(new Vector3i(x1, position.y, z));
        }

        return points;
    }

    private List<Vector3i> GetBoundingPoints()
    {
        var points = new HashSet<Vector3i>();

        int x0 = position.x;
        int y0 = position.y;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int y1 = y0 + size.y;
        int z1 = z0 + size.z;

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

    public bool Intersect3D(Vector3i pos)
    {
        if (!Intersect2D(pos))
            return false;

        if (pos.y < position.y)
            return false;

        if (pos.y >= position.y + size.z)
            return false;

        return true;
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
            position.x + size.x / 2,
            position.y + size.y / 2,
            position.z + size.z / 2
        );
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

    public PrefabDataInstance ToPrefabDataInstance(int y)
    {
        position.y = y;
        prefabDataInstance.boundingBoxPosition = position;
        prefabDataInstance.rotation = rotation;

        return prefabDataInstance;
    }

    public List<Vector2s> GetOverlappingChunks()
    {
        var chunkPositions = new List<Vector2s>();

        var x0chunk = position.x / 16;
        var z0chunk = position.z / 16;
        var x1Chunk = (position.x + size.x - 1) / 16;
        var z1Chunk = (position.z + size.z - 1) / 16;

        for (int x = x0chunk; x <= x1Chunk; x++)
        {
            for (int z = z0chunk; z <= z1Chunk; z++)
            {
                chunkPositions.Add(new Vector2s(x, z));
            }
        }

        return chunkPositions;
    }
}


public class Direction
{
    public static Direction North = new Direction(-1, 0);

    public static Direction South = new Direction(1, 0);

    public static Direction East = new Direction(0, 1);

    public static Direction West = new Direction(0, -1);

    public static Direction None = new Direction(0, 0);

    public Vector3i Vector { get; internal set; }

    public Direction(int x, int z)
    {
        Vector = new Vector3i(x, 0, z);
    }

    public override bool Equals(object obj)
    {
        Direction other = (Direction)obj;
        return Vector.GetHashCode() == other.GetHashCode();
    }

    public override int GetHashCode()
    {
        return Vector.GetHashCode();
    }

    public static bool operator ==(Direction dir1, Direction dir2)
    {
        return dir1.Vector == dir2.Vector;
    }

    public static bool operator !=(Direction dir1, Direction dir2)
    {
        return dir1.Vector != dir2.Vector;
    }
}


public class GraphNode
{
    public Vector3i position;

    public CavePrefab prefab;

    public Direction direction;

    public Prefab.Marker marker;

    public int PrefabID => prefab.id;

    public GraphNode(Prefab.Marker marker, CavePrefab prefab)
    {
        this.marker = marker;
        this.prefab = prefab;
        position = prefab.position + marker.start + marker.size / 2;
        direction = GetDirection();

        CaveUtils.Assert(direction != Direction.None, $"None direction: {prefab.Name}, marker: [{marker.start}]");
    }

    public GraphNode(Vector3i position, CavePrefab prefab)
    {
        this.prefab = prefab;
        this.position = position;
        direction = Direction.None;
    }

    private Direction GetDirection()
    {
        if (marker.start.x == -1)
            return Direction.North;

        if (marker.start.x == marker.size.x)
            return Direction.South;

        if (marker.start.z == -1)
            return Direction.West;

        if (marker.start.z == marker.size.z)
            return Direction.East;

        return Direction.None;
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

    public Vector3i Normal(int distance)
    {
        CaveUtils.Assert(direction != Direction.None);

        return position + direction.Vector * distance;
    }

    public override string ToString()
    {
        return position.ToString();
    }

    public List<Vector3i> GetMarkerPoints()
    {
        return CaveUtils.GetPointsInside(marker.start, marker.start + marker.size);
    }

    public HashSet<Vector3i> GetSphere()
    {
        var center = position;
        var radius = 2 * CaveUtils.FastMax(marker.size.x, marker.size.z, marker.size.y);

        var queue = new HashSet<Vector3i>() { center };
        var visited = new HashSet<Vector3i>();
        var sphere = new HashSet<Vector3i>();
        var markerEnd = prefab.position + marker.start + marker.size;

        CaveUtils.Assert(!prefab.Intersect3D(center));

        while (queue.Count > 0)
        {
            foreach (var pos in queue.ToArray())
            {
                queue.Remove(pos);

                if (visited.Contains(pos))
                    continue;

                visited.Add(pos);

                if (prefab.Intersect3D(pos))
                    continue;

                if (pos.y >= markerEnd.y || pos.y < marker.start.y)
                    continue;

                if (direction.Vector.x == 0 && (pos.x < marker.start.x || pos.x >= markerEnd.x))
                    continue;

                if (direction.Vector.z == 0 && (pos.z < marker.start.z || pos.z >= markerEnd.z))
                    continue;

                if (CaveUtils.SqrEuclidianDist(pos, center) >= radius)
                    continue;

                queue.UnionWith(CaveUtils.GetValidNeighbors(pos));
                sphere.Add(pos);
            }
        }

        Log.Out($"Create node sphere at {prefab.Name}, center={center}, radius={radius}, points={sphere.Count}");

        return sphere;
    }

}


public class Edge : IComparable<Edge>
{
    public float Weight;

    public GraphNode node1;

    public GraphNode node2;

    public CavePrefab Prefab1 => node1.prefab;

    public CavePrefab Prefab2 => node2.prefab;

    public string HashPrefabs()
    {
        int index1 = CaveUtils.FastMin(Prefab1.id, Prefab2.id);
        int index2 = CaveUtils.FastMax(Prefab1.id, Prefab2.id);

        return $"{index1};{index2}";
    }

    private float GetWeight()
    {
        // return CaveUtils.SqrEuclidianDist2D(node1, node2) / CaveUtils.FastAbs(node1.y - node2.y);
        return CaveUtils.SqrEuclidianDist(node1.position, node2.position);
    }

    public int GetOrientationWeight()
    {
        Vector3i p1 = node1.position;
        Vector3i p2 = node2.position;

        var segment = new Segment(p1, p2);

        int result = Prefab1.CountIntersections(segment) + Prefab2.CountIntersections(segment);

        return result + 1;
    }

    public Edge(GraphNode node1, GraphNode node2)
    {
        this.node1 = node1;
        this.node2 = node2;
        Weight = GetWeight();
    }

    public int CompareTo(Edge other)
    {
        return Weight.CompareTo(other.Weight);
    }

    public override int GetHashCode()
    {
        int hash1 = node1.GetHashCode();
        int hash2 = node2.GetHashCode();

        int hash = 17;
        hash = hash * 23 + hash1 + hash2;
        hash = hash * 23 + hash1 + hash2;

        return hash;
    }
}


public class Node
{
    public Vector3i position;

    public float GCost { get; set; } // coût du chemin depuis le noeud de départ jusqu'à ce noeud

    public float HCost { get; set; } // estimation heuristique du coût restant jusqu'à l'objectif

    public float FCost => GCost + HCost; // coût total estimé (GCost + HCost)

    public Node Parent { get; set; } // noeud parent dans le chemin optimal

    public Node(Vector3i pos)
    {
        position = pos;
    }

    public Node(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
    }

    public List<Node> GetNeighbors()
    {
        var neighbors = new List<Node>();

        foreach (var pos in CaveUtils.GetValidNeighbors(position))
        {
            neighbors.Add(new Node(pos));
        }

        return neighbors;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        Node other = (Node)obj;
        return position.GetHashCode() == other.position.GetHashCode();
    }
}


public class HashedPriorityQueue<T>
{
    // see https://github.com/FyiurAmron/PriorityQueue
    private readonly SortedDictionary<float, Queue<T>> _sortedDictionary;

    private HashSet<T> _items;

    private int _count;

    public HashedPriorityQueue()
    {
        _items = new HashSet<T>();
        _sortedDictionary = new SortedDictionary<float, Queue<T>>();
        _count = 0;
    }

    public void Enqueue(T item, float priority)
    {
        if (!_sortedDictionary.TryGetValue(priority, out Queue<T> queue))
        {
            queue = new Queue<T>();
            _sortedDictionary.Add(priority, queue);
        }
        _items.Add(item);
        queue.Enqueue(item);
        _count++;
    }

    public T Dequeue()
    {
        if (_count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        var firstPair = _sortedDictionary.First();
        var queue = firstPair.Value;
        var item = queue.Dequeue();

        if (queue.Count == 0)
        {
            _sortedDictionary.Remove(firstPair.Key);
        }
        _count--;

        _items.Remove(item);

        return item;
    }

    public bool Contains(T element)
    {
        return _items.Contains(element);
    }

    public int Count => _count;
}


public class PrefabCache
{
    public Dictionary<Vector2s, List<CavePrefab>> groupedPrefabs;

    public List<CavePrefab> Prefabs;

    public int Count => Prefabs.Count;

    public PrefabCache()
    {
        Prefabs = new List<CavePrefab>();
        groupedPrefabs = new Dictionary<Vector2s, List<CavePrefab>>();
    }

    public void AddPrefab(CavePrefab prefab)
    {
        Prefabs.Add(prefab);

        var chunkPositions = prefab.GetOverlappingChunks();

        foreach (var chunkPos in chunkPositions)
        {
            if (!groupedPrefabs.ContainsKey(chunkPos))
                groupedPrefabs[chunkPos] = new List<CavePrefab>();

            groupedPrefabs[chunkPos].Add(prefab);
        }
    }

    public static IEnumerable<Vector2s> BrowseNeighborsChunks(int chunkX, int chunkZ, bool includeGiven = false)
    {
        if (includeGiven)
            yield return new Vector2s(chunkX, chunkZ);

        yield return new Vector2s(chunkX + 1, chunkZ);
        yield return new Vector2s(chunkX - 1, chunkZ);
        yield return new Vector2s(chunkX, chunkZ + 1);
        yield return new Vector2s(chunkX, chunkZ - 1);
        yield return new Vector2s(chunkX + 1, chunkZ - 1);
        yield return new Vector2s(chunkX - 1, chunkZ - 1);
        yield return new Vector2s(chunkX + 1, chunkZ + 1);
        yield return new Vector2s(chunkX - 1, chunkZ + 1);
    }

    public HashSet<CavePrefab> GetNearestPrefabs(Vector3i position)
    {
        var nearestPrefabs = new HashSet<CavePrefab>();

        int chunkX = position.x / 16;
        int chunkZ = position.z / 16;

        foreach (var chunkPos in BrowseNeighborsChunks(chunkX, chunkZ, includeGiven: true))
        {
            if (!groupedPrefabs.TryGetValue(chunkPos, out var chunkPrefabs))
                continue;

            foreach (var prefab in chunkPrefabs)
            {
                nearestPrefabs.Add(prefab);
            }
        }

        return nearestPrefabs;
    }

    public float MinDistToPrefab(Vector3i position)
    {
        float minDist = int.MaxValue;
        var prefabs = GetNearestPrefabs(position);

        foreach (var prefab in prefabs)
        {
            if (prefab.Intersect2D(position))
            {
                return 0f;
            }

            float dist = CaveUtils.SqrEuclidianDist(position, prefab.GetCenter()) - prefab.BoundingRadiusSqr;

            if (dist < minDist)
            {
                minDist = dist;
            }
        }

        return minDist == 0 ? -1 : minDist;
    }
}


public static class CaveTunneler
{
    private static ConcurrentDictionary<Vector3i, bool> validPositions = new ConcurrentDictionary<Vector3i, bool>();

    private static List<Vector3i> ReconstructPath(Node currentNode)
    {
        var path = new List<Vector3i>();

        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.Parent;
        }

        return path;
    }

    public static List<Vector3i> FindPath(GraphNode start, GraphNode target, PrefabCache cachedPrefabs)
    {
        var startNode = new Node(start.Normal(5));
        var goalNode = new Node(target.Normal(5));

        var queue = new HashedPriorityQueue<Node>();
        var visited = new HashSet<Node>();
        var index = 0;

        queue.Enqueue(startNode, float.MaxValue);

        while (queue.Count > 0 && index++ < 100_000)
        {
            Node currentNode = queue.Dequeue();

            visited.Add(currentNode);

            foreach (Node neighbor in currentNode.GetNeighbors())
            {
                CaveUtils.Assert(neighbor.position.y >= CaveBuilder.bedRockMargin);

                if (neighbor.position == goalNode.position)
                {
                    return ReconstructPath(currentNode);
                }
                if (visited.Contains(neighbor))
                    continue;

                float minDist = cachedPrefabs.MinDistToPrefab(neighbor.position);

                if (minDist == 0)
                    continue;

                bool isCave = CaveBuilder.pathingNoise.IsCave(neighbor.position.x, neighbor.position.y, neighbor.position.z);
                float factor = 1.0f;

                factor *= isCave ? 0.5f : 1f;
                factor *= minDist < 20 ? 1 : .5f;

                float tentativeGCost = currentNode.GCost + CaveUtils.SqrEuclidianDist(currentNode, neighbor) * factor;

                bool isInQueue = queue.Contains(neighbor);

                if (!isInQueue || tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CaveUtils.SqrEuclidianDist(neighbor, goalNode) * factor;

                    if (!isInQueue)
                        queue.Enqueue(neighbor, neighbor.FCost);
                }
            }
        }

        Log.Warning($"No Path found from {start} to {target} after {index} iterations");

        return new List<Vector3i>();
    }

    public static HashSet<Vector3i> GetSphere(Vector3i center, float radius)
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

                if (pos.y <= CaveBuilder.bedRockMargin)
                    continue;

                if (pos.y + CaveBuilder.terrainMargin >= WorldBuilder.Instance.GetHeight(pos.x, pos.z))
                    continue;

                visited.Add(pos);

                if (CaveUtils.SqrEuclidianDist(pos, center) >= radius)
                    continue;

                queue.UnionWith(CaveUtils.GetValidNeighbors(pos));
            }
        }

        return visited;
    }

    public static HashSet<Vector3i> ThickenTunnel(List<Vector3i> path, GraphNode start, GraphNode target)
    {
        var caveMap = new HashSet<Vector3i>();

        caveMap.UnionWith(start.GetSphere());
        caveMap.UnionWith(target.GetSphere());

        foreach (var position in path)
        {
            var circle = GetSphere(position, 6f);
            caveMap.UnionWith(circle);
        }

        return caveMap;
    }

}


public class EdgeWeightComparer : IComparer<Edge>
{
    private readonly Graph _graph;

    public EdgeWeightComparer(Graph graph)
    {
        _graph = graph;
    }

    public int Compare(Edge x, Edge y)
    {
        if (x == null || y == null)
        {
            throw new ArgumentException("Comparing null objects is not supported.");
        }

        return _graph.GetEdgeWeight(x).CompareTo(_graph.GetEdgeWeight(y));
    }
}


public class Graph
{
    public List<Edge> Edges { get; set; }

    public HashSet<GraphNode> Nodes { get; set; }

    public Dictionary<string, int> prefabsConnections;

    public Graph()
    {
        Edges = new List<Edge>();
        Nodes = new HashSet<GraphNode>();
        prefabsConnections = new Dictionary<string, int>();
    }

    public int GetEdgeWeight(Edge edge)
    {
        prefabsConnections.TryGetValue(edge.HashPrefabs(), out int occurences);

        const float distCoef = .5f;
        const float occurencesCoef = 1f;
        const float IntersectionCoef = 5f;

        double weight = Math.Pow(edge.Weight, distCoef);

        weight *= Math.Pow(occurences + 1, occurencesCoef);
        weight *= Math.Pow(edge.GetOrientationWeight(), IntersectionCoef);

        return (int)weight;
    }

    public void AddPrefabConnection(Edge edge)
    {
        string hash = edge.HashPrefabs();

        if (!prefabsConnections.ContainsKey(hash))
            prefabsConnections[hash] = 0;

        prefabsConnections[hash] += 1;

        // Log.Out($"{hash}: {prefabsConnections[hash]}");
    }

    public void AddEdge(Edge edge)
    {
        Edges.Add(edge);
        Nodes.Add(edge.node1);
        Nodes.Add(edge.node2);
    }

    public List<Edge> GetEdgesFromNode(GraphNode node)
    {
        return Edges.Where(e => e.node1.Equals(node) || e.node2.Equals(node)).ToList();
    }

    private static Graph BuildPrimaryGraph(List<CavePrefab> prefabs)
    {
        var prefabEdges = new Dictionary<int, List<Edge>>();
        var graph = new Graph();

        for (int i = 0; i < prefabs.Count; i++)
        {
            for (int j = i + 1; j < prefabs.Count; j++)
            {
                var prefab1 = prefabs[i];
                var prefab2 = prefabs[j];

                var prefabCenter1 = new GraphNode(prefab1.GetCenter(), prefab1);
                var prefabCenter2 = new GraphNode(prefab2.GetCenter(), prefab2);
                var edge = new Edge(prefabCenter1, prefabCenter2);

                if (!prefabEdges.ContainsKey(prefab1.id))
                    prefabEdges.Add(prefab1.id, new List<Edge>());

                if (!prefabEdges.ContainsKey(prefab2.id))
                    prefabEdges.Add(prefab2.id, new List<Edge>());

                prefabEdges[prefab1.id].Add(edge);
                prefabEdges[prefab2.id].Add(edge);
            }
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            var relatedPrefabEdges = prefabEdges[prefab.id];
            var nodes = prefabs[i].nodes.ToHashSet();

            if (nodes.Count == 0)
            {
                Log.Error($"[Cave] no cave node for {prefabs[i].Name}");
                continue;
            }

            CaveUtils.Assert(relatedPrefabEdges.Count == prefabs.Count - 1);

            relatedPrefabEdges.Sort();

            for (int j = 0; j < nodes.Count; j++)
            {
                var relatedEdge = relatedPrefabEdges[j];

                var nodes1 = relatedEdge.Prefab1.nodes;
                var nodes2 = relatedEdge.Prefab2.nodes;

                foreach (var node1 in nodes1)
                {
                    foreach (var node2 in nodes2)
                    {
                        var graphNode1 = node1; //new GraphNode(node1, relatedEdge.Prefab1);
                        var graphNode2 = node2; //new GraphNode(node2, relatedEdge.Prefab2);

                        graph.AddEdge(new Edge(graphNode1, graphNode2));

                        // Log.Out($"Create edge between {graphNode1} and {graphNode2}");
                    }
                }
            }
        }

        return graph;
    }

    public List<Edge> FindMST()
    {
        var graph = new List<Edge>();
        var nodes = new HashSet<GraphNode>();

        foreach (var node in Nodes)
        {
            if (nodes.Contains(node))
                continue;

            var relatedEdges = GetEdgesFromNode(node);

            // CaveUtils.Assert(node.direction != Direction.None);

            relatedEdges.Sort(new EdgeWeightComparer(this));

            graph.Add(relatedEdges[0]);

            nodes.Add(relatedEdges[0].node1);
            nodes.Add(relatedEdges[0].node1);

            AddPrefabConnection(relatedEdges[0]);

            // Log.Out(prefabsConnections[relatedEdges[0].HashPrefabs()].ToString());
        }

        return graph;
    }

    public static List<Edge> Resolve(List<CavePrefab> prefabs)
    {
        var timer = new Stopwatch();

        timer.Start();

        // List<Edge> graph = BuildPrefabsGraph(prefabs);
        Graph graph = BuildPrimaryGraph(prefabs);

        // Log.Out($"[Cave] primary graph: edges={graph.Edges.Count}, nodes={graph.Nodes.Count}");

        var edges = graph.FindMST();

        Log.Out($"Graph resolved in {CaveUtils.TimeFormat(timer)}, edges={edges.Count}");

        return edges; // graph.Edges;
    }

}


public static class CaveBuilder
{
    public static int SEED = 1634735684; // new Random().Next();

    public static int worldSize = 2048;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;

    public static float POINT_WIDTH = 5;

    public static int PREFAB_COUNT => worldSize / 5;

    public static Random rand = new Random(SEED);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 20;

    public static int radiationSize = StreetTile.TileSize;

    public static int bedRockMargin = 2;

    public static int terrainMargin = 5;

    public static CaveNoise pathingNoise = CaveNoise.defaultNoise;

    public static FastNoiseLite ParsePerlinNoise(int seed = -1)
    {
        var noise = new FastNoiseLite(seed == -1 ? SEED : seed);

        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFractalType(FastNoiseLite.FractalType.None);
        noise.SetFractalGain(1);
        noise.SetFractalOctaves(1);
        noise.SetFrequency(0.1f);

        return noise;
    }

    public static bool TryPlacePrefab(ref CavePrefab prefab, List<CavePrefab> others)
    {
        int maxTries = 10;

        while (maxTries-- > 0)
        {
            prefab.SetRandomPosition(rand, worldSize);

            if (!prefab.OverLaps2D(others))
            {
                return true;
            }
        }

        return false;
    }

    public static PrefabCache GetRandomPrefabs(int count)
    {
        Log.Out("Start POIs placement...");

        var prefabCache = new PrefabCache();

        for (int i = 0; i < count; i++)
        {
            var prefab = new CavePrefab(prefabCache.Count + 1, rand);

            if (TryPlacePrefab(ref prefab, prefabCache.Prefabs))
                prefabCache.AddPrefab(prefab);
        }

        Log.Out($"{prefabCache.Count} / {PREFAB_COUNT} prefabs added");

        return prefabCache;
    }

    public static void SaveCaveMap(string filename, HashSet<Vector3i> caveMap)
    {
        SortedDictionary<Vector3i, List<string>> groupedCaveMap = GroupByChunk(caveMap);

        using (var writer = new StreamWriter(filename))
        {
            writer.WriteLine(groupedCaveMap.Count);

            foreach (var entry in groupedCaveMap)
            {
                if (entry.Value.Count == 0)
                    continue;

                writer.WriteLine($"{entry.Key.x}, {entry.Key.z}");
                writer.WriteLine(entry.Value.Count);

                foreach (var position in entry.Value)
                {
                    writer.WriteLine(position);
                }
            }
        }
    }

    public static Dictionary<Vector2s, Vector3bf[]> ReadCaveMap(string filename)
    {
        var caveMap = new Dictionary<Vector2s, Vector3bf[]>();

        if (!File.Exists(filename))
        {
            Log.Warning($"[Cave] CaveBuilder.ReadCaveMap: File not found '{filename}'");
            return caveMap;
        }

        using (var reader = new StreamReader(filename))
        {
            int chunkCount = int.Parse(reader.ReadLine());

            for (int i = 0; i < chunkCount; i++)
            {
                var chunkPos = new Vector2s(reader.ReadLine());
                var blockCount = int.Parse(reader.ReadLine());

                caveMap[chunkPos] = new Vector3bf[blockCount];

                for (int j = 0; j < blockCount; j++)
                {
                    caveMap[chunkPos][j] = new Vector3bf(reader.ReadLine());
                }
            }
        }

        return caveMap;
    }

    public static Dictionary<Vector3i, Vector3i[]> ReadCaveMap3i(string filename)
    {
        var caveMap = new Dictionary<Vector3i, Vector3i[]>();

        using (var reader = new StreamReader(filename))
        {
            int chunkCount = int.Parse(reader.ReadLine());

            for (int i = 0; i < chunkCount; i++)
            {
                var chunkPos2s = new Vector2s(reader.ReadLine());
                var chunkPos = new Vector3i(chunkPos2s.x, 0, chunkPos2s.z);
                var blockCount = int.Parse(reader.ReadLine());

                caveMap[chunkPos] = new Vector3i[blockCount];

                for (int j = 0; j < blockCount; j++)
                {
                    var array = reader.ReadLine().Split(',');
                    caveMap[chunkPos][j] = new Vector3i(
                        int.Parse(array[0]),
                        int.Parse(array[1]),
                        int.Parse(array[2])
                    );
                }
            }
        }

        return caveMap;
    }

    public static Vector3i GetChunkPosZX(Vector3i pos)
    {
        return new Vector3i(
            (pos.x / 16) - worldSize / 32,
            0,
            (pos.z / 16) - worldSize / 32
        );
    }

    public static SortedDictionary<Vector3i, List<string>> GroupByChunk(HashSet<Vector3i> caveMap)
    {
        var groupedCaveMap = new SortedDictionary<Vector3i, List<string>>(new VectorComparer());

        foreach (var pos in caveMap)
        {
            Vector3i chunkPos = GetChunkPosZX(pos);

            if (!groupedCaveMap.ContainsKey(chunkPos))
                groupedCaveMap[chunkPos] = new List<string>();

            var transform = new Vector3i(
                16 * (pos.x / 16),
                0,
                16 * (pos.z / 16)
            );

            var chunkRelativePos = pos - transform;

            // groupedCaveMap[chunkPos].Add($"{pos} - {transform} = {relative_pos}");
            groupedCaveMap[chunkPos].Add(chunkRelativePos.ToString());

            if (chunkRelativePos.x < 0 || chunkRelativePos.x > 15)
                throw new Exception($"ChunkPos.x out of bound: {pos} - {transform} = {chunkRelativePos}");

            if (chunkRelativePos.y < 0 || chunkRelativePos.y > 255)
                throw new Exception($"ChunkPos.y out of bound: {pos} - {transform} = {chunkRelativePos}");

            if (chunkRelativePos.z < 0 || chunkRelativePos.z > 15)
                throw new Exception($"ChunkPos.z out of bound: {pos} - {transform} = {chunkRelativePos}");
        }

        return groupedCaveMap;
    }

}


public class VectorComparer : IComparer<Vector3i>
{
    public int Compare(Vector3i p1, Vector3i p2)
    {
        if (p2.x != p1.x)
            return p1.x - p2.x;

        return p1.z - p2.z;
    }
}

