using System.Collections.Generic;
using System.Linq;

public class CmdBezier : CmdAbstract
{

    public override string[] GetCommands()
    {
        return new string[] { "bezier" };
    }

    public override void Execute(List<string> args)
    {
        var timer = CaveUtils.StartTimer();

        var P0 = new Vector3i(0, 0, 0);
        var P1 = new Vector3i(20, 40, 20);
        var P2 = new Vector3i(40, 60, 20);
        var P3 = new Vector3i(60, 0, 0);

        var voxells = new BezierCurve3D()
            .GetPoints(256, P0, P1, P2, P3)
            .Select(pos => new Voxell(pos))
            .ToHashSet();

        Logging.Info($"blocks: {voxells.Count}, timer: {timer.ElapsedMilliseconds}ms");

        DrawingUtils.GenerateObjFile("bezier.obj", voxells, true);
    }

}