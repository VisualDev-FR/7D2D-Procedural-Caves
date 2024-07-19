using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public static class CaveGenerator
{
    public static Dictionary<Vector2s, Vector3bf[]> caveMap;

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
            Log.Warning($"[Cave] Null chunk {chunk.ChunkPos}");
            return;
        }

        var chunkPos = new Vector2s(chunk.ChunkPos);

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

        foreach (Vector3bf pos in blockPositions)
        {
            try
            {
                chunk.SetBlockRaw(pos.x, pos.y, pos.z, caveAirBlock);
                chunk.SetDensity(pos.x, pos.y, pos.z, MarchingCubes.DensityAir);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={chunkPos}, block={pos})");
            }
        }

        Log.Warning($"[Cave] {blockPositions.Length} caveBlock spawned in chunk {chunkPos}");
    }
}