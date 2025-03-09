using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CaveChunksProvider
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<CaveChunksProvider>();

    private readonly Dictionary<int, CaveRegion> regions = new Dictionary<int, CaveRegion>();

    private readonly HashSet<CaveBlock> blocksToSave = new HashSet<CaveBlock>();

    private readonly Queue<int> regionQueue = new Queue<int>();

    private static readonly int maxQueueSize = 4;

    public readonly string cavemapDir;

    public readonly string cavemapSaveDir;

    private readonly int worldSize;

    public CaveChunksProvider(int worldSize)
    {
        this.worldSize = worldSize;

        cavemapDir = $"{GameIO.GetWorldDir()}/cavemap";
        cavemapSaveDir = $"{GameIO.GetSaveGameDir()}/cavemap";

        if (!Directory.Exists(cavemapDir))
            logger.Warning($"Cavemap not found at '{cavemapDir}'");

        if (!Directory.Exists(cavemapSaveDir))
            Directory.CreateDirectory(cavemapSaveDir);
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
        regions[regionID] = new CaveRegion(regionID);
        regions[regionID].TryRead($"{cavemapDir}/region_{regionID}.bin");
        regions[regionID].TryRead($"{cavemapSaveDir}/region_{regionID}.bin");

        regionQueue.Enqueue(regionID);

        logger.Info($"Enqueue region '{regionID}'");

        if (regionQueue.Count > maxQueueSize)
        {
            int dequeuedID = regionQueue.Dequeue();
            regions.Remove(dequeuedID);

            logger.Info($"Dequeue region '{dequeuedID}'");
        }

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

    public void RegisterAsCaveBlock(Vector3i position)
    {
        blocksToSave.Add(new CaveBlock(position));

        if (blocksToSave.Count > 1000)
        {
            SaveBlocks();
        }
    }

    public void SaveBlocks()
    {
        if (blocksToSave.Count == 0)
            return;

        var groupedBlocks = blocksToSave.GroupBy(block => GetRegionID(block.chunkPos));

        using (var multistream = new MultiStream(cavemapSaveDir, create: true))
        {
            foreach (var group in groupedBlocks)
            {
                var regionID = group.Key;
                var writer = multistream.GetWriter($"region_{regionID}.bin", FileMode.Append);

                foreach (CaveBlock caveblock in group)
                {
                    writer.Write(caveblock.x);
                    writer.Write(caveblock.y);
                    writer.Write(caveblock.z);
                }
            }
        }

        blocksToSave.Clear();

        logger.Info("{n} cave regions saved.");
    }
}
