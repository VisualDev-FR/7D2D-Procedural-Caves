public class CaveNoise
{
    public FastNoiseLite noise;

    public float threshold;

    public int seed;

    public static CaveNoise defaultNoise = new CaveNoise(
        seed: CaveBuilder.SEED,
        octaves: 1,
        frequency: 0.15f,
        threshold: -0.2f,
        invert: true,
        noiseType: FastNoiseLite.NoiseType.Perlin,
        fractalType: FastNoiseLite.FractalType.None
    );

    public CaveNoise(int seed, int octaves, float frequency, float threshold, bool invert, FastNoiseLite.NoiseType noiseType, FastNoiseLite.FractalType fractalType)
    {
        this.seed = seed;
        this.threshold = threshold;

        if (invert) this.threshold *= -1;

        noise = new FastNoiseLite(seed != -1 ? seed : CaveBuilder.SEED);
        noise.SetFractalType(fractalType);
        noise.SetNoiseType(noiseType);
        noise.SetFractalOctaves(octaves);
        noise.SetFrequency(frequency);
    }

    public void SetFractalLacunarity(float value)
    {
        noise.SetFractalLacunarity(value);
    }

    public void SetSeed(int seed)
    {
        noise.SetSeed(seed);
    }

    public bool IsTerrain(int x, int y, int z)
    {
        return noise.GetNoise(x, y, z) < threshold;
    }

    public bool IsTerrain(int x, int z)
    {
        return noise.GetNoise(x, z) < threshold;
    }

    public bool IsCave(Vector3i pos)
    {
        return noise.GetNoise(pos.x, pos.y, pos.z) > threshold;
    }

    public bool IsCave(int x, int y, int z)
    {
        return noise.GetNoise(x, y, z) > threshold;
    }

    public bool IsCave(int x, int z)
    {
        return noise.GetNoise(x, z) > threshold;
    }

    public float GetNoise(int x, int y, int z)
    {
        return noise.GetNoise(x, y, z);
    }

    public float GetNoise(int x, int z)
    {
        return noise.GetNoise(x, z);
    }
}

