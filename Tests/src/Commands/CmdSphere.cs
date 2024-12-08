using System.Collections.Generic;
using System.Linq;

public class CmdSphere : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "sphere" };
    }

    public override void Execute(List<string> args)
    {
        var voxels = new HashSet<Voxell>();

        for (int i = CaveConfig.minTunnelRadius; i <= CaveConfig.maxTunnelRadius; i++)
        {
            int radius = i;
            int pos = i * radius * 3;

            var timer = CaveUtils.StartTimer();
            var position = new Vector3i(pos, 20, 20);
            var caveBlock = new CaveBlock(position);
            var sphere = SphereManager.GetSphere(caveBlock.ToVector3i(), radius);

            Logging.Info($"radius: {radius}, blocks: {sphere.ToList().Count}, timer: {timer.ElapsedMilliseconds} ms");

            foreach (var block in sphere)
            {
                voxels.Add(new Voxell(block.x, block.y, block.z));
            }
        }

        DrawingUtils.GenerateObjFile("sphere.obj", voxels, false);
    }

}