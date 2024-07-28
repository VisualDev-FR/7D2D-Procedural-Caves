using System.Collections.Generic;
using System.IO;

public class CaveRegion
{
    private Dictionary<Vector2s, List<Vector3bf>> caveChunks;

    public int Count => caveChunks.Count;

    public CaveRegion(string filename)
    {
        caveChunks = new Dictionary<Vector2s, List<Vector3bf>>();

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

                    if (!caveChunks.ContainsKey(chunkPos))
                    {
                        caveChunks[chunkPos] = new List<Vector3bf>();
                    }

                    caveChunks[chunkPos].Add(blockPos);
                }
            }
        }
    }

}
