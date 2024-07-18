using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public static class CaveGenerator
{
    public static HashSet<CaveBlock> caveMap;

    private static BlockValue caveAirBlock = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    public static void LoadCaveMap(string worldName)
    {
        string filePath = GameIO.GetWorldDir(worldName) + "/cavemap.csv";

        caveMap = new HashSet<CaveBlock>();

        using (var reader = new StreamReader(filePath))
        {
            string strPosition;
            while ((strPosition = reader.ReadLine()) != null)
            {
                var coords = strPosition.Split(",");

                short x = short.Parse(coords[0]);
                short y = short.Parse(coords[1]);
                short z = short.Parse(coords[2]);

                caveMap.Add(new CaveBlock(x, y, z, 0b0));
            }
        }

        Log.Out($"[Cave] {caveMap.Count} caveBlocks Loaded.");
    }

    public static bool IsBlockInsideChunk(CaveBlock caveBlock, Vector3i chunkPos)
    {
        if (caveBlock.x < chunkPos.x)
            return false;

        if (caveBlock.x > chunkPos.x + 15)
            return false;

        if (caveBlock.z < chunkPos.z)
            return false;

        if (caveBlock.z > chunkPos.z + 15)
            return false;

        return true;
    }

    public static List<Vector3i> GetCaveBlocks(Vector3i chunkPos)
    {
        var result =
            from pos in caveMap
            where IsBlockInsideChunk(pos, chunkPos)
            select pos - chunkPos;

        return result.ToList();
    }

    public static void GenerateCave(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Out($"[Cave] Null chunk {chunk.ChunkPos}");
            return;
        }

        var chunkPos = chunk.GetWorldPos();
        var blockPositions = GetCaveBlocks(chunkPos);

        Log.Out($"[Cave] {blockPositions.Count} caveBlock spawned in chunk {chunkPos}");

        foreach (var pos in blockPositions)
        {
            try
            {
                // var position = pos + CavePlanner.HalfWorldSize;

                chunk.SetBlockRaw(pos.x, pos.y, pos.z, caveAirBlock);
                chunk.SetDensity(pos.x, pos.y, pos.z, MarchingCubes.DensityAir);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] (Chunk={chunkPos}, block={pos}) {e}");
            }
        }
    }
}