#pragma warning disable CS0162, CA1416, CA1050, CA2211, IDE0090, IDE0044, IDE0028, IDE0305, IDE0035


using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using WorldGenerationEngineFinal;

using Random = System.Random;
using Debug = System.Diagnostics.Debug;
using System.IO;


public static class CaveUtils
{
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

    public static float EuclidianDist(Vector3i p1, Vector3i p2)
    {
        return (float)Math.Sqrt(SqrEuclidianDist(p1, p2));
    }

    public static List<Vector3i> GetValidNeighbors(Vector3i position)
    {
        var neighbors = new List<Vector3i>();

        if (position.x - CaveBuilder.radiationZoneMargin > 1)
            neighbors.Add(new Vector3i(position.x - 1, position.y, position.z));

        if (position.x + CaveBuilder.radiationZoneMargin < CaveBuilder.worldSize)
            neighbors.Add(new Vector3i(position.x + 1, position.y, position.z));

        if (position.z - CaveBuilder.radiationZoneMargin > 1)
            neighbors.Add(new Vector3i(position.x, position.y, position.z - 1));

        if (position.z + CaveBuilder.radiationZoneMargin < CaveBuilder.worldSize)
            neighbors.Add(new Vector3i(position.x, position.y, position.z + 1));

        if (position.y - CaveBuilder.cavePrefabBedRockMargin > 1)
            neighbors.Add(new Vector3i(position.x, position.y - 1, position.z));

        if (position.y + CaveBuilder.cavePrefabterrainMargin < WorldBuilder.Instance.GetHeight(position.x, position.z))
            neighbors.Add(new Vector3i(position.x, position.y + 1, position.z));

        return neighbors;
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
        frequency: 0.1f,
        threshold: 0.5f,
        invert: true,
        noiseType: FastNoiseLite.NoiseType.Perlin,
        fractalType: FastNoiseLite.FractalType.None
    );

    // {    new FastNoiseLite(seed == -1 ? CaveBuilder.seed : seed);

    //     noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
    //         noise.SetFractalType(FastNoiseLite.FractalType.None);
    //         noise.SetFractalGain(1);
    //         noise.SetFractalOctaves(1);
    //         noise.SetFrequency(0.1f);
    //         }

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
    public static FastTags<TagGroup.Poi> caveNodeTags = FastTags<TagGroup.Poi>.Parse("cavenode");

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

    public byte rotation;

    public int BoundingRadiusSqr { get; internal set; }

    public string Name => prefabDataInstance.prefab.Name;

    public List<Vector3i> nodes;

    public CavePrefab()
    {
        nodes = new List<Vector3i>();
    }

    public CavePrefab(Vector3i position)
    {
        nodes = new List<Vector3i>();
        this.position = new Vector3i(position);
    }

