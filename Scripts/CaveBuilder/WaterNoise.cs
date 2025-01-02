using System.Collections.Generic;

public class WaterNoise
{
    private readonly bool fullWater;

    private readonly bool noWater;

    private readonly float threshold;

    private readonly FastNoiseLite noise;

    private static readonly Dictionary<CaveConfig.WaterConfig, float> thresholds = new Dictionary<CaveConfig.WaterConfig, float>(){
        { CaveConfig.WaterConfig.LOW,    -0.5f }, // ~5%
        { CaveConfig.WaterConfig.MEDIUM, -0.3f }, // ~15%
        { CaveConfig.WaterConfig.HIGH,   -0.2f }, // ~25%
    };

    public WaterNoise(int seed, CaveConfig.WaterConfig waterConfig)
    {
        noise = new FastNoiseLite(seed);
        noise.SetFractalOctaves(1);
        noise.SetFrequency(0.01f);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        thresholds.TryGetValue(waterConfig, out threshold);

        fullWater = waterConfig == CaveConfig.WaterConfig.FULL;
        noWater = waterConfig == CaveConfig.WaterConfig.NONE;
    }

    public bool IsWater(int x, int z)
    {
        if (noWater)
        {
            return false;
        }

        if (fullWater)
        {
            return true;
        }

        return noise.GetNoise(x, z) < threshold;
    }
}