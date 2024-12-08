using System.Collections.Generic;
using System.Linq;

public class CmdClusterize : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "cluster" };
    }

    public override void Execute(List<string> args)
    {
        var timer = CaveUtils.StartTimer();

        var prefabName = "army_camp_01";
        var path = $"C:/SteamLibrary/steamapps/common/7 Days To Die/Data/Prefabs/POIs/{prefabName}.tts";
        var yOffset = -7;

        var clusters = BlockClusterizer.Clusterize(path, yOffset);

        Logging.Info($"{clusters.Count} clusters found, timer: {timer.ElapsedMilliseconds}ms");

        var prefabVoxels = TTSReader.ReadUndergroundBlocks(path, yOffset).Select(pos => new Voxell(pos))
            .ToHashSet();

        DrawingUtils.GenerateObjFile("prefab.obj", prefabVoxels, false);


        var clusterVoxels = clusters
            .Select(cluster => new Voxell(cluster.start, cluster.size, WaveFrontMaterial.DarkGreen) { force = true })
            .ToHashSet();

        DrawingUtils.GenerateObjFile("clusters.obj", clusterVoxels, false);
    }

}