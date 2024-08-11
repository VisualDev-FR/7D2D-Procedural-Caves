using System.Collections.Generic;
using System.IO;

public class CaveChunkProvider
{
    public string cavemapDir;

    public Dictionary<int, CaveRegion> regions;

    public CaveChunkProvider(string worldName)
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

    public HashSet<CaveBlock> GetCaveChunk(Chunk chunk)
    {
        var chunkPos = GetChunkPos(chunk);
        var regionID = GetRegionID(chunkPos);
        var caveRegion = GetRegion(regionID);

        Log.Out($"chunk=[{chunk.ChunkPos.x}, {chunk.ChunkPos.z}] -> [{chunkPos.x}, {chunkPos.z}]: regionID={regionID}");

        if (caveRegion == null)
        {
            return null;
        }

        return caveRegion.GetChunk(chunkPos);
    }
}
