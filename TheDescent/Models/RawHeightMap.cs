using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using WorldGenerationEngineFinal;

public class RawHeightMap
{
    private readonly NativeArray<float> heightMap;

    public readonly int worldSize;

    public float MinHeight => heightMap.Min();

    public float MaxHeight => heightMap.Max();

    public RawHeightMap(string dtmPath, int worldSize)
    {
        this.worldSize = worldSize;
        this.heightMap = new NativeArray<float>(worldSize * worldSize, Allocator.Persistent);

        var values = HeightMapUtils.LoadRAWToHeightData(dtmPath);

        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                heightMap[x + z * worldSize] = values[x, z];
            }
        }
    }

    public RawHeightMap(WorldBuilder worldBuilder)
    {
        heightMap = worldBuilder.data.HeightMap;
        worldSize = worldBuilder.WorldSize;
    }

    public RawHeightMap(int _worldSize, float defaultHeight = 0)
    {
        worldSize = _worldSize;
        heightMap = new NativeArray<float>(worldSize * worldSize, Allocator.Persistent);

        for (int i = 0; i < heightMap.Length; i++)
        {
            heightMap[i] = defaultHeight;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(Vector3i vector)
    {
        return GetHeight(vector.x, vector.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int z)
    {
        int index = x + z * worldSize;

        if (index >= 0 && index < heightMap.Length)
        {
            return heightMap[index];
        }

        Logging.Warning($"worldsize: {worldSize}, pos: [{x}, {z}]");
        return 0;
    }

    public static void test_heightmap()
    {
        var rawPath = @"C:\tools\DEV\7DaysToDie-modding\7D2D-Procedural-caves\Tests\ignore\heightMap.raw";
        var csvPath = @"C:\tools\DEV\7DaysToDie-modding\7D2D-Procedural-caves\Tests\ignore\heightMap.csv";

        var size = 1024;
        var random = new Random();
        var heightMap = new float[size * size].Select(value => 10f + (float)random.NextDouble() * 245f).ToArray();

        using (var stream = new FileStream(rawPath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            HeightMapUtils.SaveHeightMapRAW(stream, heightMap, -1f);
        }

        var rawHeightMap = new RawHeightMap(rawPath, size);

        using (var stream = new StreamWriter(csvPath))
        {
            stream.WriteLine("valid;value1;value2;delta");

            for (int i = 0; i < size * size; i++)
            {
                var value1 = heightMap[i];
                var value2 = rawHeightMap.heightMap[i];
                var delta = Math.Abs(value1 - value2);
                var valid = delta > 1 ? 0 : 1;

                stream.WriteLine($"{valid};{value1};{value2};{delta}");
            }
        }

        Log.Out("heightmap done");
    }
}