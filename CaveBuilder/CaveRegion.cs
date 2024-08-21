using System.Collections.Generic;
using System.IO;

public class CaveRegion
{
    private readonly Dictionary<Vector2s, HashSet<CaveBlock>> CaveChunks;

    public int ChunkCount => CaveChunks.Count;

    public CaveRegion(string filename)
    {
        CaveChunks = new Dictionary<Vector2s, HashSet<CaveBlock>>();

        using (var stream = new FileStream(filename, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var caveBlock = new CaveBlock(reader);

                    if (!CaveChunks.ContainsKey(caveBlock.chunkPos))
                    {
                        CaveChunks[caveBlock.chunkPos] = new HashSet<CaveBlock>();
                    }

                    CaveChunks[caveBlock.chunkPos].Add(caveBlock);
                }
            }
        }
    }

    public HashSet<CaveBlock> GetCaveBlocks(Vector2s chunkPos)
    {
        if (CaveChunks.TryGetValue(chunkPos, out var positions))
        {
            return positions;
        }

        return null;
    }
}
