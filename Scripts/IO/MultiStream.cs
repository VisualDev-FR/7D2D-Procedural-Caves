using System;
using System.Collections.Generic;
using System.IO;


public class MultiStream : IDisposable
{
    private readonly string path;

    private readonly Dictionary<string, BinaryWriter> writers;

    private readonly Dictionary<string, FileStream> streams;

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

    private BinaryWriter CreateWriter(string name, FileMode filemode = FileMode.Create)
    {
        if (writers.ContainsKey(name))
            throw new InvalidOperationException($"A similar writer is Already open: '{name}'");

        var stream = new FileStream($"{path}/{name}", filemode);
        var writer = new BinaryWriter(stream);

        streams[name] = stream;
        writers[name] = writer;

        return writer;
    }

    public BinaryWriter GetWriter(string name, FileMode filemode = FileMode.Create)
    {
        if (writers.TryGetValue(name, out var writer))
        {
            return writer;
        }

        return CreateWriter(name, filemode);
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
}
