using System;
using System.Collections.Generic;

public class CmdPath : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "path" };
    }

    public override void Execute(List<string> args)
    {
        int worldSize = 100;
        int seed = 133487;
        var rand = new Random(seed);
        var heightMap = new RawHeightMap(worldSize, 128);

        if (args.Count > 1)
            worldSize = int.Parse(args[1]);

        var p1 = new CavePrefab(0)
        {
            position = new Vector3i(20, 5, 20),
            Size = new Vector3i(10, 10, 10),
        };

        var p2 = new CavePrefab(1)
        {
            position = new Vector3i(20, 100, worldSize - 30),
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

        Logging.Info($"prefab   {node2.prefab.position}");
        Logging.Info($"start    {node2.marker.start}");
        Logging.Info($"size     {node2.marker.size}");
        Logging.Info($"result   {node2.position}\n");

        SphereManager.InitSpheres(5);

        var timer = CaveUtils.StartTimer();
        var cavemap = new CaveMap(worldSize);
        var tunnel = new CaveTunnel(edge, cachedPrefabs, heightMap, worldSize, seed);

        cavemap.AddTunnel(tunnel);

        Logging.Info($"{p1.position} -> {p2.position} | Astar dist: {tunnel.path.Count}, eucl dist: {CaveUtils.EuclidianDist(p1.position, p2.position)}, timer: {timer.ElapsedMilliseconds}ms");

        var voxels = new HashSet<Voxell>(){
            new Voxell(p1.position, p1.Size, WaveFrontMaterial.DarkGreen) { force = true },
            new Voxell(p2.position, p2.Size, WaveFrontMaterial.DarkGreen) { force = true },
        };

        foreach (var block in tunnel.path)
        {
            voxels.Add(new Voxell(block.x, block.y, block.z, WaveFrontMaterial.DarkRed));
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

        DrawingUtils.GenerateObjFile("ignore/path.obj", voxels, true);
    }

}