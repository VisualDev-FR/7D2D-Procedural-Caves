using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class MultiStream : IDisposable
{
    private string path;

    private Dictionary<string, BinaryWriter> writers;

    private Dictionary<string, FileStream> streams;

    public MultiStream(string path, bool create = false)
    {
        if (!Directory.Exists(path))
        {
            if (!create) throw new DirectoryNotFoundException($"'{path}'");

            Directory.CreateDirectory(path);
        }

        this.path = path;
        writers = new Dictionary<string, BinaryWriter>();
        streams = new Dictionary<string, FileStream>();
    }

    private BinaryWriter CreateWriter(string name)
    {
        if (writers.ContainsKey(name))
            throw new InvalidOperationException($"A similar writer is Already open: '{name}'");

        var stream = new FileStream($"{path}/{name}", FileMode.Create);
        var writer = new BinaryWriter(stream);

        streams[name] = stream;
        writers[name] = writer;

        return writer;
    }

    public BinaryWriter GetWriter(string name)
    {
        if (writers.TryGetValue(name, out var writer))
        {
            return writer;
        }

        return CreateWriter(name);
    }

    public void Dispose()
    {
        foreach (var writer in writers.Values)
        {
            writer.Dispose();
        }

        foreach (var stream in streams.Values)
        {
            stream.Dispose();
        }
    }

    public static void BinaryWriterDemo(string[] args)
    {
        CaveBuilder.worldSize = 6144;
        CaveBuilder.RegionSize = 1024;

        var count = 30_000_000;
        var chunkDensity = count / ((CaveBuilder.worldSize / 16) * (CaveBuilder.worldSize / 16));

        Log.Out($"chunkDensity = {chunkDensity}");

        var positions = (
            from i in Enumerable.Range(0, count + 1)
            select new Vector3i(
                CaveBuilder.rand.Next(0, CaveBuilder.worldSize),
                CaveBuilder.rand.Next(0, 255),
                CaveBuilder.rand.Next(0, CaveBuilder.worldSize)
            )).ToList();

        var timer = CaveUtils.StartTimer();

        SaveBinaryCaveMap(positions);
        Log.Out($"{count:N0} caveblocks saved, timer: {CaveUtils.TimeFormat(timer)}");

        timer = CaveUtils.StartTimer();
        var region = new CaveRegion("region/region_0.bin");
        Log.Out($"{region.Count:N0} chunks loaded, timer: {CaveUtils.TimeFormat(timer)}");
    }

    public static void SaveBinaryCaveMap(IEnumerable<Vector3i> positions)
    {
        var regionGridSize = CaveBuilder.worldSize / CaveBuilder.RegionSize;

        using (var multiStream = new MultiStream("region", create: true))
        {
            foreach (var position in positions)
            {
                short chunk_x = (short)((position.x / 16) - CaveBuilder.worldSize / 32);
                short chunk_z = (short)((position.z / 16) - CaveBuilder.worldSize / 32);

                var blockPos = new Vector3bf(
                    (byte)(position.x % 16),
                    (byte)(position.z % 16),
                    (byte)position.y
                );

                int region_x = position.x / CaveBuilder.RegionSize;
                int region_z = position.z / CaveBuilder.RegionSize;
                int regionID = region_x + region_z * regionGridSize;

                var writer = multiStream.GetWriter($"region_{regionID}.bin");

                writer.Write(chunk_x);
                writer.Write(chunk_z);
                writer.Write(blockPos.value);
            }
        }
    }

}

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

public static class CaveBlockProvider
{
    public static Dictionary<int, CaveRegion> regions;

    public static CaveRegion GetRegion(int worldPos_x, int worldPos_z)
    {
        throw new NotImplementedException();
    }

    public static List<Vector3bf> GetCaveChunk(int worldPos_x, int worldPos_z)
    {
        throw new NotImplementedException();
    }

}
