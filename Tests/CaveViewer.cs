using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using System.Numerics;

public static class CaveViewer
{
    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color UnderGroundColor = Color.Green;

    public static readonly Color EntranceColor = Color.Yellow;

    public static readonly Color RoomColor = Color.DarkGray;

    public static readonly Color NoiseColor = Color.FromArgb(84, 84, 82);

    public static void RunCoroutine(IEnumerator coroutine)
    {
        while (coroutine.MoveNext())
        {
            if (coroutine.Current is IEnumerator enumerator)
            {
                RunCoroutine(enumerator);
            }
        }
    }

    public static PointF ParsePointF(Vector3i point)
    {
        return new PointF(point.x, point.z);
    }

    public static void DrawGrid(Bitmap b, Graphics graph, int worldSize, int gridSize)
    {
        for (int x = gridSize; x < worldSize; x += gridSize)
        {
            var color = Color.FromArgb(50, Color.FromName("DarkGray"));

            using (var pen = new Pen(color, 1))
            {
                graph.DrawLine(pen, new PointF(x, 0), new PointF(x, worldSize));
                graph.DrawLine(pen, new PointF(0, x), new PointF(worldSize, x));
            }
        }
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<CavePrefab> prefabs)
    {
        foreach (var prefab in prefabs)
        {
            // var baseColor = Color.FromArgb(prefab.Name.GetHashCode());
            // var color = Color.FromArgb(255, baseColor);

            var color = UnderGroundColor;

            if (prefab.isRoom)
            {
                color = RoomColor;
            }
            else if (prefab.isEntrance)
            {
                color = EntranceColor;
            }

            using (var pen = new Pen(color, 1))
            {
                graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.Size.x, prefab.Size.z);
            }

            foreach (var node in prefab.nodes)
            {
                b.SetPixel(node.position.x, node.position.z, NodeColor);
            }
        }
    }

    public static void DrawPoints(Bitmap bitmap, IEnumerable<Vector3i> points, Color color)
    {
        foreach (var point in points)
        {
            bitmap.SetPixel(point.x, point.z, color);
        }
    }

    public static void DrawEdges(Graphics graph, List<GraphEdge> edges)
    {
        foreach (var edge in edges)
        {
            // var color = edge.isVirtual ? RoomColor : TunnelsColor;
            var color = Color.FromName(edge.colorName);

            using (var pen = new Pen(Color.FromArgb(edge.opacity, color), edge.width))
            {
                graph.DrawCurve(pen, new PointF[2]{
                    ParsePointF(edge.node1.position),
                    ParsePointF(edge.node2.position),
                });
            }
        }
    }

    public static void DrawNoise(Bitmap b, CaveNoise noise, int worldSize)
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                if (noise.IsCave(x, z))
                    b.SetPixel(x, z, NoiseColor);
            }
        }
    }

    public static void GraphCommand(string[] args)
    {
        // var prefabs = PrefabLoader.LoadPrefabs().Values.ToList();

        Random random = new Random(1337);

        int worldSize = 1024 * 2;
        int prefabCounts = worldSize / 5;
        int gridSize = 150;
        int minMarkers = 2;
        int maxMarkers = 6;

        var prefabManager = new CavePrefabManager(worldSize);
        var heightMap = new RawHeightMap(worldSize, 128);

        prefabManager.SetupBoundaryPrefabs(random, gridSize);
        prefabManager.GetRandomPrefabs(random, heightMap, prefabCounts, minMarkers, maxMarkers);

        var graph = new Graph(prefabManager.Prefabs, worldSize);
        var voxels = new HashSet<Voxell>();

        foreach (var prefab in prefabManager.Prefabs)
        {
            voxels.Add(new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen) { force = true });

            foreach (var node in prefab.nodes)
            {
                voxels.Add(new Voxell(node.prefab.position + node.marker.start, node.marker.size, WaveFrontMaterial.Orange) { force = true });
            }
        }

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawGrid(b, g, worldSize, gridSize);
                DrawEdges(g, graph.Edges.ToList());
                DrawPrefabs(b, g, prefabManager.Prefabs);
            }

            b.Save(@"graph.png", ImageFormat.Png);
        }
    }

    public static void BezierCommand(string[] args)
    {
        Vector2 p0 = new Vector2(0, 0);
        Vector2 p1 = new Vector2(50, 100);
        Vector2 p2 = new Vector2(100, 0);

        int numPoints = 100;

        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i <= numPoints; i++)
        {
            float t = i / (float)numPoints;
            float x = (1 - t) * (1 - t) * p0.X + 2 * (1 - t) * t * p1.X + t * t * p2.X;
            float y = (1 - t) * (1 - t) * p0.Y + 2 * (1 - t) * t * p1.Y + t * t * p2.Y;

            points.Add(new Vector2(x, y));
        }

        using (var b = new Bitmap(150, 150))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawPoints(b, points.Select(p => new Vector3i(p.X, 0, p.Y)), TunnelsColor);
            }

            b.Save("bezier.png", ImageFormat.Png);
        }

    }

    public static void PathCommand(string[] args)
    {
        int worldSize = 100;
        var rand = new Random(1337);
        var heightMap = new RawHeightMap(worldSize, 128);

        if (args.Length > 1)
            worldSize = int.Parse(args[1]);

        var p1 = new CavePrefab(0)
        {
            position = new Vector3i(20, 5, 20),
            Size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab(1)
        {
            position = new Vector3i(20, 50, worldSize - 30),
            Size = new Vector3i(20, 10, 20),
        };

        p1.UpdateMarkers(rand);
        p2.UpdateMarkers(rand);

        var cachedPrefabs = new CavePrefabManager(worldSize);
        cachedPrefabs.AddPrefab(p1);
        cachedPrefabs.AddPrefab(p2);

        var node1 = p1.nodes[1];
        var node2 = p2.nodes[0];

        var edge = new GraphEdge(node1, node2);

        Log.Out($"prefab   {node2.prefab.position}");
        Log.Out($"start    {node2.marker.start}");
        Log.Out($"size     {node2.marker.size}");
        Log.Out($"result   {node2.position}\n");

        CaveTunnel.InitSpheres(5);

        var timer = CaveUtils.StartTimer();
        var cavemap = new CaveMap();
        var tunnel = new CaveTunnel(edge, cachedPrefabs, heightMap, worldSize);

        cavemap.AddTunnel(tunnel);

        Log.Out($"{p1.position} -> {p2.position} | Astar dist: {tunnel.path.Count}, eucl dist: {CaveUtils.EuclidianDist(p1.position, p2.position)}, timer: {timer.ElapsedMilliseconds}ms");
        Log.Out($"{tunnel.localMinimas.Count} water blocks found");

        var voxels = new HashSet<Voxell>(){
            new Voxell(p1.position, p1.Size, WaveFrontMaterial.DarkGreen) { force = true },
            new Voxell(p2.position, p2.Size, WaveFrontMaterial.DarkGreen) { force = true },
        };

        foreach (var block in tunnel.blocks)
        {
            if (cavemap.GetBlock(block.GetHashCode()).isWater)
            {
                voxels.Add(new Voxell(block.x, block.y, block.z, WaveFrontMaterial.LightBlue));
            }
            else
            {
                voxels.Add(new Voxell(block.x, block.y, block.z, WaveFrontMaterial.DarkRed));
            }
        }

        foreach (var node in p1.nodes)
        {
            foreach (var point in node.GetMarkerPoints())
            {
                voxels.Add(new Voxell(point, WaveFrontMaterial.Orange) { force = true });
            }
        }

        foreach (var node in p2.nodes)
        {
            foreach (var point in node.GetMarkerPoints())
            {
                voxels.Add(new Voxell(point, WaveFrontMaterial.Orange) { force = true });
            }
        }

        GenerateObjFile("path.obj", voxels, true);
    }

    public static void SphereCommand(string[] args)
    {
        var voxels = new HashSet<Voxell>();

        for (int i = CaveTunnel.minRadius; i <= CaveTunnel.maxRadius; i++)
        {
            int radius = i;
            int pos = i * radius * 3;

            var timer = CaveUtils.StartTimer();
            var position = new Vector3i(pos, 20, 20);
            var caveBlock = new CaveBlock(position);
            var sphere = CaveTunnel.GetSphere(caveBlock.ToVector3i(), radius);

            Log.Out($"radius: {radius}, blocks: {sphere.ToList().Count}, timer: {timer.ElapsedMilliseconds} ms");

            foreach (var block in sphere)
            {
                voxels.Add(new Voxell(block.x, block.y, block.z));
            }
        }


        GenerateObjFile("sphere.obj", voxels, false);
    }

    public static void RoomCommand(string[] args)
    {
        var _ = CaveTunnel.spheres.Count;

        var timer = CaveUtils.StartTimer();
        var seed = DateTime.Now.GetHashCode();
        var random = new Random(seed);
        var size = new Vector3i(50, 20, 50);
        var prefab = new CavePrefab(0)
        {
            Size = size,
            position = new Vector3i(0, 20, 0),
        };
        prefab.UpdateMarkers(random);
        var room = new CaveRoom(prefab, seed);

        var voxels = room.GetBlocks().Select(pos => new Voxell(pos)).ToHashSet();

        Log.Out($"timer: {timer.ElapsedMilliseconds}ms");

        // var voxels = new HashSet<Voxell>
        // {
        //     new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen)
        // };

        // voxels.UnionWith(prefab.caveMarkers.Select(marker => new Voxell(position + marker.start, marker.size, WaveFrontMaterial.Orange)));

        GenerateObjFile("room.obj", voxels, false);
    }

    public static void CaveCommand(string[] args)
    {
        int worldSize = 1024;
        int seed = 1337;
        int prefabCount = worldSize / 5;

        var timer = CaveUtils.StartTimer();
        var prefabs = PrefabLoader.LoadPrefabs().Values.ToList();
        var cachedPrefabs = new CavePrefabManager(worldSize);
        var rand = new Random(seed);
        var heightMap = new RawHeightMap(worldSize, 64);

        cachedPrefabs.GetRandomPrefabs(rand, heightMap, prefabCount, prefabs);

        Log.Out("Start solving graph...");

        var memoryBefore = GC.GetTotalMemory(true);
        var graph = new Graph(cachedPrefabs.Prefabs, worldSize);
        var index = 0;

        object lockObject = new object();

        var cavemap = new CaveMap();
        var localMinimas = new HashSet<CaveBlock>();

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);

                Parallel.ForEach(graph.Edges, edge =>
                {
                    Log.Out($"Cave tunneling: {100.0f * index++ / graph.Edges.Count:F0}% ({index} / {graph.Edges.Count}) {cavemap.Count:N0}");

                    var tunnel = new CaveTunnel(edge, cachedPrefabs, heightMap, worldSize);

                    lock (lockObject)
                    {
                        cavemap.AddTunnel(tunnel);

                        foreach (CaveBlock caveBlock in tunnel.blocks)
                        {
                            b.SetPixel(caveBlock.x, caveBlock.z, TunnelsColor);
                        }
                    }
                });

                DrawPrefabs(b, g, cachedPrefabs.Prefabs);
                b.Save(@"cave.png", ImageFormat.Png);
            }
        }

        // cavemap.SetWater(localMinimas, cachedPrefabs);

        Log.Out($"{cavemap.Count:N0} cave blocks generated ({cavemap.TunnelsCount} unique tunnels), timer={timer.ElapsedMilliseconds:N0}ms, memory={(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576.0:F1}MB.");
        Log.Out($"{localMinimas.Count} local minimas");

        if (worldSize > 1024)
            return;

        bool isFloor(CaveBlock block) => block.isFloor && block.isFlat;

        var voxels = cavemap
            .Where(isFloor)
            .Select(block => new Voxell(block.x, block.y, block.z, WaveFrontMaterial.LightBlue))
            .ToHashSet();

        Log.Out($"{voxels.Count} water blocks");

        // var tunnels = cavemap.tunnels.ElementAt(0).Value.blocks
        //     .Where(block => !isFloor(block))
        //     .Select(block => new Voxell(block.x, block.y, block.z, WaveFrontMaterial.DarkRed))
        //     .ToHashSet();

        foreach (var tunnel in cavemap.tunnels.Values)
        {
            var tunnelBlocks = tunnel.blocks
                .Select(block => new Voxell(block.x, block.y, block.z, WaveFrontMaterial.DarkRed))
                .ToHashSet();

            GenerateObjFile($"wavefront/tunnel_{tunnel.id.value}.obj", tunnelBlocks, false);
        }

        // voxels.UnionWith(tunnels);
        // voxels = tunnels;

        // foreach (var prefab in cachedPrefabs.Prefabs)
        // {
        //     voxels.Add(new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen) { force = true });

        //     foreach (var marker in prefab.caveMarkers)
        //     {
        //         voxels.Add(new Voxell(prefab.position + marker.start, marker.size, WaveFrontMaterial.Orange) { force = true });
        //     }
        // }

        // GenerateObjFile("cave.obj", voxels, false);
    }

    public static void CellularAutomaCommand(string[] args)
    {
        var seed = -1;
        var size = new Vector3i(50, 20, 100);
        var room = new CaveRoom(Vector3i.zero, size, seed);

        var timer = CaveUtils.StartTimer();
        var memoryBefore = GC.GetTotalMemory(true);

        var voxels = room.GetBlocks()
            .Select(pos => new Voxell(pos))
            .ToHashSet();

        Log.Out($"{voxels.Count:N0} blocks, timer: {timer.ElapsedMilliseconds}ms, memory: {GC.GetTotalMemory(true) - memoryBefore:N0} bytes");

        GenerateObjFile("cellular.obj", voxels, false);
    }

    public static void CaveMapToWaveFront()
    {
        throw new NotImplementedException();
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
        var prefabs = PrefabLoader.LoadPrefabs();

        foreach (var entry in prefabs)
        {
            var prefab = entry.Value;

            Log.Out($"{entry.Key}: {prefab.POIMarkers.Count}");
        }

        Log.Out($"{prefabs.Count} prefabs found.");
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

    public static void BitCommand()
    {
        var memoryBefore = GC.GetTotalMemory(true);
        var size = new Vector3i(512, 256, 512);
        var array = new BitArray(size.x * size.y * size.z);

        int Index(int x, int y, int z) => x + (y * size.x) + (z * size.x * size.y);

        foreach (var offset in CaveUtils.offsets)
        {
            var p1 = new Vector3i(10, 10, 10);
            var p2 = p1 + offset;

            var idx1 = Index(p1.x, p1.y, p1.z);
            var idx2 = Index(p2.x, p2.y, p2.z);

            var idxOffset = Index(offset.x, offset.y, offset.z);

            Log.Out($"{idx1 + idxOffset}, {idx2}");
        }

        Log.Out($"memory: {(GC.GetTotalMemory(true) - memoryBefore) / 1_048_000:N0}MB");
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
            Process.Start("CMD.exe", $"/C {System.IO.Path.GetFullPath(filename)}");

    }

    public static void RegionCommand(string[] args)
    {
        string dirname = @"C:\Users\menan\AppData\Roaming\7DaysToDie\GeneratedWorlds\Old Honihebu County\cavemap";

        for (int i = 0; i < 16; i++)
        {
            string filename = $"{dirname}/region_{i}.bin";

            var timer = CaveUtils.StartTimer();
            var region = new CaveRegion(filename);

            Log.Out($"{i}: ChunkCount={region.ChunkCount}, timer={timer.ElapsedMilliseconds}ms");
        }
    }

    public static void ClusterizeCommand(string[] args)
    {
        var timer = CaveUtils.StartTimer();

        var prefabName = "army_camp_01";
        // var path = $"C:/SteamLibrary/steamapps/common/7 Days To Die/Data/Prefabs/RWGTiles/{prefabName}.tts";
        var path = $"C:/SteamLibrary/steamapps/common/7 Days To Die/Data/Prefabs/POIs/{prefabName}.tts";
        var yOffset = -7;

        var clusters = TTSReader.Clusterize(path, yOffset);

        Log.Out($"{clusters.Count} clusters found, timer: {timer.ElapsedMilliseconds}ms");

        var prefabVoxels = TTSReader.ReadUndergroundBlocks(path, yOffset).Select(pos => new Voxell(pos))
            .ToHashSet();

        GenerateObjFile("prefab.obj", prefabVoxels, false);


        var clusterVoxels = clusters
            .Select(cluster => new Voxell(cluster.start, cluster.size, WaveFrontMaterial.DarkGreen) { force = true })
            .ToHashSet();

        GenerateObjFile("clusters.obj", clusterVoxels, false);
    }

    public static void BoundingCommands(string[] args)
    {
        var start = new Vector3i(0, 0, 0);
        var size = new Vector3i(9, 9, 25);
        var bb = new BoundingBox(start, size);
        var voxels = new HashSet<Voxell>();

        // var voxels = bb.IteratePoints().Select(pos => new Voxell(pos, WaveFrontMaterial.LightBlue) { force = true }).ToHashSet();
        var octree = bb.Octree().ToList();
        foreach (var rect in octree)
        {
            voxels.Add(new Voxell(rect.start, rect.size, WaveFrontMaterial.DarkGreen) { force = true });
        }

        Log.Out($"{octree.Count} sub-volumes found.");

        GenerateObjFile("bounds.obj", voxels, false);
    }

    public static void NoiseCommand(string[] args)
    {
        var roomNoise = new CaveNoise(
            seed: 1337,
            octaves: 2,
            frequency: 0.015f,
            threshold: -0.4f,
            invert: true,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.FBm
        );
        // using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        // {
        //     using (Graphics g = Graphics.FromImage(b))
        //     {
        //         g.Clear(BackgroundColor);
        //         DrawNoise(b, roomNoise);
        //     }

        //     b.Save("noise.png", ImageFormat.Png);
        // }

        var width = 200;
        var size = new Vector3i(width, 50, width);
        var voxels = new HashSet<Voxell>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (CaveNoise.pathingNoise.IsTerrain(x, y, z))
                    {
                        voxels.Add(new Voxell(x, y, z));
                    }
                }
            }
        }

        Log.Out($"{voxels.Count} cave blocks generated");

        GenerateObjFile("noise.obj", voxels, false);
    }

    public static void Main(string[] args)
    {
        switch (args[0])
        {
            case "graph":
                GraphCommand(args);
                break;

            case "path":
                PathCommand(args);
                break;

            case "cave":
            case "caves":
                CaveCommand(args);
                break;

            case "prefab":
                PrefabCommand(args);
                break;

            case "rgb":
                HexToRgb(args);
                break;

            case "sphere":
                SphereCommand(args);
                break;

            case "room":
                RoomCommand(args);
                break;

            case "region":
                RegionCommand(args);
                break;

            case "noise":
                NoiseCommand(args);
                break;

            case "cluster":
                ClusterizeCommand(args);
                break;

            case "cellaut":
            case "cell":
                CellularAutomaCommand(args);
                break;

            case "bit":
                BitCommand();
                break;

            case "boundingbox":
            case "bounds":
            case "bb":
                BoundingCommands(args);
                break;

            case "bezier":
                BezierCommand(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }
}
