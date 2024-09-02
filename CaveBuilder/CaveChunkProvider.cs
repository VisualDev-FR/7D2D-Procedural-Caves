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
        var region_x = chunkPos.x / CaveBuilder.chunkRegionGridSize;
        var region_z = chunkPos.z / CaveBuilder.chunkRegionGridSize;

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

    public bool IsCave(int x, int y, int z)
    {
        return IsCave(new Vector3i(x, y, z));
    }

    public bool IsCave(Vector3i worldPos)
    {
        var worldSize = CaveBuilder.worldSize;
        var chunkPos = World.toChunkXZ(worldPos) + new Vector2i(worldSize / 32, worldSize / 32);
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
        var worldSize = CaveBuilder.worldSize;
        var chunkPos = World.toChunkXZ(worldPosition) + new Vector2i(worldSize / 32, worldSize / 32);

        foreach (var offset in CaveUtils.offsets)
        {
            var neighborChunkPos = new Vector2s(
                (short)(chunkPos.x + offset.x),
                (short)(chunkPos.y + offset.z)
            );

            var blocks = GetCaveBlocks(neighborChunkPos);

            if (blocks == null)
                continue;

            caveBlocks.UnionWith(blocks.Where(block => block.isFloor && block.isFlat && !block.isWater));
        }

        return caveBlocks.ToList();
    }
}
