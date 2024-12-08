using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public class CmdNoise : CmdAbstract
{

    public enum CaveWater
    {
        NONE,
        LOW,
        MEDIUM,
        HIGH,
        FULL,
    }

    private static Dictionary<CaveWater, float> waterconfig = new Dictionary<CaveWater, float>(){
        {CaveWater.LOW, -0.5f},    // 5%
        {CaveWater.MEDIUM, -0.3f}, // 15%
        {CaveWater.HIGH, -0.2f},  // 25%
    };

    public override string[] GetCommands()
    {
        return new string[] { "noise" };
    }

    public override void Execute(List<string> args)
    {

        var waterNoise = new CaveNoise(
            seed: 1337,
            octaves: 1,
            frequency: 0.01f,
            threshold: waterconfig[CaveWater.HIGH],
            invert: true,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.None
        );

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
                        if (waterNoise.IsCave(x, y))
                        {
                            b.SetPixel(x, y, Color.LightBlue);
                            count++;
                        }
                    }
                }

                Logging.Info($"{100f * count / sqrSize:F1}% ({count:N0} / {sqrSize:N0})");
                b.Save("noise.png", ImageFormat.Png);
            }
        }

    }

}