    public CavePrefab(Random rand)
    {
        nodes = new List<Vector3i>();
        size = new Vector3i(
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE),
            rand.Next(CaveBuilder.MIN_PREFAB_SIZE, CaveBuilder.MAX_PREFAB_SIZE)
        );
    }

    public CavePrefab(PrefabDataInstance pdi, Vector3i offset)
    {
        prefabDataInstance = pdi;
        position = pdi.boundingBoxPosition + offset;
        size = pdi.boundingBoxSize;
        rotation = pdi.rotation;

        UpdateNodes(pdi);
    }

    public void UpdateNodes(PrefabDataInstance prefab)
    {
        nodes = new List<Vector3i>();

        foreach (var marker in prefab.prefab.POIMarkers)
        {
            if (!marker.tags.Test_AnySet(caveNodeTags))
                continue;

            nodes.Add(marker.start + position);
        }
    }

    public CavePrefab(int id, PrefabData prefabData)
    {
        position = new Vector3i();
        rotation = 0;
        prefabDataInstance = new PrefabDataInstance(id, position, rotation, prefabData);
    }

    public void UpdateNodes(Random rand = null)
    {
        if (rand == null)
        {
            nodes = new List<Vector3i>()
                {
                    position + new Vector3i(size.x / 2 , 0, 0),
                    position + new Vector3i(0, 0, size.z / 2),
                    position + new Vector3i(size.x / 2, 0, size.z),
                    position + new Vector3i(size.x , 0, size.z / 2),
                };
        }
        else
        {
            nodes = new List<Vector3i>()
                {
                    position + new Vector3i(rand.Next(size.x) , 0, 0),
                    position + new Vector3i(0, 0, rand.Next(size.z)),
                    position + new Vector3i(rand.Next(size.x), 0, size.z),
                    position + new Vector3i(size.x , 0, rand.Next(size.z)),
                };
        }
    }

    public List<Vector3i> GetInnerPoints()
    {
        var innerPoints = new List<Vector3i>();

        for (int x = position.x; x <= (position.x + size.x); x++)
        {
            for (int y = position.y; y <= (position.y + size.z); y++)
            {
                for (int z = position.z; z <= (position.z + size.z); z++)
                {
                    innerPoints.Add(new Vector3i(x, y, z));
                }
            }
        }

        return innerPoints;
    }

    public void SetRandomPosition(Random rand, int mapSize, int mapOffset)
    {
        position = new Vector3i(
            rand.Next(mapOffset, mapSize - mapOffset - size.x),
            rand.Next(5, 200),
            rand.Next(mapOffset, mapSize - mapOffset - size.z)
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

    public List<Edge> GetFaces()
    {
        // a face is described his diagonal in one xyz plane.

        int x0 = position.x;
        int y0 = position.y;
        int z0 = position.z;

        int x1 = x0 + size.x;
        int y1 = y0 + size.z;
        int z1 = z0 + size.z;

        var p000 = new Vector3i(x0, y0, z0);
        var p001 = new Vector3i(x0, y0, z1);
        var p100 = new Vector3i(x1, y0, z0);
        var p101 = new Vector3i(x1, y0, z1);
        var p010 = new Vector3i(x0, y1, z0);
        var p011 = new Vector3i(x0, y1, z1);
        var p110 = new Vector3i(x1, y1, z0);
        var p111 = new Vector3i(x1, y1, z1);

        var faces = new List<Edge>(){
            new Edge(p000, p011),
            new Edge(p000, p101),
            new Edge(p000, p110),
            new Edge(p111, p100),
            new Edge(p111, p010),
            new Edge(p111, p001),
        };

        return faces;
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

    public HashSet<Vector3i> GetNoiseAround(Random rand)
    {
        int maxNoiseSize = 10;
        var perlinNoise = CaveBuilder.ParsePerlinNoise(rand.Next());
        var noiseMap = new HashSet<Vector3i>();

        foreach (Edge diagonal in GetFaces())
        {
            Vector3i p1 = diagonal.node1;
            Vector3i p2 = diagonal.node2;

            int normalDir = p1 == position ? -1 : 1;

            if (p1.x == p2.x)
            {
                int yMin = CaveUtils.FastMin(p1.y, p2.y);
                int yMax = CaveUtils.FastMax(p1.y, p2.y);

                for (int y = yMin; y < yMax; y++)
                {
                    int zMin = CaveUtils.FastMin(p1.z, p2.z);
                    int zMax = CaveUtils.FastMax(p1.z, p2.z);

                    for (int z = zMin; z < zMax; z++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(p1.x, y, z));

                        int xMin = CaveUtils.FastMin(p1.x, p1.x + (int)(maxNoiseSize * noise));
                        int xMax = CaveUtils.FastMax(p1.x, p1.x + (int)(maxNoiseSize * noise));

                        for (int x = xMin; x <= xMax; x++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
            else if (p1.y == p2.y)
            {
                int zMin = CaveUtils.FastMin(p1.z, p2.z);
                int zMax = CaveUtils.FastMax(p1.z, p2.z);

                for (int z = zMin; z < zMax; z++)
                {
                    int xMin = CaveUtils.FastMin(p1.x, p2.x);
                    int xMax = CaveUtils.FastMax(p1.x, p2.x);

                    for (int x = xMin; x < xMax; x++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(x, p1.y, z));

                        int yMin = CaveUtils.FastMin(p1.y, p1.y + (int)(maxNoiseSize * noise));
                        int yMax = CaveUtils.FastMax(p1.y, p1.y + (int)(maxNoiseSize * noise));

                        for (int y = yMin; y < yMax; y++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
            else if (p1.z == p2.z)
            {
                int xMin = CaveUtils.FastMin(p1.x, p2.x);
                int xMax = CaveUtils.FastMax(p1.x, p2.x);

                for (int x = xMin; x < xMax; x++)
                {
                    int yMin = CaveUtils.FastMin(p1.y, p2.y);
                    int yMax = CaveUtils.FastMax(p1.y, p2.y);

                    for (int y = yMin; y < yMax; y++)
                    {
                        float noise = 0.5f * normalDir * (1 + perlinNoise.GetNoise(x, y, p1.z));

                        int zMin = CaveUtils.FastMin(p1.z, p1.z + (int)(maxNoiseSize * noise));
                        int zMax = CaveUtils.FastMax(p1.z, p1.z + (int)(maxNoiseSize * noise));

                        for (int z = zMin; z < zMax; z++)
                        {
                            noiseMap.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
        }

        // noiseMap.ExceptWith(innerPoints);

        return noiseMap;
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


public class Edge : IComparable<Edge>
{
    public int nodeIndex1;

    public int nodeIndex2;

    public int prefabIndex1;

    public int prefabIndex2;

    public float Weight;

    public Vector3i node1;

    public Vector3i node2;

    public string HashPrefabs()
    {
        int index1 = CaveUtils.FastMin(prefabIndex1, prefabIndex2);
        int index2 = CaveUtils.FastMax(prefabIndex1, prefabIndex2);

        return $"{index1};{index2}";
    }

    private float GetWeight()
    {
        return CaveUtils.SqrEuclidianDist(node1, node2);
    }

    public Edge(int index1, int index2, int prefab1, int prefab2, Vector3i _startPoint, Vector3i _endPoint)
    {
        nodeIndex1 = index1;
        nodeIndex2 = index2;
        prefabIndex1 = prefab1;
        prefabIndex2 = prefab2;
        node1 = _startPoint;
        node2 = _endPoint;
        Weight = GetWeight();
    }

    public Edge(int index1, int index2, Vector3i _startPoint, Vector3i _endPoint)
    {
        prefabIndex1 = index1;
        prefabIndex2 = index2;
        node1 = _startPoint;
        node2 = _endPoint;
        Weight = GetWeight();
    }

    public Edge(Vector3i node1, Vector3i node2)
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

        // Log.Out($"{position} {prefabs.Count}");

        foreach (var prefab in prefabs)
        {
            if (prefab.Intersect3D(position))
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

    private static HashSet<Vector3i> ReconstructPath(Node currentNode)
    {
        var path = new HashSet<Vector3i>();

        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.Parent;
        }

        return path;
    }

    public static HashSet<Vector3i> FindPath(Vector3i startPos, Vector3i targetPos, PrefabCache cachedPrefabs)
    {
        var startNode = new Node(startPos);
        var goalNode = new Node(targetPos);

        var queue = new HashedPriorityQueue<Node>();
        var visited = new HashSet<Node>();

        queue.Enqueue(startNode, float.MaxValue);

        var path = new HashSet<Vector3i>();

        while (queue.Count > 0)
        {
            Node currentNode = queue.Dequeue();

            visited.Add(currentNode);

            foreach (Node neighbor in currentNode.GetNeighbors())
            {
                if (neighbor.position == goalNode.position)
                {
                    return ReconstructPath(currentNode);
                }
                if (visited.Contains(neighbor))
                    continue;

                // var prefabs = GetNeighborChunksPrefabs(position, prefabs);
                float minDist = cachedPrefabs.MinDistToPrefab(neighbor.position);

                // Log.Out(minDist.ToString());

                if (minDist == 0)
                    continue;

                // float noise = 0.5f * (1 + CaveBuilder.pathingNoise.GetNoise(neighbor.position.x, neighbor.position.y, neighbor.position.z));
                // float factor = noise < CaveBuilder.NOISE_THRESHOLD ? .5f : 1f;

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

        if (path.Count == 0)
        {
            Log.Warning($"No Path found from {startPos} to {targetPos}.");
        }

        return path;
    }

    public static HashSet<Vector3i> ThickenCaveMap(HashSet<Vector3i> wiredCaveMap)
    {
        var caveMap = new HashSet<Vector3i>();

        foreach (var position in wiredCaveMap)
        {
            var circle = CaveBuilder.ParseCircle(position, 2f);

            caveMap.UnionWith(circle);
        }

        return caveMap;
    }

}


public static class GraphSolver
{
    private static List<Edge> BuildPrefabGraph(List<CavePrefab> prefabs)
    {
        var prefabEdges = new Dictionary<int, List<Edge>>();
        var graph = new HashSet<Edge>();

        if (prefabs.Count < 2)
        {
            Log.Error("[Cave] At least two prefabs must be provided");
            return graph.ToList();
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            for (int j = i + 1; j < prefabs.Count; j++)
            {
                Vector3i p1 = prefabs[i].GetCenter();
                Vector3i p2 = prefabs[j].GetCenter();

                var edge = new Edge(i, j, p1, p2);

                if (!prefabEdges.ContainsKey(i))
                    prefabEdges.Add(i, new List<Edge>());

                if (!prefabEdges.ContainsKey(j))
                    prefabEdges.Add(j, new List<Edge>());

                prefabEdges[i].Add(edge);
                prefabEdges[j].Add(edge);
            }
        }

        Log.Out($"prefabs: {prefabs.Count}, prefabEdges: {prefabEdges.Count}");

        for (int i = 0; i < prefabs.Count; i++)
        {
            var relatedPrefabEdges = prefabEdges[i];
            var nodes = prefabs[i].nodes.ToHashSet();

            if (nodes.Count == 0)
            {
                Log.Error($"[Cave] no cave node for {prefabs[i].Name}");
                continue;
            }

            Debug.Assert(relatedPrefabEdges.Count == prefabs.Count - 1);

            relatedPrefabEdges.Sort();

            var connectedNodes = new HashSet<Vector3i>();

            for (int j = 0; j < relatedPrefabEdges.Count; j++)
            {
                if (nodes.Count == 0)
                    break;

                var relatedEdge = relatedPrefabEdges[j];
                var edges = new List<Edge>();

                var nodes1 = prefabs[relatedEdge.prefabIndex1].nodes;
                var nodes2 = prefabs[relatedEdge.prefabIndex2].nodes;

                foreach (var node1 in nodes1)
                {
                    foreach (var node2 in nodes2)
                    {
                        var edge = new Edge(node1, node2);

                        edges.Add(edge);
                    }
                }

                edges.Sort();

                var edgeNode1 = edges[0].node1;
                var edgeNode2 = edges[0].node2;

                if (connectedNodes.Contains(edgeNode1) || connectedNodes.Contains(edgeNode2))
                    continue;

                connectedNodes.Add(edgeNode1);
                connectedNodes.Add(edgeNode2);

                nodes.Remove(edgeNode1);
                nodes.Remove(edgeNode2);

                graph.Add(edges[0]);
                continue;
            }

            // break;
        }

        return graph.ToList();
    }

    public static List<Edge> Resolve(List<CavePrefab> prefabs)
    {
        var timer = new Stopwatch();

        timer.Start();

        List<Edge> graph = BuildPrefabGraph(prefabs);

        Log.Out($"Graph resolved in {CaveUtils.TimeFormat(timer)}");

        return graph;
    }

}


public static class CaveBuilder
{
    public static int SEED = new Random().Next();

    public static int worldSize = 1000;

    public static int MIN_PREFAB_SIZE = 8;

    public static int MAX_PREFAB_SIZE = 100;

    public static int MAP_OFFSET => worldSize / 60;

    public static float POINT_WIDTH = 5;

    public static int PREFAB_COUNT => worldSize / 5;

    public static Random rand = new Random(SEED);

    public static int overLapMargin = 50;

    public static int radiationZoneMargin = 10;

    public static int cavePrefabBedRockMargin = 2;

    public static int cavePrefabterrainMargin = 10;

    public static CaveNoise pathingNoise = CaveNoise.defaultNoise;

    public static HashSet<Vector3i> ParseCircle(Vector3i center, float radius)
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

                visited.Add(pos);

                if (CaveUtils.SqrEuclidianDist(pos, center) >= radius)
                    continue;

                queue.UnionWith(CaveUtils.GetValidNeighbors(pos));
            }
        }

        return visited;
    }

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
            prefab.SetRandomPosition(rand, worldSize, MAP_OFFSET);

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
            var prefab = new CavePrefab(rand);

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
                throw new Exception($"{pos} - {transform} = {chunkRelativePos}");

            if (chunkRelativePos.y < 0 || chunkRelativePos.y > 255)
                throw new Exception($"{pos} - {transform} = {chunkRelativePos}");

            if (chunkRelativePos.z < 0 || chunkRelativePos.z > 15)
                throw new Exception($"{pos} - {transform} = {chunkRelativePos}");
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

