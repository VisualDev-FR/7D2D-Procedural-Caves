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
using System.Threading;


public static class CaveViewer
{
    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color PrefabBoundsColor = Color.Green;

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

    public static void DrawPrefabs(Graphics graph, List<CavePrefab> prefabs)
    {
        // throw new NotImplementedException("obsolete, has to be updated.");
        using (var pen = new Pen(PrefabBoundsColor, 1))
        {
            foreach (var prefab in prefabs)
            {
                graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.Size.x, prefab.Size.z);

                // DrawPoints(b, new HashSet<Vector3i>(prefab.nodes), NodeColor);
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
        CaveBuilder.worldSize = 6144;
        // CaveBuilder.radiationSize = 0;
        // CaveBuilder.radiationZoneMargin = 0;
        // CaveBuilder.PREFAB_COUNT = 1000;

        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : CaveBuilder.PREFAB_COUNT;

        PrefabCache cachedPrefabs = CaveBuilder.GetRandomPrefabs(prefabCounts);
        List<GraphEdge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        var voxels = new HashSet<Voxell>();

        foreach (var prefab in cachedPrefabs.Prefabs)
        {
            voxels.Add(new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen) { force = true });

            foreach (var node in prefab.nodes)
            {
                voxels.Add(new Voxell(node.prefab.position + node.marker.start, node.marker.size, WaveFrontMaterial.Orange) { force = true });
            }
        }

        using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);
                DrawEdges(g, edges);
                DrawPrefabs(g, cachedPrefabs.Prefabs);
            }

            Log.Out($"{edges.Count} Generated edges.");

            b.Save(@"graph.png", ImageFormat.Png);
        }

        // return;
        // int index = 0;

        // using (StreamWriter writer = new StreamWriter("graph.obj"))
        // {
        //     writer.WriteLine("mtllib materials.mtl");

        //     foreach (var voxel in voxels)
        //     {
        //         string strVoxel = voxel.ToWavefront(ref index, voxels);

        //         if (strVoxel != "")
        //             writer.WriteLine(strVoxel);
        //     }

        //     foreach (var tetra in edges)
        //     {
        //         string strTetra = tetra.ToWaveFront(ref index);

        //         writer.WriteLine(strTetra);
        //     }
        // }
    }

    public static void PathCommand(string[] args)
    {
        CaveBuilder.worldSize = 100;
        CaveBuilder.radiationZoneMargin = 0;
        // CaveBuilder.rand = new Random();

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var p1 = new CavePrefab(0)
        {
            position = new Vector3i(20, 5, 20),
            Size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab(1)
        {
            position = new Vector3i(20, 50, CaveBuilder.worldSize - 30),
            Size = new Vector3i(20, 10, 20),
        };

        p1.UpdateMarkers(CaveBuilder.rand);
        p2.UpdateMarkers(CaveBuilder.rand);

        var cachedPrefabs = new PrefabCache();
        cachedPrefabs.AddPrefab(p1);
        cachedPrefabs.AddPrefab(p2);

        var node1 = p1.nodes[1];
        var node2 = p2.nodes[0];

        var edge = new GraphEdge(node1, node2);

        Log.Out($"prefab   {node2.prefab.position}");
        Log.Out($"start    {node2.marker.start}");
        Log.Out($"size     {node2.marker.size}");
        Log.Out($"result   {node2.position}\n");

        var cavemap = new CaveMap();

        var timer = CaveUtils.StartTimer();
        var tunnel = new CaveTunnel(0, edge, cachedPrefabs);

        cavemap.AddTunnel(tunnel);
        cavemap.SetWater(tunnel.localMinimas, cachedPrefabs);

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
        var timer = CaveUtils.StartTimer();
        var voxels = new HashSet<Voxell>();

        for (int i = 1; i < 15; i++)
        {
            int radius = i;
            int pos = i * radius * 3;

            var position = new Vector3i(pos, 20, 20);
            var caveBlock = new CaveBlock(position);
            var sphere = CaveTunnel.GetSphere(caveBlock, radius);

            foreach (var block in sphere)
            {
                voxels.Add(new Voxell(block.x, block.y, block.z));
            }
        }

        Log.Out($"{voxels.Count} voxels generated, timer = {CaveUtils.TimeFormat(timer)}");

        GenerateObjFile("sphere.obj", voxels, false);
    }

    public static void RoomCommand(string[] args)
    {
        const int vecSize = 50;

        var timer = CaveUtils.StartTimer();
        var position = new Vector3i(0, 0, 0);
        var size = new Vector3i(vecSize, 10, vecSize);
        var seed = new Random().Next();
        var terrain = CavePrefabGenerator.GenerateRoomV3(position, size);

        var voxels = terrain.Select((pos) => new Voxell(pos)).ToHashSet();

        voxels.Add(new Voxell(position, size, WaveFrontMaterial.DarkGreen) { force = true });

        Log.Out($"{voxels.Count} voxels generated, timer = {CaveUtils.TimeFormat(timer)}");

        GenerateObjFile("room.obj", voxels, true);
    }

    public static void CaveCommand(string[] args)
    {
        CaveBuilder.worldSize = 1024;

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var timer = CaveUtils.StartTimer();
        var cachedPrefabs = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);

        Log.Out("Start solving graph...");

        List<GraphEdge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        int index = 0;

        long memoryBefore = GC.GetTotalMemory(true);

        object lockObject = new object();

        var cavemap = new CaveMap();
        var localMinimas = new HashSet<CaveBlock>();

        using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(BackgroundColor);

                Parallel.ForEach(edges, edge =>
                {
                    Log.Out($"Cave tunneling: {100.0f * index++ / edges.Count:F0}% ({index} / {edges.Count}) {cavemap.Count:N0}");

                    var tunnel = new CaveTunnel(index, edge, cachedPrefabs);

                    lock (lockObject)
                    {
                        cavemap.AddTunnel(tunnel);

                        foreach (CaveBlock caveBlock in tunnel.blocks)
                        {
                            b.SetPixel(caveBlock.x, caveBlock.z, TunnelsColor);
                        }
                    }
                });

                DrawPrefabs(g, cachedPrefabs.Prefabs);
                b.Save(@"cave.png", ImageFormat.Png);
            }
        }

        // cavemap.SetWater(localMinimas, cachedPrefabs);

        Log.Out($"{cavemap.Count:N0} cave blocks generated ({cavemap.TunnelsCount} unique tunnels), timer={timer.ElapsedMilliseconds:N0}ms, memory={(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576.0:F1}MB.");
        Log.Out($"{localMinimas.Count} local minimas");

        if (CaveBuilder.worldSize > 1024)
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
        if (args[0] == "") { }

        CaveBuilder.rand = new Random();

        var rand = CaveBuilder.rand;
        var prefab = new CavePrefab(0, rand);

        var voxels = new HashSet<Voxell>(){
            new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen){ force = true },
        };

        foreach (var point in prefab.GetMarkerPoints())
        {
            voxels.Add(new Voxell(point, WaveFrontMaterial.Orange));
        }

        GenerateObjFile("prefab.obj", voxels, true);
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
            seed: CaveBuilder.SEED + 13,
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

            case "boundingbox":
            case "bounds":
            case "bb":
                BoundingCommands(args);
                break;

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }
}
