# pragma warning disable CA1416, CA1050, CA2211, IDE0290, IDE0063, IDE0305, IDE0090


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;


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

    public static PointF ParsePointF(Vector3 point)
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
        CaveBuilder.worldSize = 6144;
        // CaveBuilder.radiationSize = 0;
        // CaveBuilder.radiationZoneMargin = 0;
        // CaveBuilder.PREFAB_COUNT = 1000;

        int prefabCounts = args.Length > 1 ? int.Parse(args[1]) : CaveBuilder.PREFAB_COUNT;

        PrefabCache cachedPrefabs = CaveBuilder.GetRandomPrefabs(prefabCounts);
        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

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
        CaveBuilder.worldSize = 200;
        CaveBuilder.radiationZoneMargin = 0;

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var p1 = new CavePrefab(0)
        {
            position = new Vector3i(20, 5, 20),
            Size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab(1)
        {
            position = new Vector3i(20, 5, CaveBuilder.worldSize - 30),
            Size = new Vector3i(20, 10, 20),
        };

        p1.UpdateMarkers(CaveBuilder.rand);
        p2.UpdateMarkers(CaveBuilder.rand);

        var cachedPrefabs = new PrefabCache();
        cachedPrefabs.AddPrefab(p1);
        cachedPrefabs.AddPrefab(p2);

        var timer = new Stopwatch();
        timer.Start();

        var node1 = p1.nodes[1];
        var node2 = p2.nodes[0];

        Log.Out($"prefab   {node2.prefab.position}");
        Log.Out($"start    {node2.marker.start}");
        Log.Out($"size     {node2.marker.size}");
        Log.Out($"result   {node2.position}\n");

        var tunnel = CaveTunneler.GenerateTunnel(node1, node2, cachedPrefabs);

        Log.Out($"{p1.position} -> {p2.position} | Astar dist: {tunnel.Count}, eucl dist: {CaveUtils.EuclidianDist(p1.position, p2.position)}, timer: {timer.ElapsedMilliseconds}ms");

        var voxels = new HashSet<Voxell>(){
            new Voxell(p1.position, p1.Size, WaveFrontMaterial.DarkGreen) { force = true },
            new Voxell(p2.position, p2.Size, WaveFrontMaterial.DarkGreen) { force = true },
        };

        foreach (var block in tunnel)
        {
            if (block.isWater)
            {
                voxels.Add(new Voxell(block.position, WaveFrontMaterial.LightBlue) { force = true });
            }
            else
            {
                voxels.Add(new Voxell(block.position, WaveFrontMaterial.DarkRed));

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

        GenerateObjFile("path.obj", voxels, false);
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
            var sphere = CaveTunneler.GetSphere(caveBlock, radius);

            foreach (var block in sphere)
            {
                voxels.Add(new Voxell(block.position));
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
        var terrain = CavePrefabGenerator.GenerateTunnels(position, size);

        var voxels = terrain.Select((pos) => new Voxell(pos)).ToHashSet();

        voxels.Add(new Voxell(position, size, WaveFrontMaterial.DarkGreen) { force = true });

        Log.Out($"{voxels.Count} voxels generated, timer = {CaveUtils.TimeFormat(timer)}");

        GenerateObjFile("room.obj", voxels, false);
    }

    public static void CaveCommand(string[] args)
    {
        CaveBuilder.worldSize = 1024;

        if (args.Length > 1)
            CaveBuilder.worldSize = int.Parse(args[1]);

        var timer = CaveUtils.StartTimer();
        var cachedPrefabs = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);

        Log.Out("Start solving graph...");

        List<Edge> edges = Graph.Resolve(cachedPrefabs.Prefabs);

        int caveBlockCount = 0;
        int index = 0;

        long memoryBefore = GC.GetTotalMemory(true);

        object lockObject = new object();

        var cavemap = new HashSet<Vector3i>();

        using (var multistream = new MultiStream("cavemap", create: true))
        {
            using (var b = new Bitmap(CaveBuilder.worldSize, CaveBuilder.worldSize))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(BackgroundColor);

                    Parallel.ForEach(edges, edge =>
                    {
                        var p1 = edge.node1;
                        var p2 = edge.node2;

                        Log.Out($"Cave tunneling: {100.0f * ++index / edges.Count:F0}% ({index} / {edges.Count}) {caveBlockCount:N0}");

                        var tunnel = CaveTunneler.GenerateTunnel(p1, p2, cachedPrefabs);

                        foreach (CaveBlock caveBlock in tunnel)
                        {
                            var position = caveBlock.position;

                            int region_x = position.x / CaveBuilder.RegionSize;
                            int region_z = position.z / CaveBuilder.RegionSize;
                            int regionID = region_x + region_z * CaveBuilder.regionGridSize;

                            lock (lockObject)
                            {
                                cavemap.Add(position);
                                b.SetPixel(position.x, position.z, TunnelsColor);
                                var writer = multistream.GetWriter($"region_{regionID}.bin");
                                caveBlock.ToBinaryStream(writer);
                                caveBlockCount++;
                            }
                        }
                    });

                    DrawPrefabs(g, cachedPrefabs.Prefabs);
                    b.Save(@"cave.png", ImageFormat.Png);
                }
            }
        }

        Console.WriteLine($"{caveBlockCount:N0} cave blocks generated, timer={CaveUtils.TimeFormat(timer)}, memory={(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576.0:F1}MB.");

        if (CaveBuilder.worldSize > 1024)
            return;

        var voxels = cavemap.Select((pos) => new Voxell(pos, WaveFrontMaterial.DarkRed)).ToHashSet();

        foreach (var prefab in cachedPrefabs.Prefabs)
        {
            voxels.Add(new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen) { force = true });

            foreach (var marker in prefab.markers)
            {
                voxels.Add(new Voxell(prefab.position + marker.start, marker.size, WaveFrontMaterial.Orange) { force = true });
            }
        }


        GenerateObjFile("cave.obj", voxels, false);
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

        foreach (var points in prefab.GetMarkerPoints())
        {
            // Log.Out(points.Count.ToString());
            foreach (var point in points)
            {
                voxels.Add(new Voxell(point, WaveFrontMaterial.Orange));
            }
            // break;
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
            Process.Start("CMD.exe", $"/C {Path.GetFullPath(filename)}");

    }

    public static void Main(string[] args)
    {
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

            default:
                Console.WriteLine($"Invalid command: {args[0]}");
                break;
        }
    }
}
