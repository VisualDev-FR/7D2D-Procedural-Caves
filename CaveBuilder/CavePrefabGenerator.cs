using System;
using System.Collections.Generic;
using System.Linq;

public class CavePrefabGenerator
{
    private const int MAX_SEED = 99999;

    public static List<Vector3i> GenerateRoom(Vector3i position, Vector3i size, int seed = -1)
    {
        if (seed == -1)
            seed = new Random().Next(MAX_SEED);

        var rand = new Random(seed);

        Log.Out($"Seed: {seed}");

        var start = position;
        var end = position + size;

        var noise = new CaveNoise(
            seed: seed,
            octaves: 2,
            frequency: 0.02f,
            threshold: 0f,
            invert: false,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.FBm
        );

        noise.SetFractalLacunarity(rand.Next(5));

        var terrain = new List<Vector3i>();

        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                for (int z = start.z; z <= end.z; z++)
                {
                    float noiseValue = noise.GetNoise(x, y, z);

                    bool isCeilOrFloor = y == start.y || y == end.y;
                    bool isWall = x == start.x || x == end.x || z == start.z || z == end.z;
                    bool isWorm = CaveUtils.FastAbs(noiseValue) > 0.05f;

                    if (isCeilOrFloor || noise.IsTerrain(x, y, z))
                    {
                        terrain.Add(new Vector3i(x, y, z));
                    }
                }
            }
        }

        return terrain;
    }


    public static List<Vector3i> GenerateHeightMap(Vector3i position, Vector3i size, int seed = -1, bool normalized = true, float frequency = 0.1f, bool invert = false)
    {
        if (seed == -1)
            seed = new Random().Next(MAX_SEED);

        var terrain = new List<Vector3i>();
        var start = position;
        var end = position + size;

        var noise = new CaveNoise(
            seed: seed,
            octaves: 1,
            frequency: frequency,
            threshold: 0f,
            invert: false,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.FBm
        );

        for (int x = start.x; x <= end.x; x++)
        {
            for (int z = start.z; z <= end.z; z++)
            {
                float noiseValue = noise.GetNoise(x, z);

                int height = normalized ?
                    (int)(size.y * 0.5f * (1 + noiseValue)) :
                    (int)(size.y * noiseValue);

                int y1 = invert ? end.y : start.y;
                int y2 = invert ?
                    CaveUtils.FastMin(end.y, end.y - height) :
                    CaveUtils.FastMax(start.y, start.y + height);

                for (int y = y1; y != y2; y += Math.Sign(y2 - y1))
                {
                    terrain.Add(new Vector3i(x, y, z));
                }
            }
        }

        return terrain;
    }


    public static List<Vector3i> GenerateRoomV2(Vector3i position, Vector3i size, int seed = -1)
    {
        if (seed == -1)
            seed = new Random().Next(MAX_SEED);

        var rand = new Random(seed);

        Log.Out($"Seed: {seed}");

        var start = position;
        var end = position + size;

        var noise = new CaveNoise(
            seed: seed,
            octaves: 1,
            frequency: 0.1f,
            threshold: 0f,
            invert: false,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.FBm
        );

        noise.SetFractalLacunarity(rand.Next(5));

        var terrain = new HashSet<Vector3i>();
        var height = 10;

        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                for (int z = start.z; z <= end.z; z++)
                {
                    var direction = new Vector3i(0, 0, 0);
                    var pos = new Vector3i(x, y, z);

                    if (x == start.x)
                        direction = new Vector3i(1, 0, 0);

                    if (x == end.x)
                        direction = new Vector3i(-1, 0, 0);

                    if (y == start.y)
                        direction = new Vector3i(0, 1, 0);

                    if (y == end.y)
                        direction = new Vector3i(0, -1, 0);

                    if (z == start.z)
                        direction = new Vector3i(0, 0, 1);

                    if (z == end.z)
                        direction = new Vector3i(0, 0, -1);

                    if (direction == Vector3i.zero)
                        continue;

                    int heightValue = (int)(height * CaveUtils.FastAbs(noise.GetNoise(x, y, z)));

                    for (int i = 0; i < heightValue; i++)
                    {
                        terrain.Add(pos + direction * i);
                    }
                }
            }
        }

        return terrain.ToList();
    }


    public static List<Vector3i> GenerateRoomV3(Vector3i position, Vector3i size, int seed = -1)
    {
        if (seed == -1)
            seed = new Random().Next(MAX_SEED);

        var rand = new Random(seed);

        Log.Out($"Seed: {seed}");

        var start = position;
        var end = position + size;

        var noise = new CaveNoise(
            seed: seed,
            octaves: 2,
            frequency: 0.02f,
            threshold: 0f,
            invert: false,
            noiseType: FastNoiseLite.NoiseType.Perlin,
            fractalType: FastNoiseLite.FractalType.FBm
        );

        noise.SetFractalLacunarity(rand.Next(5));

        var terrain = new List<Vector3i>();

        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                for (int z = start.z; z <= end.z; z++)
                {
                    float noiseValue = noise.GetNoise(x, y, z);

                    if (CaveUtils.FastAbs(noiseValue) < 0.05f)
                    {
                        terrain.Add(new Vector3i(x, y, z));
                    }
                }
            }
        }

        return terrain;
    }

}