using System.Collections.Generic;
using System.IO;

public class CaveChunkProvider
{
    public string cavemapDir;

    public static Dictionary<int, CaveRegion> regions;

    public CaveChunkProvider(string worldName)
    {
        cavemapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";
    }

    public int GetRegionID(Chunk chunk)
    {
        var chunk_x = chunk.ChunkPos.x;
        var chunk_z = chunk.ChunkPos.z;

        return chunk_x + chunk_z * CaveBuilder.chunkGridSize;
    }

    private CaveRegion CreateCaveRegion(int regionID)
    {
        string filename = $"{cavemapDir}/region_{regionID}.bin";

        if (!File.Exists(filename))
        {
            Log.Warning("[Cave] cave region not found '{filename}'");
            return null;
        }

        return new CaveRegion(filename);
    }

    public CaveRegion GetRegion(Chunk chunk)
    {
        var regionID = GetRegionID(chunk);

        if (regions.TryGetValue(regionID, out var region))
        {
            return region;
        }

        return CreateCaveRegion(regionID);
    }

    public List<Vector3bf> GetCaveChunk(Chunk chunk)
    {
        var caveRegion = GetRegion(chunk);

        if (caveRegion == null)
            return null;

        return caveRegion.GetChunk(chunk);
    }
}
