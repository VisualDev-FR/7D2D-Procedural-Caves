using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Random = System.Random;

public class CaveChunksProvider
{
    public string cavemapDir;

    public CaveGraph caveGraph;

    public Dictionary<int, CaveRegion> regions;

    public int worldSize;

    public CaveChunksProvider(string worldName, int worldSize)
    {
        this.worldSize = worldSize;

        regions = new Dictionary<int, CaveRegion>();
        cavemapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";
    }

    public int GetRegionID(Vector2s chunkPos)
    {
        int chunkRegionGridSize = CaveConfig.RegionSize >> 4;
        int regionGridSize = worldSize / CaveConfig.RegionSize;

        int region_x = chunkPos.x / chunkRegionGridSize;
        int region_z = chunkPos.z / chunkRegionGridSize;

        int regionID = region_x + region_z * regionGridSize;

        return regionID;
    }

    public int HashCodeFromWorldPos(int x, int y, int z)
    {
        int halfWorldSize = worldSize / 2;

        return CaveBlock.GetHashCode(
            x + halfWorldSize,
            y,
            z + halfWorldSize
        );
    }

    public Vector2s GetChunkPos(Chunk chunk)
    {
        return new Vector2s(
            (short)(chunk.ChunkPos.x + (worldSize >> 5)),
            (short)(chunk.ChunkPos.z + (worldSize >> 5))
        );
    }

    public Vector2s GetChunkPos(Vector3 worldPos)
    {
        return GetChunkPos(
            (short)worldPos.x,
            (short)worldPos.z
        );
    }

    public Vector2s GetChunkPos(short worldX, short worldZ)
    {
        return new Vector2s(
            (short)((worldX >> 4) + (worldSize >> 5)),
            (short)((worldZ >> 4) + (worldSize >> 5))
        );
    }

    private CaveRegion CreateCaveRegion(int regionID)
    {
        string filename = $"{cavemapDir}/region_{regionID}.bin";

        if (!File.Exists(filename))
        {
            Log.Warning($"[Cave] cave region not found 'region_{regionID}'");
            return null;
        }

        regions[regionID] = new CaveRegion(filename);

        return regions[regionID];
    }

    public CaveRegion GetRegion(Vector2s chunkPos)
    {
        int regionID = GetRegionID(chunkPos);

        if (regions.TryGetValue(regionID, out var region))
        {
            return region;
        }

        return CreateCaveRegion(regionID);
    }

    public CaveChunk GetCaveChunk(short worldX, short worldZ)
    {
        var chunkPos = GetChunkPos(worldX, worldZ);
        var caveRegion = GetRegion(chunkPos);

        return caveRegion?.GetCaveChunk(chunkPos);
    }

    public HashSet<CaveBlock> GetCaveBlocks(Vector2s chunkPos)
    {
        var caveRegion = GetRegion(chunkPos);

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

    public bool IsCave(int worldX, int worldY, int worldZ)
    {
        var caveChunk = GetCaveChunk((short)worldX, (short)worldZ);
        var hashcode = HashCodeFromWorldPos(worldX, worldY, worldZ);

        if (caveChunk == null)
            return false;

        return caveChunk.Exists(hashcode);
    }

}
