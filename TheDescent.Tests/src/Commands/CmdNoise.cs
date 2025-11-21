using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using WorldGenerationEngineFinal;

public class CmdNoise : CmdAbstract
{

    public override string[] GetCommands()
    {
        return new string[] { "noise" };
    }

    public override void Execute(List<string> args)
    {
        var waterNoise = new WaterNoise(1337, WorldBuilder.GenerationSelections.Few);
        var worldSize = 4096;

        int count = 0;
        int sqrSize = worldSize * worldSize;

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Black);

                for (int x = 0; x < worldSize; x++)
                {
                    for (int y = 0; y < worldSize; y++)
                    {
                        if (waterNoise.IsWater(x, y))
                        {
                            b.SetPixel(x, y, Color.LightBlue);
                            count++;
                        }
                    }
                }

                Logging.Info($"{100f * count / sqrSize:F1}% ({count:N0} / {sqrSize:N0})");
                b.Save("ignore/noise.png", ImageFormat.Png);
            }
        }

    }

}