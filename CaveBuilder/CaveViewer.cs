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
    public static int MAP_SIZE => CaveBuilder.worldSize;

    public static Random Rand => CaveBuilder.rand;

    public static int SEED => CaveBuilder.SEED;

    public static int PREFAB_COUNT => CaveBuilder.PREFAB_COUNT;

    public static float NOISE_THRESHOLD => CaveBuilder.NOISE_THRESHOLD;

    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.DarkGray;

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

                if (fill)
                    DrawPoints(b, prefab.GetInnerPoints().ToHashSet(), PrefabBoundsColor);

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
                ParsePointF(edge.node1),
                ParsePointF(edge.node2),
            });
            }
        }
    }

    public static void DrawNoise(Bitmap b, FastNoiseLite perlinNoise)
    {
        for (int x = 0; x < MAP_SIZE; x++)
        {
            for (int z = 0; z < MAP_SIZE; z++)
            {
                float noise = 0.5f * (perlinNoise.GetNoise(x, z) + 1);

                if (noise < NOISE_THRESHOLD)
                    b.SetPixel(x, z, NoiseColor);
            }
        }
    }

    public static void GraphCommand(string[] args)
    {
        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;

        var prefabs = CaveBuilder.GetRandomPrefabs(prefabCounts);

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = GraphSolver.Resolve(prefabs);

        Logger.Info("Start Drawing graph...");

        using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawEdges(g, edges);
                DrawPrefabs(b, g, prefabs);
            }

            Logger.Info($"{edges.Count} Generated edges.");

            b.Save(@"graph.png", ImageFormat.Png);
        }
    }

    public static void NoiseCommand(string[] args)
    {
        var noise = CaveBuilder.ParsePerlinNoise();

        using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawNoise(b, noise);
            }

            b.Save(@"noise.png", ImageFormat.Png);
        }
    }

    public static void PathCommand(string[] args)
    {
        var p1 = new CavePrefab()
        {
            position = new Vector3i(20, 20, 20),
            size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab()
        {
            position = new Vector3i(MAP_SIZE - 30, 50, MAP_SIZE - 30),
            size = new Vector3i(20, 10, 20),
        };

        var prefabs = new List<CavePrefab>() { p1, p2 };

        // HashSet<Vector3i> obstacles = CaveBuilder.CollectPrefabObstacles(prefabs);
        // HashSet<Vector3i> noiseMap = CaveBuilder.CollectPrefabNoise(prefabs);

        var timer = new Stopwatch();
        timer.Start();

        HashSet<Vector3i> path = CaveTunneler.FindPath(p1.position, p2.position, prefabs);

        Logger.Info($"{p1.position} -> {p2.position} | Astar dist: {path.Count}, eucl dist: {CaveUtils.EuclidianDist(p1.position, p2.position)}, timer: {timer.ElapsedMilliseconds}ms");

        // using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        // {
        //     using (Graphics g = Graphics.FromImage(b))
        //     {
        //         g.Clear(BackgroundColor);
        //         // DrawNoise(b, noise);
        //         // DrawPoints(b, noiseMap, NoiseColor);
        //         DrawPoints(b, path, TunnelsColor);
        //         DrawPrefabs(b, g, prefabs);

        //         b.SetPixel(p1.position.x, p1.position.z, NodeColor);
        //         b.SetPixel(p2.position.x, p2.position.z, NodeColor);
        //     }

        //     b.Save(@"pathing.png", ImageFormat.Png);
        // }

        var voxels = (
            from point in path
            select new Voxell(point, WaveFrontMat.DarkRed)
        ).ToList();

        voxels.Add(new Voxell(p1.position, p1.size, WaveFrontMat.DarkGreen));
        voxels.Add(new Voxell(p2.position, p2.size, WaveFrontMat.DarkGreen));

        GenerateObjFile("path.obj", voxels, true);
    }

    public static void SaveCaveMap(HashSet<Vector3i> caveMap, string filename)
    {
        Log.Out("Exporting CaveMap");
        CaveBuilder.SaveCaveMap(filename, caveMap);
    }

    public static void CaveCommand(string[] args)
    {
        var timer = new Stopwatch();
        timer.Start();

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var prefabs = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);
        var chunksDict = CaveBuilder.GroupPrefabsByChunk(prefabs);
        var choice = chunksDict[chunksDict.Keys.ElementAt(CaveBuilder.rand.Next(chunksDict.Count))][0];

        var voxels = new List<Voxell>();

        using (var writer = new StreamWriter("chunks.txt"))
        {
            foreach (var entry in chunksDict)
            {
                writer.WriteLine(entry.Key.ToString());
                writer.WriteLine(string.Concat(Enumerable.Repeat("-", 10)));

                foreach (var prefab in entry.Value)
                {
                    if (prefab.position == choice.position)
                        voxels.Add(new Voxell(entry.Key * 16, new Vector3i(15, 1, 15), WaveFrontMat.DarkRed));

                    writer.WriteLine(prefab.position.ToString());
                }

                writer.WriteLine("");
            }
        }

        voxels.Add(new Voxell(new Vector3i(choice.position.x, 10, choice.position.z), choice.size, WaveFrontMat.DarkGreen));

        GenerateObjFile("chunks.obj", voxels, true);

        return;

        Logger.Info("Start solving graph...");

        List<Edge> edges = GraphSolver.Resolve(prefabs);

        var wiredCaveMap = new ConcurrentBag<Vector3i>();
        int index = 0;

        Parallel.ForEach(edges, edge =>
        {
            Vector3i p1 = edge.node1;
            Vector3i p2 = edge.node2;

            Logger.Info($"Noise pathing: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count}), dist={CaveUtils.SqrEuclidianDist(p1, p2)}");

            HashSet<Vector3i> path = CaveTunneler.FindPath(p1, p2, prefabs);

            foreach (Vector3i node in path)
            {
                wiredCaveMap.Add(node);
            }
        });

        Logger.Info("Start caves thickening");

        var caveMap = CaveTunneler.ThickenCaveMap(wiredCaveMap.ToHashSet());

        SaveCaveMap(caveMap, "cavemap.txt");

        Logger.Info("Start caves drawing");

        using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        {

            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);

                DrawPoints(b, caveMap, TunnelsColor);
                DrawPrefabs(b, g, prefabs);
            }

            b.Save(@"cave.png", ImageFormat.Png);
        }



        Console.WriteLine($"{caveMap.Count} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");
    }

    public static void PrefabCommand(string[] args)
    {
        var mapCenter = new Vector3i(20, 20, 20);
        var prefab = new CavePrefab()
        {
            position = mapCenter,
            size = new Vector3i(10, 10, 10),
        };

        prefab.UpdateNodes(Rand);

        var voxels = (
            from point in CaveBuilder.ParseCircle(prefab.GetCenter(), 80)
            select new Voxell(point, WaveFrontMat.Orange)
        ).ToList();

        voxels.Add(new Voxell(mapCenter, prefab.size, WaveFrontMat.DarkGreen));

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

    public static void GenerateCaveMap(string[] args)
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

    public static void GenerateObjFile(string[] args)
    {

        var filename = "example.obj";
        var points = new List<Vector3i>()
        {
            new Vector3i(0, 0, 0),
            new Vector3i(0, 2, 0),
        };

        var index = 0;

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("mtllib materials.mtl");
            writer.WriteLine(new Voxell(points[0]).ToWavefront(ref index));
            writer.WriteLine(new Voxell(points[1]).ToWavefront(ref index));
        }

        Log.Out($"index: {index}");
    }

    public static void GenerateObjFile(string filename, IEnumerable<Voxell> voxels, bool openFile = false)
    {
        int index = 0;

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("mtllib materials.mtl");

            foreach (var voxel in voxels)
            {
                writer.WriteLine(voxel.ToWavefront(ref index));
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

        Logger.Info($"SEED .......... {SEED}");
        Logger.Info($"SIZE .......... {MAP_SIZE}");
        Logger.Info($"PREFAB_COUNT .. {PREFAB_COUNT}");
        Logger.Blank();

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
                GenerateCaveMap(args);
                break;

            case "obj":
            case "wavefront":
                GenerateObjFile(args);
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
}

public class Voxell
{
    Vector3i position;

    Vector3i size = Vector3i.one;

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

    public string ToWavefront(ref int vertexIndexOffset)
    {
        var vertIndices = new int[8];
        var result = new List<string>();

        for (int i = 0; i < Vertices.GetLength(0); i++)
        {
            int vx = Vertices[i, 0] + position.x;
            int vy = Vertices[i, 1] + position.y;
            int vz = Vertices[i, 2] + position.z;

            result.Add($"v {vx} {vy} {vz}");

            vertIndices[i] = ++vertexIndexOffset;
        }

        if (material != "")
            result.Add($"usemtl {material}");

        result.Add($"f {vertIndices[0]} {vertIndices[1]} {vertIndices[2]} {vertIndices[3]}");
        result.Add($"f {vertIndices[4]} {vertIndices[5]} {vertIndices[6]} {vertIndices[7]}");
        result.Add($"f {vertIndices[0]} {vertIndices[1]} {vertIndices[5]} {vertIndices[4]}");
        result.Add($"f {vertIndices[1]} {vertIndices[2]} {vertIndices[6]} {vertIndices[5]}");
        result.Add($"f {vertIndices[2]} {vertIndices[3]} {vertIndices[7]} {vertIndices[6]}");
        result.Add($"f {vertIndices[3]} {vertIndices[0]} {vertIndices[4]} {vertIndices[7]}");

        return string.Join("\n", result);
    }
}
