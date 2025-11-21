using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;


public class CmdGraph : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "graph" };
    }

    public override void Execute(List<string> args)
    {
        // var prefabs = PrefabLoader.LoadPrefabs().Values.ToList();

        Random random = new Random();

        int worldSize = 1024 * 2;
        int prefabCounts = worldSize / 5;
        int gridSize = 150;
        int minMarkers = 2;
        int maxMarkers = 6;

        var prefabManager = new CavePrefabManager(worldSize);
        var heightMap = new RawHeightMap(worldSize, 128);

        prefabManager.SetupBoundaryPrefabs(random, gridSize);
        prefabManager.AddRandomPrefabs(random, heightMap, prefabCounts, minMarkers, maxMarkers);

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
                g.Clear(DrawingUtils.BackgroundColor);
                DrawingUtils.DrawGrid(b, g, worldSize, gridSize);
                DrawingUtils.DrawEdges(g, graph.Edges.ToList());
                DrawingUtils.DrawPrefabs(b, g, prefabManager.Prefabs);
            }

            b.Save(@"ignore/graph.png", ImageFormat.Png);
        }
    }


}