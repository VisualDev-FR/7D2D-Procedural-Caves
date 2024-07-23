using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class Exporter : IDisposable
{
    public int regionSize;

    public string path;

    private Dictionary<int, StreamWriter> writers;

    public Exporter(string path, int regionSize)
    {
        this.path = path;
        this.regionSize = regionSize;
        writers = new Dictionary<int, StreamWriter>();
    }

    private StreamWriter CreateWriter(int hashCode)
    {
        if (writers.ContainsKey(hashCode))
            throw new Exception($"A similar writer is Already open: '{hashCode}'");

        var writer = new StreamWriter($"{path}/region_{hashCode}.csv");

        writers[hashCode] = writer;

        return writer;
    }

    private StreamWriter GetWriter(int hashCode)
    {
        if (writers.TryGetValue(hashCode, out var writer))
        {
            return writer;
        }

        return CreateWriter(hashCode);
    }

    public void Export(HashSet<Vector3i> points)
    {
        foreach (var position in points)
        {
            int chunk_x = (position.x / 16) - CaveBuilder.worldSize / 32;
            int chunk_z = (position.z / 16) - CaveBuilder.worldSize / 32;

            int block_x = position.x - 16 * (position.x / 16);
            int block_z = position.z - 16 * (position.z / 16);
            int block_y = position.y;

            int region_x = position.x / regionSize;
            int region_z = position.z / regionSize;
            int regionID = region_x + region_z * regionSize;

            var writer = GetWriter(regionID);
            writer.WriteLine($"{chunk_x} {chunk_z} {block_x} {block_y} {block_z}");
        }
    }

    public void Dispose()
    {
        foreach (var writer in writers.Values)
        {
            writer.Dispose();
        }
    }

    public static void Demo(string[] args)
    {
        CaveBuilder.worldSize = 10240 * 2;

        var points = new HashSet<Vector3i>();

        Log.Out("Start creating random datas...");

        for (int i = 0; i < 50_000_000; i++)
        {
            points.Add(new Vector3i(
                CaveBuilder.rand.Next(0, CaveBuilder.worldSize),
                CaveBuilder.rand.Next(0, 255),
                CaveBuilder.rand.Next(0, CaveBuilder.worldSize)
            ));
        }

        Log.Out("Start importing datas...");

        var timer = new Stopwatch();
        timer.Start();

        using (var exporter = new Exporter("region", 1024))
        {
            exporter.Export(points);
        }

        Log.Out($"{points.Count} blocks imported, timer: {CaveUtils.TimeFormat(timer)}");
    }

}
