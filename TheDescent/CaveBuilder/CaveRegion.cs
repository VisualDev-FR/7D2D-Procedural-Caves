using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CaveRegion
{
    private readonly Dictionary<Vector2s, CaveChunk> CaveChunks;

    public int ChunkCount => CaveChunks.Count;

    public int BlockCount => CaveChunks.Values.Sum(chunk => chunk.BlockCount);

    public CaveRegion(string filename)
    {
        CaveChunks = new Dictionary<Vector2s, CaveChunk>();

        var layer = new RLELayer();
        var chunkPos = new Vector2s();

        using (var stream = new FileStream(filename, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var x = reader.ReadInt32();
                    var z = reader.ReadInt32();
                    var layerCount = reader.ReadInt32();

                    chunkPos.x = (short)(x >> 4);
                    chunkPos.z = (short)(z >> 4);

                    if (!CaveChunks.ContainsKey(chunkPos))
                    {
                        CaveChunks[chunkPos] = new CaveChunk();
                    }

                    var caveChunk = CaveChunks[chunkPos];

                    for (int i = 0; i < layerCount; i++)
                    {
                        layer.rawData = reader.ReadInt32();

                        for (int y = layer.Start; y <= layer.End; y++)
                        {
                            caveChunk.AddBlock(new CaveBlock(x, y, z) { rawData = layer.BlockRawData });
                        }
                    }
                }
            }
        }
    }

    public HashSet<CaveBlock> GetCaveBlocks(Vector2s chunkPos)
    {
        if (CaveChunks.TryGetValue(chunkPos, out var caveChunk))
        {
            return caveChunk.GetBlocks();
        }

        return null;
    }

    public CaveChunk GetCaveChunk(Vector2s chunkPos)
    {
        if (CaveChunks.TryGetValue(chunkPos, out var caveChunk))
        {
            return caveChunk;
        }

        return null;
    }
}
