using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

public class CmdCave : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "cave" };
    }

    public override void Execute(List<string> args)
    {
        int worldSize = 4096;
        int seed = 1337;
        int prefabCount = worldSize / 5;

        var timer = ProfilingUtils.StartTimer();
        var prefabs = PrefabLoader.LoadPrefabs().Values.ToList();
        var cachedPrefabs = new CavePrefabManager(worldSize);
        var rand = new Random(seed);
        var heightMap = new RawHeightMap(worldSize, 128);

        cachedPrefabs.AddRandomPrefabs(rand, heightMap, prefabCount, prefabs);

        Logging.Info("Start solving graph...");

        var memoryBefore = GC.GetTotalMemory(true);
        var graph = new Graph(cachedPrefabs.Prefabs, worldSize);
        var index = 0;

        object lockObject = new object();

        var cavemap = new CaveMap(worldSize);
        var localMinimas = new HashSet<CaveBlock>();

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(DrawingUtils.BackgroundColor);

                Parallel.ForEach(graph.Edges, edge =>
                {
                    Logging.Info($"Cave tunneling: {100.0f * index++ / graph.Edges.Count:F0}% ({index} / {graph.Edges.Count})");

                    var tunnel = new CaveTunnel(edge, cachedPrefabs, heightMap, worldSize, seed);

                    lock (lockObject)
                    {
                        cavemap.AddTunnel(tunnel);
                        localMinimas.UnionWith(tunnel.LocalMinimas);

                        foreach (CaveBlock caveBlock in tunnel.blocks)
                        {
                            b.SetPixel(caveBlock.x, caveBlock.z, DrawingUtils.TunnelsColor);
                        }
                    }
                });

                DrawingUtils.DrawPrefabs(b, g, cachedPrefabs.Prefabs);
                b.Save(@"ignore/cave.png", ImageFormat.Png);
            }
        }

        // cavemap.SetWater(cachedPrefabs, localMinimas);

        Logging.Info($"{cavemap.BlocksCount:N0} cave blocks generated ({cavemap.TunnelsCount} unique tunnels), timer={timer.ElapsedMilliseconds:N0}ms, memory={(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576.0:N1}MB.");
        Logging.Info($"{localMinimas.Count} local minimas");

        Logging.Debug($"region offset: {CaveConfig.RegionSizeOffset}");
        cavemap.Save("ignore/cavemap", worldSize);

        if (worldSize > 1024)
            return;

        var voxels = new HashSet<Voxell>();
        var water = new HashSet<Voxell>();

        foreach (var block in cavemap.GetBlocks())
        {
            voxels.Add(new Voxell(block.x, block.y, block.z, block.isWater ? WaveFrontMaterial.LightBlue : WaveFrontMaterial.DarkRed));

            if (block.isWater)
            {
                water.Add(new Voxell(block.x, block.y, block.z, WaveFrontMaterial.LightBlue));
            }
        }

        foreach (var prefab in cachedPrefabs.Prefabs)
        {
            voxels.Add(new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen) { force = true });

            foreach (var marker in prefab.caveMarkers)
            {
                voxels.Add(new Voxell(prefab.position + marker.start, marker.size, WaveFrontMaterial.Orange) { force = true });
            }
        }

        DrawingUtils.GenerateObjFile("ignore/cave.obj", voxels, false);
        DrawingUtils.GenerateObjFile("ignore/caveWater.obj", water, false);
    }

}