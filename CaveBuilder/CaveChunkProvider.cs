using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CaveBlocksProvider
{
    public string cavemapDir;

    public Dictionary<int, CaveRegion> regions;

    public CaveBlocksProvider(string worldName)
    {
        regions = new Dictionary<int, CaveRegion>();
        cavemapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";
    }

    public int GetRegionID(Vector2s chunkPos)
    {
        var region_x = chunkPos.x / CaveBuilder.chunkGridSize;
        var region_z = chunkPos.z / CaveBuilder.chunkGridSize;

        var regionID = region_x + region_z * CaveBuilder.regionGridSize;

        return regionID;
    }

    private CaveRegion CreateCaveRegion(int regionID)
    {
        string filename = $"{cavemapDir}/region_{regionID}.bin";

        if (!File.Exists(filename))
        {
            Log.Warning($"[Cave] cave region not found '{filename}'");
            return null;
        }

        regions[regionID] = new CaveRegion(filename);

        return regions[regionID];
    }

    public CaveRegion GetRegion(int regionID)
    {
        if (regions.TryGetValue(regionID, out var region))
        {
            return region;
        }

        return CreateCaveRegion(regionID);
    }

    public static Vector2s GetChunkPos(Chunk chunk)
    {
        // return new Vector2s(
        //     (pos.x / 16) - CaveBuilder.worldSize / 32,
        //     (pos.z / 16) - CaveBuilder.worldSize / 32
        // );

        return new Vector2s(
            (short)(chunk.ChunkPos.x + CaveBuilder.worldSize / 32),
            (short)(chunk.ChunkPos.z + CaveBuilder.worldSize / 32)
        );
    }

    public HashSet<CaveBlock> GetCaveBlocks(Vector2s chunkPos)
    {
        var regionID = GetRegionID(chunkPos);
        var caveRegion = GetRegion(regionID);

        if (caveRegion == null)
        {
            return null;
        }

        return caveRegion.GetCaveBlocks(chunkPos);
    }

    public HashSet<CaveBlock> GetCaveBlocks(Chunk chunk)
    {
        var chunkPos = GetChunkPos(chunk);
        return GetCaveBlocks(chunkPos);
    }

    public static List<CaveBlock> FilterFloorBlocks(HashSet<CaveBlock> blocks)
    {
        HashSet<Vector3> positions = blocks.Select(block => block.BlockChunkPos.ToVector3()).ToHashSet();
        List<CaveBlock> result = new List<CaveBlock>();

        foreach (var block in blocks)
        {
            var upper = block.BlockChunkPos.ToVector3() + Vector3.up;
            var lower = block.BlockChunkPos.ToVector3() + Vector3.down;

            if (!positions.Contains(lower) && positions.Contains(upper))
            {
                result.Add(block);
            }
        }

        return result;
    }

    public bool IsCave(Vector3i worldPos)
    {
        var worldSize = CaveBuilder.worldSize;
        Vector2i chunkPos = World.toChunkXZ(worldPos) + new Vector2i(worldSize / 32, worldSize / 32);

        var caveBlocks = GetCaveBlocks(new Vector2s(chunkPos));

        if (caveBlocks == null)
        {
            return false;
        }

        var caveBlockPosition = worldPos + new Vector3i(worldSize / 2, 0, worldSize / 2);

        return caveBlocks.Contains(new CaveBlock(caveBlockPosition));
    }

    public List<CaveBlock> GetSpawnPositions(Vector3 worldPosition)
    {
        var caveBlocks = new HashSet<CaveBlock>();
        var chunkPos = new Vector2s(
            (int)worldPosition.x / 16 + CaveBuilder.worldSize / 32,
            (int)worldPosition.z / 16 + CaveBuilder.worldSize / 32
        );

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0)
                    continue;

                var neighborChunkPos = new Vector2s(chunkPos.x + dx, chunkPos.z + dz);
                var blocks = GetCaveBlocks(neighborChunkPos);

                if (blocks == null)
                    continue;

                caveBlocks.UnionWith(FilterFloorBlocks(blocks));
            }
        }

        return caveBlocks.ToList();
    }
}
