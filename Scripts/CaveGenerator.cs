using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public static class CaveGenerator
{
    public static Dictionary<Vector3i, List<Vector3i>> caveMap;

    private static BlockValue caveAirBlock = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    public static void LoadCaveMap(string worldName)
    {
        string filename = GameIO.GetWorldDir(worldName) + "/cavemap.csv";

        caveMap = CaveBuilder.ReadCaveMap(filename);
    }

    public static bool IsInsideChunk(Vector3i position, Vector3i chunkPos)
    {
        if (position.x < chunkPos.x)
            return false;

        if (position.x > chunkPos.x + 15)
            return false;

        if (position.z < chunkPos.z)
            return false;

        if (position.z > chunkPos.z + 15)
            return false;

        return true;
    }

    public static void GenerateCave(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Out($"[Cave] Null chunk {chunk.ChunkPos}");
            return;
        }

        int worldSize = GameManager.Instance.World.ChunkCache.ChunkProvider.GetWorldSize().x;
        int chunkWorldSize = worldSize / 16;
        var chunkPos = chunk.ChunkPos + new Vector3i(chunkWorldSize / 2, 0, chunkWorldSize / 2);
        var chunkWorldPos = chunk.GetWorldPos();
        var HalfWorldSize = new Vector3i(worldSize / 2, 0, worldSize / 2);

        // var playerPos = new Vector3i(GameManager.Instance.World.GetPrimaryPlayer().position);
        // if (!IsInsideChunk(playerPos, chunk.GetWorldPos()))
        // {
        //     return;
        // }
        // Log.Out($"[Cave] playerPos: {GameManager.Instance.World.GetPrimaryPlayer().position}");
        // Log.Out($"[Cave] chunkWorld: {chunk.GetWorldPos()}");
        // Log.Out($"[Cave] chunkWorldSize: {chunkWorldSize}");
        // Log.Out($"[Cave] chunk.ChunkPos: {chunk.ChunkPos}");
        // Log.Out($"[Cave] ChunkPos: {chunkPos}");

        if (!caveMap.TryGetValue(chunkPos, out var blockPositions))
        {
            // Log.Warning($"[Cave] chunk {chunkPos} not found in cavemap.");
            return;
        }

        Log.Warning($"[Cave] {blockPositions.Count} caveBlock spawned in chunk {chunkPos}");

        foreach (var block in blockPositions)
        {
            var caveBlock = block - chunkWorldPos - HalfWorldSize;

            var neighbors = new List<Vector3i>()
            {
                new Vector3i(caveBlock.x, caveBlock.y, caveBlock.z),
                new Vector3i(caveBlock.x + 1, caveBlock.y, caveBlock.z),
                new Vector3i(caveBlock.x - 1, caveBlock.y, caveBlock.z),
                new Vector3i(caveBlock.x, caveBlock.y + 1, caveBlock.z),
                new Vector3i(caveBlock.x, caveBlock.y + 2, caveBlock.z),
                new Vector3i(caveBlock.x, caveBlock.y + 3, caveBlock.z),
                new Vector3i(caveBlock.x, caveBlock.y, caveBlock.z + 1),
                new Vector3i(caveBlock.x, caveBlock.y, caveBlock.z - 1),
            };

            foreach (var pos in neighbors)
            {
                try
                {
                    chunk.SetBlockRaw(pos.x, pos.y, pos.z, caveAirBlock);
                    chunk.SetDensity(pos.x, pos.y, pos.z, MarchingCubes.DensityAir);
                }
                catch
                {
                    // Log.Error($"[Cave] (Chunk={chunkPos}, block={caveBlock}) {e}");
                }
            }
        }
    }
}