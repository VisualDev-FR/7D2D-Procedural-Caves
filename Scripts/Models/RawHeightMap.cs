using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using WorldGenerationEngineFinal;

public class RawHeightMap
{
    private readonly NativeArray<float> heightMap;

    public readonly int worldSize;

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
        if (x < 0 || z < 0 || x >= worldSize || z >= worldSize)
        {
            return 0;
        }

        return heightMap[x + z * worldSize];
    }
}