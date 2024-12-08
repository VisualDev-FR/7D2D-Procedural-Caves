using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public class CmdNoise1D : CmdAbstract
{
    public override string[] GetCommands()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute(List<string> args)
    {
        var timer = CaveUtils.StartTimer();

        var rand = new System.Random();
        var curve = new Noise1D(rand, 20, 50, 100);

        timer.Stop();

        using (var b = new Bitmap(100, 100))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(DrawingUtils.BackgroundColor);

                for (int i = 0; i < curve.Count - 1; i++)
                {
                    var p1 = curve.points[i];
                    var p2 = curve.points[i + 1];

                    using (var pen = new Pen(DrawingUtils.TunnelsColor, 1))
                    {
                        g.DrawLine(pen, new PointF(p1.x, 100 - p1.y), new PointF(p2.x, 100 - p2.y));
                    }
                }
            }

            b.Save(@"noise1d.png", ImageFormat.Png);
        }

        Logging.Info($"timer: {timer.ElapsedMilliseconds}ms");
    }

}