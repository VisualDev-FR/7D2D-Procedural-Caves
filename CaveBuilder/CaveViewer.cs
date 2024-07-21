# pragma warning disable CA1416, CA1050, CA2211, IDE0290, IDE0063, IDE0305, IDE0090


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


public static class CaveViewer
{
    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.FromArgb(84, 84, 82);

    public static PointF ParsePointF(Vector3i point)
    {
        return new PointF(point.x, point.z);
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<CavePrefab> prefabs, bool fill = false)
    {
        using (var pen = new Pen(PrefabBoundsColor, 1))
        {
            foreach (var prefab in prefabs)
            {
                graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);

                DrawPoints(b, new HashSet<Vector3i>(prefab.nodes), NodeColor);
            }
        }
    }

    public static void DrawPoints(Bitmap bitmap, HashSet<Vector3i> points, Color color)
    {
        foreach (var point in points)
        {
            bitmap.SetPixel(point.x, point.z, color);
        }
    }

    public static void DrawEdges(Graphics graph, List<Edge> edges)
    {
        using (var pen = new Pen(TunnelsColor, 2))
        {
            foreach (var edge in edges)
            {
                graph.DrawCurve(pen, new PointF[2]{
                ParsePointF(edge.node1.position),
                ParsePointF(edge.node2.position),
            });
            }
        }
    }

    public static void DrawNoise(Bitmap b, CaveNoise noise)
    {
        for (int x = 0; x < CaveBuilder.worldSize; x++)
        {
            for (int z = 0; z < CaveBuilder.worldSize; z++)
            {
                if (noise.IsCave(x, z))
                    b.SetPixel(x, z, NoiseColor);
            }
        }
    }

    public static void GraphCommand(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : CaveBuilder.PREFAB_COUNT;

        var prefabs = CaveBuilder.GetRandomPrefabs(prefabCounts);

        Log.Out("Start solving MST Krustal...");

        List<Edge> edges = Graph.Resolve(prefabs.Prefabs);

        Log.Out("Start Drawing graph...");

        using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawEdges(g, edges);
                DrawPrefabs(b, g, prefabs.Prefabs);
            }

            Log.Out($"{edges.Count} Generated edges.");

            b.Save(@"graph.png", ImageFormat.Png);
        }
    }

    public static void NoiseCommand(string[] args)
    {
        CaveBuilder.SEED = 12345;
        CaveBuilder.worldSize = 100;

        var noise = new CaveNoise(
            seed: CaveBuilder.SEED,
            octaves: 2,
            frequency: 0.01f,
            threshold: 0.8f,
            invert: true,
            noiseType: FastNoiseLite.NoiseType.Cellular,
            fractalType: FastNoiseLite.FractalType.FBm
        );

        noise.noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);

        var prefabs = new List<CavePrefab>(){
            new CavePrefab(0){
                position = new Vector3i(10, 10, 10),
                size = new Vector3i(10, 10, 10),
            },
            new CavePrefab(1){
                position = new Vector3i(CaveBuilder.worldSize - 20, 10, CaveBuilder.worldSize - 20),
                size = new Vector3i(10, 10, 10),
            },
        };

        var vox = CollectCaveNoise(noise, 50, 50).ToHashSet();

        GenerateObjFile("noise.obj", vox, false);

        using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);

                for (int x = 0; x < CaveBuilder.worldSize; x++)
                {
                    for (int z = 0; z < CaveBuilder.worldSize; z++)
                    {
                        if (noise.IsTerrain(x, z))
                        {
                            b.SetPixel(x, z, NoiseColor);
                        }
                    }
                }

                DrawPrefabs(b, g, prefabs);
            }

            b.Save(@"noise.png", ImageFormat.Png);
        }
    }

    public static List<Voxell> CollectCaveNoise(CaveNoise noise, int worldSize, int height)
    {
        var noiseMap = new List<Voxell>();

        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (noise.IsCave(x, y, z))
                        noiseMap.Add(new Voxell(x, y, z, WaveFrontMat.DarkGray));
                }
            }
        }

        return noiseMap;
    }

    public static void PathCommand(string[] args)
    {
        CaveBuilder.worldSize = 100;
        CaveBuilder.radiationZoneMargin = 0;
        // CaveBuilder.pathingNoise = new CaveNoise(
        //     seed: CaveBuilder.seed,
        //     octaves: 1,
        //     frequency: 0.05f,
        //     threshold: 0.8f,
        //     invert: false,
        //     noiseType: FastNoiseLite.NoiseType.OpenSimplex2S,
        //     fractalType: FastNoiseLite.FractalType.Ridged
        // );

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var p1 = new CavePrefab(0)
        {
            position = new Vector3i(20, 5, 20),
            size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab(1)
        {
            position = new Vector3i(CaveBuilder.worldSize - 30, 50, CaveBuilder.worldSize - 30),
            size = new Vector3i(20, 10, 20),
        };

        p1.UpdateNodes(CaveBuilder.rand);
        p2.UpdateNodes(CaveBuilder.rand);

        var cachedPrefabs = new PrefabCache();

        cachedPrefabs.AddPrefab(p1);
        cachedPrefabs.AddPrefab(p2);

        // HashSet<Vector3i> obstacles = CaveBuilder.CollectPrefabObstacles(prefabs);
        // HashSet<Vector3i> noiseMap = CaveBuilder.CollectPrefabNoise(prefabs);

        var timer = new Stopwatch();
        timer.Start();

        HashSet<Vector3i> path = CaveTunneler.FindPath(p1.position, p2.position, cachedPrefabs);

        // path = CaveTunneler.ThickenCaveMap(path);

        Log.Out($"{p1.position} -> {p2.position} | Astar dist: {path.Count}, eucl dist: {CaveUtils.EuclidianDist(p1.position, p2.position)}, timer: {timer.ElapsedMilliseconds}ms");

        var prefabs = new HashSet<Voxell>(){
            new Voxell(p1.position, p1.size, WaveFrontMat.DarkGreen) { force = true },
            new Voxell(p2.position, p2.size, WaveFrontMat.DarkGreen) { force = true },
        };

        var cavemap = (
            from point in path
            select new Voxell(point, WaveFrontMat.DarkRed)
        );

        prefabs.UnionWith(cavemap);
        // prefabs.UnionWith(CollectCaveNoise(CaveBuilder.pathingNoise, CaveBuilder.worldSize, 60));

        GenerateObjFile("path.obj", prefabs, true);
    }

    public static void SaveCaveMap(HashSet<Vector3i> caveMap, string filename)
    {
        Log.Out("Exporting CaveMap");
        CaveBuilder.SaveCaveMap(filename, caveMap);
    }

    public static void test_prefabGrouping()
    {
        long memoryBefore = GC.GetTotalMemory(true);

        PrefabCache prefabCache = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);

        long memoryUsed = GC.GetTotalMemory(true) - memoryBefore;

        Log.Out($"Cave map size: {memoryUsed:N0} Bytes ({memoryUsed / 1_048_576.0:F1} MB)");
    }

    public static void CaveCommand(string[] args)
    {
        var timer = new Stopwatch();
        timer.Start();

        // CaveBuilder.pathingNoise = new CaveNoise(
        //     seed: CaveBuilder.SEED,
        //     octaves: 1,
        //     frequency: 0.01f,
        //     threshold: 0.8f,
        //     invert: false,
        //     noiseType: FastNoiseLite.NoiseType.OpenSimplex2S,
        //     fractalType: FastNoiseLite.FractalType.Ridged
        // );

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var cachedPrefabs = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);

        Log.Out("Start solving graph...");

        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        var wiredCaveMap = new ConcurrentBag<Vector3i>();
        int index = 0;

        Parallel.ForEach(edges, edge =>
        {
            Vector3i p1 = edge.node1.position;
            Vector3i p2 = edge.node2.position;

            Log.Out($"Noise pathing: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count}), dist={CaveUtils.SqrEuclidianDist(p1, p2)}");

            HashSet<Vector3i> path = CaveTunneler.FindPath(p1, p2, cachedPrefabs);

            foreach (Vector3i node in path)
            {
                wiredCaveMap.Add(node);
            }
        });

        var caveMap = wiredCaveMap.ToHashSet(); // CaveTunneler.ThickenCaveMap(wiredCaveMap.ToHashSet());

        SaveCaveMap(caveMap, "cavemap.txt");

        Log.Out("Start caves drawing");

        using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        {

            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);

                // DrawNoise(b, CaveBuilder.pathingNoise);
                DrawPoints(b, caveMap, TunnelsColor);
                DrawPrefabs(b, g, cachedPrefabs.Prefabs);
            }

            b.Save(@"cave.png", ImageFormat.Png);
        }

        Console.WriteLine($"{caveMap.Count:N0} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");

        // var voxels = (
        //     from block in caveMap
        //     select new Voxell(block, WaveFrontMat.DarkRed)
        // ).ToHashSet();

        // var prefabVox = (
        //     from block in cachedPrefabs.Prefabs
        //     select new Voxell(block.position, block.size, WaveFrontMat.DarkGreen)
        // ).ToHashSet();

        // voxels.UnionWith(prefabVox);

        // GenerateObjFile("cave.obj", voxels, false);
    }

    public static void PrefabCommand(string[] args)
    {
        var rand = CaveBuilder.rand;
        var mapCenter = new Vector3i(20, 20, 20);
        var prefab = new CavePrefab(0, rand);

        prefab.UpdateNodes(CaveBuilder.rand);

        var voxels = new HashSet<Voxell>(){
            new Voxell(mapCenter, prefab.size, WaveFrontMat.DarkGreen){ force = true },
        };

        foreach (var points in prefab.GetMarkerPoints())
        {
            Log.Out(points.Count.ToString());
            foreach (var point in points)
            {
                voxels.Add(new Voxell(point, WaveFrontMat.Orange));
            }
        }

        GenerateObjFile("prefab.obj", voxels, true);
    }

    public static void ProfileCaveMap(string filename)
    {

        long memoryBefore = GC.GetTotalMemory(true);

        var caveMap = CaveBuilder.ReadCaveMap(filename);
        int index = 0;

        // var caveMap3i = CaveBuilder.ReadCaveMap3i(filename);

        long memoryAfter = GC.GetTotalMemory(true);
        long memoryUsed = memoryAfter - memoryBefore;

        Log.Out($"Chunk count: {caveMap.Count}");

        foreach (var entry in caveMap)
        {

            Log.Out(entry.Key.ToString());
            Log.Out(string.Concat(Enumerable.Repeat("=", 10)));

            foreach (Vector3bf point in entry.Value)
            {
                index++;
                Log.Out(point.ToString());

            }

            Log.Out("");

            if (index > 100)
                break;
        }

        // Log.Out($"Memory before: {memoryBefore:N0} memory after {memoryAfter:N0}");
        Log.Out($"Cave map size: {memoryUsed:N0} Bytes ({memoryUsed / 1_048_576.0:F1} MB)");

        return;
    }

    public static void CaveMapCommand(string[] args)
    {
        string filename = "cavemap.txt";

        if (args.Length > 1)
        {
            ProfileCaveMap(filename);
            return;
        }

        var points = new HashSet<Vector3i>(){
            new Vector3i(0, 0, 0),
            new Vector3i(1, 0, 1),
            new Vector3i(1, 0, 2),
            new Vector3i(14, 0, 14),
            new Vector3i(1, 0, 18),
        };

        CaveBuilder.SaveCaveMap(filename, points);
    }

    public static void HexToRgb(string[] args)
    {
        var hexColor = args[1];

        if (hexColor.StartsWith("#"))
        {
            hexColor = hexColor.Substring(1);
        }

        if (hexColor.Length != 6)
        {
            throw new ArgumentException("La couleur hexadécimale doit être de 6 caractères.");
        }

        float r = 1f * Convert.ToInt32(hexColor.Substring(0, 2), 16) / 255;
        float g = 1f * Convert.ToInt32(hexColor.Substring(2, 2), 16) / 255;
        float b = 1f * Convert.ToInt32(hexColor.Substring(4, 2), 16) / 255;

        Console.WriteLine($"Kd {r:F2} {g:F2} {b:F2}".Replace(",", "."));
    }

    public static void GenerateObjFile(string filename, HashSet<Voxell> voxels, bool openFile = false)
    {
        int index = 0;

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("mtllib materials.mtl");

            foreach (var voxel in voxels)
            {
                string strVoxel = voxel.ToWavefront(ref index, voxels);

                if (strVoxel != "")
                    writer.WriteLine(strVoxel);
            }
        }

        if (openFile)
            Process.Start("CMD.exe", $"/C {Path.GetFullPath(filename)}");

    }

    public static void Main(string[] args)
    {
        // var vec = new Vector3bf(15, 255, 1);
        // Log.Out(vec.ToBinaryString());
        // Log.Out(vec.ToString());
        // return;

        Log.Out($"SEED .......... {CaveBuilder.SEED}");
        Log.Out($"SIZE .......... {CaveBuilder.worldSize}");
        Log.Out($"PREFAB_COUNT .. {CaveBuilder.PREFAB_COUNT}");
        Log.Out("");

        switch (args[0])
        {
            case "graph":
                GraphCommand(args);
                break;

            case "path":
                PathCommand(args);
                break;

            case "noise":
                NoiseCommand(args);
                break;

            case "cave":
            case "caves":
                CaveCommand(args);
                break;

            case "prefab":
                PrefabCommand(args);
                break;

            case "cavemap":
                CaveMapCommand(args);
                break;

            case "rgb":
                HexToRgb(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }

}


public static class WaveFrontMat
{
    public static string None = "";

    public static string DarkRed = "DarkRed";

    public static string DarkGreen = "DarkGreen";

    public static string Orange = "Orange";

    public static string DarkGray = "DarkGray";
}


public struct VoxelFace
{
    public int[] vertIndices;

    public VoxelFace(int[] values)
    {
        vertIndices = values;
    }

    public VoxelFace(int a, int b, int c, int d)
    {
        vertIndices = new int[4] { a, b, c, d };
    }

    public override string ToString()
    {
        return $"f {vertIndices[0]} {vertIndices[1]} {vertIndices[2]} {vertIndices[3]}";
    }
}


public class Voxell
{
    Vector3i position;

    Vector3i size = Vector3i.one;

    public bool force;

    public string material = "";

    public int[,] Vertices
    {
        get
        {
            int x = size.x;
            int y = size.y;
            int z = size.z;

            return new int[,]
            {
                {0, 0, 0},
                {x, 0, 0},
                {x, y, 0},
                {0, y, 0},
                {0, 0, z},
                {x, 0, z},
                {x, y, z},
                {0, y, z},
            };
        }
    }

    public static Dictionary<int, int[]> faceMapping = new Dictionary<int, int[]>()
    {
        { 0, new int[4]{ 1, 2, 6, 5 } }, // normal: x + 1
        { 1, new int[4]{ 3, 0, 4, 7 } }, // normal: x - 1
        { 2, new int[4]{ 2, 3, 7, 6 } }, // normal: y + 1
        { 3, new int[4]{ 0, 1, 5, 4 } }, // normal: y - 1
        { 4, new int[4]{ 4, 5, 6, 7 } }, // normal: z + 1
        { 5, new int[4]{ 0, 1, 2, 3 } }, // normal: z - 1
    };

    public Voxell(Vector3i position)
    {
        this.position = position;
    }

    public Voxell(Vector3i position, string material)
    {
        this.position = position;
        this.material = material;
    }

    public Voxell(Vector3i position, Vector3i size)
    {
        this.position = position;
        this.size = size;
    }

    public Voxell(Vector3i position, Vector3i size, string material)
    {
        this.position = position;
        this.size = size;
        this.material = material;
    }

    public Voxell(Vector3i position, int sizeX, int sizeY, int sizeZ)
    {
        this.position = position;
        size = new Vector3i(sizeX, sizeY, sizeZ);
    }

    public Voxell(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
    }

    public Voxell(int x, int y, int z, string material)
    {
        position = new Vector3i(x, y, z);
        this.material = material;
    }

    public bool[] GetNeighbors(HashSet<Voxell> others)
    {
        if (force)
            return new bool[6] { true, true, true, true, true, true };

        var result = new bool[6];
        var neighbors = new List<Voxell>()
        {
            new Voxell(position.x + 1, position.y, position.z),
            new Voxell(position.x - 1, position.y, position.z),
            new Voxell(position.x, position.y + 1, position.z),
            new Voxell(position.x, position.y - 1, position.z),
            new Voxell(position.x, position.y, position.z + 1),
            new Voxell(position.x, position.y, position.z - 1),
        };

        for (int i = 0; i < 6; i++)
        {
            result[i] = !others.Contains(neighbors[i]);
        }

        return result;
    }

    public bool ShouldAddVertice(int index, bool[] neighbors)
    {
        // 0 {0, 0, 0},
        // 1 {x, 0, 0},
        // 2 {x, y, 0},
        // 3 {0, y, 0},
        // 4 {0, 0, z},
        // 5 {x, 0, z},
        // 6 {x, y, z},
        // 7 {0, y, z},

        /*
            0 | 0 1 2 3 | z - 1
            1 | 4 5 6 7 | z + 1
            2 | 0 1 5 4 | y - 1
            3 | 1 2 6 5 | x + 1
            4 | 2 3 7 6 | y + 1
            5 | 3 0 4 7 | x - 1
        */

        if (index == 0)
            return neighbors[0] || neighbors[2] || neighbors[5];

        if (index == 1)
            return neighbors[0] || neighbors[2] || neighbors[3];

        if (index == 2)
            return neighbors[0] || neighbors[3] || neighbors[4];

        if (index == 3)
            return neighbors[0] || neighbors[4] || neighbors[5];

        if (index == 4)
            return neighbors[1] || neighbors[4] || neighbors[5];

        if (index == 5)
            return neighbors[1] || neighbors[2] || neighbors[3];

        if (index == 6)
            return neighbors[1] || neighbors[3] || neighbors[4];

        if (index == 7)
            return neighbors[1] || neighbors[4] || neighbors[5];

        return false;
    }

    public string ToWavefront(ref int vertexIndexOffset, HashSet<Voxell> others)
    {
        var strVertices = new List<string>();

        for (int i = 0; i < 8; i++)
        {
            int vx = Vertices[i, 0] + position.x;
            int vy = Vertices[i, 1] + position.y;
            int vz = Vertices[i, 2] + position.z;

            strVertices.Add($"v {vx} {vy} {vz}");
        }

        var faces = new List<VoxelFace>();
        var usedVerticeIndexes = new Dictionary<int, int>();
        var neighbors = GetNeighbors(others);
        var result = new List<string>();

        foreach (var entry in faceMapping)
        {
            int faceIndex = entry.Key;
            int[] verticeIndexes = entry.Value;

            if (!neighbors[faceIndex])
                continue;

            for (int i = 0; i < 4; i++)
            {
                int index = verticeIndexes[i];

                if (!usedVerticeIndexes.ContainsKey(index))
                {
                    result.Add(strVertices[index] + $" # {vertexIndexOffset + 1}");
                    usedVerticeIndexes[index] = ++vertexIndexOffset;
                }
            }

            faces.Add(new VoxelFace(
                usedVerticeIndexes[verticeIndexes[0]],
                usedVerticeIndexes[verticeIndexes[1]],
                usedVerticeIndexes[verticeIndexes[2]],
                usedVerticeIndexes[verticeIndexes[3]]
            ));
        }

        if (faces.Count == 0)
            return "";

        if (material != "")
            result.Add($"usemtl {material}");

        foreach (var face in faces)
        {
            result.Add(face.ToString());
        }

        return string.Join("\n", result);
    }

    public override int GetHashCode()
    {
        return position.GetHashCode() + size.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var other = (Voxell)obj;

        return GetHashCode() == other.GetHashCode();
    }

}
