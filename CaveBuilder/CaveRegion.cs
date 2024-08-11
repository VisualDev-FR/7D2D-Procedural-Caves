using System.Collections.Generic;
using System.IO;

public class CaveRegion
{
    private readonly Dictionary<Vector2s, List<Vector3bf>> CaveChunks;

    public int ChunkCount => CaveChunks.Count;

    public CaveRegion(string filename)
    {
        CaveChunks = new Dictionary<Vector2s, List<Vector3bf>>();

        using (var stream = new FileStream(filename, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    short chunk_x = reader.ReadInt16();
                    short chunk_z = reader.ReadInt16();
                    ushort bitfield = reader.ReadUInt16();

                    var chunkPos = new Vector2s(chunk_x, chunk_z);
                    var blockPos = new Vector3bf(bitfield);

                    if (!CaveChunks.ContainsKey(chunkPos))
                    {
                        CaveChunks[chunkPos] = new List<Vector3bf>();
                    }

                    CaveChunks[chunkPos].Add(blockPos);
                }
            }
        }
    }

    public List<Vector3bf> GetChunk(Chunk chunk)
    {
        var chunkPos = new Vector2s(chunk.ChunkPos);

        if (CaveChunks.TryGetValue(chunkPos, out var positions))
        {
            return positions;
        }

        return null;
    }
}
