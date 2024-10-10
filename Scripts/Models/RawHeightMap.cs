using System;
using System.Linq;
using System.Runtime.CompilerServices;
using WorldGenerationEngineFinal;

public class RawHeightMap
{
    private readonly float[] heightMap;

    public readonly int worldSize;

    public RawHeightMap(WorldBuilder worldBuilder)
    {
        heightMap = worldBuilder.HeightMap;
        worldSize = worldBuilder.WorldSize;
    }

    public RawHeightMap(int _worldSize, float defaultHeight = 0)
    {
        worldSize = _worldSize;
        heightMap = Enumerable
            .Repeat(defaultHeight, worldSize * worldSize)
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(Vector3i vector)
    {
        return GetHeight(vector.x, vector.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int z)
    {
        return heightMap[x + z * worldSize];
    }
}