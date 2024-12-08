using System.Collections.Generic;

public class CmdPath : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "path" };
    }

    public override void Execute(List<string> args)
    {
        throw new System.NotImplementedException();
        // var timer = CaveUtils.StartTimer();
        // int worldSize = 50;
        // int seed = 133487;
        // var rand = new Random(seed);
        // var heightMap = new RawHeightMap(worldSize, 128);

        // if (args.Length > 1)
        //     worldSize = int.Parse(args[1]);

        // var p1 = new Vector3i(0, 0, 0);
        // var p2 = new Vector3i(worldSize, 150, worldSize);

        // var tunnel = new CaveTunnel();

        // for (int i = 0; i < 1; i++)
        // {
        //     tunnel.FindPath(p1, p2);
        // }

        // Logging.Debug($"path: {tunnel.path.Count}, timer: {timer.ElapsedMilliseconds}ms");

        // if (tunnel.path.Count == 0) return;

        // var voxels = tunnel.path.Select(pos => new Voxell(pos.ToVector3i())).ToHashSet();


        // GenerateObjFile("path.obj", voxels, true);
    }

}