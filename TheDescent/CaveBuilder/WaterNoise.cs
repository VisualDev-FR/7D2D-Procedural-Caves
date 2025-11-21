using System.Collections.Generic;
using WorldGenerationEngineFinal;

public class WaterNoise
{
    private readonly bool noWater;

    private readonly float threshold;

    private readonly FastNoiseLite noise;

    private static readonly Dictionary<WorldBuilder.GenerationSelections, float> thresholds = new Dictionary<WorldBuilder.GenerationSelections, float>(){
        { WorldBuilder.GenerationSelections.Few,     -0.5f }, // ~5%
        { WorldBuilder.GenerationSelections.Default, -0.3f }, // ~15%
        { WorldBuilder.GenerationSelections.Many,    -0.2f }, // ~25%
    };

    public WaterNoise(int seed, WorldBuilder.GenerationSelections waterConfig)
    {
        noise = new FastNoiseLite(seed);
        noise.SetFractalOctaves(1);
        noise.SetFrequency(0.01f);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        thresholds.TryGetValue(waterConfig, out threshold);

        noWater = waterConfig == WorldBuilder.GenerationSelections.None;
    }

    public bool IsWater(int x, int z)
    {
        if (noWater)
        {
            return false;
        }

        return noise.GetNoise(x, z) < threshold;
    }
}