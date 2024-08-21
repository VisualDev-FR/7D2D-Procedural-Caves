using System.IO;
using UnityEngine;

public class CaveBlock
{
    public const sbyte defaultDensity = 0;

    public Vector2s ChunkPos { get; internal set; }

    public Vector3bf BlockChunkPos { get; internal set; }

    public sbyte density;

    public bool isWater;

    public int x => (ChunkPos.x << 4) + BlockChunkPos.x;

    public int y => BlockChunkPos.y;

    public int z => (ChunkPos.z << 4) + BlockChunkPos.z;

    public Vector3i position => new Vector3i(x, y, z);

    public CaveBlock(Vector3i position, sbyte density = defaultDensity)
    {
        short chunk_x = (short)(position.x >> 4);
        short chunk_z = (short)(position.z >> 4);

        this.density = density;
        isWater = false;

        ChunkPos = new Vector2s(chunk_x, chunk_z);

        BlockChunkPos = new Vector3bf(
            (byte)(position.x & 15),
            (byte)position.y,
            (byte)(position.z & 15)
        );
    }

    public CaveBlock(int x, int y, int z, sbyte density = defaultDensity)
    {
        short chunk_x = (short)(x >> 4);
        short chunk_z = (short)(z >> 4);

        this.density = density;
        isWater = false;

        ChunkPos = new Vector2s(chunk_x, chunk_z);

        BlockChunkPos = new Vector3bf(
            (byte)(x & 15),
            (byte)y,
            (byte)(z & 15)
        );
    }

    public CaveBlock(BinaryReader reader)
    {
        ChunkPos = new Vector2s(reader.ReadInt16(), reader.ReadInt16());
        BlockChunkPos = new Vector3bf(reader.ReadUInt16());
        density = reader.ReadSByte();
        isWater = reader.ReadBoolean();
    }

    public void ToBinaryStream(BinaryWriter writer)
    {
        writer.Write(ChunkPos.x);
        writer.Write(ChunkPos.z);
        writer.Write(BlockChunkPos.value);
        writer.Write(density);
        writer.Write(isWater);
    }

    public Vector3i ToVector3i()
    {
        return new Vector3i(x, y, z);
    }

    public void SetWater(bool isWater)
    {
        this.isWater = isWater;
    }

    public override int GetHashCode()
    {
        return GetHashCode(x, y, z);
    }

    public static int GetHashCode(int x, int y, int z)
    {
        // same as Vector3i.GetHashCode(), to make a caveblock comaprable to Vector3i
        return x * 8976890 + y * 981131 + z;
    }

    public override bool Equals(object obj)
    {
        if (obj is CaveBlock other)
        {
            return x == other.x && y == other.y && z == other.z;
        }
        return false;
    }

    public static bool operator ==(CaveBlock p1, CaveBlock p2)
    {
        return p1.x == p2.x && p1.y == p2.y && p1.z == p2.z;
    }

    public static bool operator !=(CaveBlock p1, CaveBlock p2)
    {
        return p1.x != p2.x || p1.y != p2.y || p1.z != p2.z;
    }

    public Vector3i ToWorldPos()
    {
        Vector3i chunkPos = new Vector3i(
            ChunkPos.x - CaveBuilder.worldSize / 32,
            0,
            ChunkPos.z - CaveBuilder.worldSize / 32
        );

        return 16 * chunkPos + BlockChunkPos.ToVector3i();
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public int Index()
    {
        return position.x + CaveBuilder.worldSize * position.z;
    }

}
