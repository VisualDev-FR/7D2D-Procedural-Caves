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
    public static int MAP_SIZE = CaveBuilder.MAP_SIZE;

    public static Random Rand = CaveBuilder.rand;

    public static int SEED = CaveBuilder.SEED;

    public static int PREFAB_COUNT = CaveBuilder.PREFAB_COUNT;

    public static float NOISE_THRESHOLD = CaveBuilder.NOISE_THRESHOLD;

    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

    public static readonly Color NoiseColor = Color.DarkGray;

    public static PointF ParsePointF(Vector3i point)
    {
        return new PointF(point.x, point.z);
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<PrefabWrapper> prefabs, bool fill = false)
    {
        using (var pen = new Pen(PrefabBoundsColor, 1))
        {
            foreach (var prefab in prefabs)
            {
                graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);

                if (fill)
                    DrawPoints(b, new HashSet<Vector3i>(prefab.innerPoints), PrefabBoundsColor);

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

    public static void GenerateGraph(string[] args)
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

    public static void GenerateNoise(string[] args)
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

    public static void GeneratePath(string[] args)
    {
        int positionY = 10;

        var p1 = new PrefabWrapper()
        {
            position = new Vector3i(10, positionY, 10),
            size = new Vector3i(10, 10, 10),
        };

        var p2 = new PrefabWrapper()
        {
            position = new Vector3i(MAP_SIZE - 20, positionY, MAP_SIZE - 20),
            size = new Vector3i(10, 10, 10),
        };

        var prefabs = new List<PrefabWrapper>() { p1, p2 };

        p1.UpdateInnerPoints();
        p2.UpdateInnerPoints();

        FastNoiseLite noise = CaveBuilder.ParsePerlinNoise();
        HashSet<Vector3i> obstacles = CaveBuilder.CollectPrefabObstacles(prefabs);
        HashSet<Vector3i> noiseMap = CaveBuilder.CollectPrefabNoise(prefabs, noise);

        HashSet<Vector3i> path = Astar.PerlinRoute(p1.position, p2.position, noise, obstacles, noiseMap);

        using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                // DrawNoise(b, noise);
                DrawPoints(b, noiseMap, NoiseColor);
                DrawPoints(b, path, TunnelsColor);
                DrawPrefabs(b, g, prefabs);

                b.SetPixel(p1.position.x, p1.position.z, NodeColor);
                b.SetPixel(p2.position.x, p2.position.z, NodeColor);
            }

            b.Save(@"pathing.png", ImageFormat.Png);
        }

        ToWaveFront(path.ToList(), "path.obj");

        Process.Start("CMD.exe", $"/C {Path.GetFullPath("path.obj")}");
    }

    public static void SaveCaveMap(HashSet<Vector3i> caveMap, string filename)
    {
        using (var writer = new StreamWriter(filename))
        {
            foreach (var caveBlock in caveMap)
            {
                writer.WriteLine(caveBlock.ToString());
            }
            Logger.Info($"CaveMap saved '{filename}'.");
        }
    }

    public static void GenerateCaves(string[] args)
    {
        var timer = new Stopwatch();
        timer.Start();

        FastNoiseLite noise = CaveBuilder.ParsePerlinNoise();

        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : PREFAB_COUNT;
        var prefabs = CaveBuilder.GetRandomPrefabs(prefabCounts);

        HashSet<Vector3i> obstacles = CaveBuilder.CollectPrefabObstacles(prefabs);
        HashSet<Vector3i> noiseMap = CaveBuilder.CollectPrefabNoise(prefabs, noise);

        Logger.Info("Start solving MST Krustal...");

        List<Edge> edges = GraphSolver.Resolve(prefabs);

        var wiredCaveMap = new ConcurrentBag<Vector3i>();
        int index = 0;

        Parallel.ForEach(edges, edge =>
        {
            Vector3i p1 = edge.node1;
            Vector3i p2 = edge.node2;

            Logger.Info($"Noise pathing: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count}), dist={CaveUtils.SqrEuclidianDist(p1, p2)}");

            HashSet<Vector3i> path = Astar.PerlinRoute(p1, p2, noise, obstacles, noiseMap);

            foreach (Vector3i node in path)
            {
                wiredCaveMap.Add(node);
            }
        });

        Logger.Info("Start caves thickening");

        var caveMap = Astar.ThickenCaveMap(wiredCaveMap.ToHashSet(), obstacles);

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

        SaveCaveMap(caveMap, "cavemap.csv");

        Console.WriteLine($"{caveMap.Count} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}.");
    }

    public static void GeneratePrefab(string[] args)
    {
        var mapCenter = new Vector3i(-10 + MAP_SIZE / 2, 0, -10 + MAP_SIZE / 2);
        var prefab = new PrefabWrapper()
        {
            position = mapCenter,
            size = new Vector3i(10, 10, 10),
        };

        prefab.UpdateNodes(Rand);
        prefab.UpdateInnerPoints();

        using (var b = new Bitmap(MAP_SIZE, MAP_SIZE))
        {
            using (Pen pen = new Pen(PrefabBoundsColor, 1))
            {
                var noise = CaveBuilder.ParsePerlinNoise(SEED);

                using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(BackgroundColor);

                    DrawPoints(b, prefab.GetNoiseAround(), NoiseColor);
                    g.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.size.x, prefab.size.z);
                    DrawPoints(b, prefab.nodes.ToHashSet(), NodeColor);
                }
            }

            b.Save(@"prefab.png", ImageFormat.Png);
        }
    }

    static void ToWaveFront(List<Vector3i> positions, string filename)
    {
        float[,] vertices = new float[,]
        {
            {0f, 0f, 0f},
            {1f, 0f, 0f},
            {1f, 1f, 0f},
            {0f, 1f, 0f},
            {0f, 0f, 1f},
            {1f, 0f, 1f},
            {1f, 1f, 1f},
            {0f, 1f, 1f}
        };

        using (StreamWriter writer = new StreamWriter(filename))
        {
            int vertexIndexOffset = 1;

            for (int cubeIndex = 0; cubeIndex < positions.Count; cubeIndex++)
            {
                float x = positions[cubeIndex].x;
                float y = positions[cubeIndex].y;
                float z = positions[cubeIndex].z;

                var cubeVerticesIndices = new int[vertices.GetLength(0)];

                for (int i = 0; i < vertices.GetLength(0); i++)
                {
                    float vx = vertices[i, 0] + x;
                    float vy = vertices[i, 1] + y;
                    float vz = vertices[i, 2] + z;

                    writer.WriteLine($"v {vx} {vy} {vz}");

                    cubeVerticesIndices[i] = vertexIndexOffset;
                    vertexIndexOffset++;
                }

                writer.WriteLine($"f {cubeVerticesIndices[0]} {cubeVerticesIndices[1]} {cubeVerticesIndices[2]} {cubeVerticesIndices[3]}"); // Face inférieure
                writer.WriteLine($"f {cubeVerticesIndices[4]} {cubeVerticesIndices[5]} {cubeVerticesIndices[6]} {cubeVerticesIndices[7]}"); // Face supérieure
                writer.WriteLine($"f {cubeVerticesIndices[0]} {cubeVerticesIndices[1]} {cubeVerticesIndices[5]} {cubeVerticesIndices[4]}"); // Face latérale
                writer.WriteLine($"f {cubeVerticesIndices[1]} {cubeVerticesIndices[2]} {cubeVerticesIndices[6]} {cubeVerticesIndices[5]}"); // Face latérale
                writer.WriteLine($"f {cubeVerticesIndices[2]} {cubeVerticesIndices[3]} {cubeVerticesIndices[7]} {cubeVerticesIndices[6]}"); // Face latérale
                writer.WriteLine($"f {cubeVerticesIndices[3]} {cubeVerticesIndices[0]} {cubeVerticesIndices[4]} {cubeVerticesIndices[7]}"); // Face latérale
            }
        }

        Console.WriteLine($"{positions.Count} voxels generated to '{filename}'.");
    }

    public static void Main(string[] args)
    {
        Logger.Info($"SEED .......... {SEED}");
        Logger.Info($"SIZE .......... {MAP_SIZE}");
        Logger.Info($"PREFAB_COUNT .. {PREFAB_COUNT}");
        Logger.Blank();

        switch (args[0])
        {
            case "graph":
                GenerateGraph(args);
                break;

            case "path":
                GeneratePath(args);
                break;

            case "noise":
                GenerateNoise(args);
                break;

            case "cave":
            case "caves":
                GenerateCaves(args);
                break;

            case "prefab":
                GeneratePrefab(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }

}