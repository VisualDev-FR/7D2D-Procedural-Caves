using System;
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
        /* before:
            [Cave] radius: 25, count: 65117
            [Cave] timer: 3588ms, memory: 5,1MB
        */
        var timer = CaveUtils.StartTimer();
        var voxels = new HashSet<Voxell>();
        var memoryBefore = GC.GetTotalMemory(true);

        SphereManager.InitSpheres(10);

        Logging.Debug("------------------------");
        Logging.Debug($"timer: {timer.ElapsedMilliseconds}ms, memory: {CaveUtils.TotalMemoryKB(memoryBefore)}");

        for (int i = CaveConfig.minTunnelRadius; i <= CaveConfig.maxTunnelRadius; i++)
        {
            int radius = i;
            int pos = i * radius * 3;

            var position = new Vector3i(pos, 20, 20);
            var sphere = SphereManager.GetSphereLRE(position, radius);

            Logging.Info($"radius: {radius}, blocks: {sphere.ToList().Count}, timer: {timer.ElapsedMilliseconds} ms");

            foreach (var layer in sphere)
            {
                for (int y = layer.yMin; y <= layer.yMax; y++)
                {
                    voxels.Add(new Voxell(layer.x, y, layer.z));
                }
            }
        }

        DrawingUtils.GenerateObjFile("sphere.obj", voxels, false);
    }

}