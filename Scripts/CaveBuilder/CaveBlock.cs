using System;
using System.IO;

public class CaveBlock
{
    public const sbyte defaultDensity = sbyte.MaxValue;

    public Vector2s chunkPos;

    public Vector3bf posInChunk;

    public sbyte density;

    public byte rawData;

    public MutableInt16 tunnelID;

    public int x => (chunkPos.x << 4) + posInChunk.x;

    public int y => posInChunk.y;

    public int z => (chunkPos.z << 4) + posInChunk.z;

    public int HashZX() => HashZX(x, z);

    public static int HashZX(int x, int z) => (x << 14) + z; // x * 16384 + z

    public static void ZXFromHash(int hash, out int x, out int z)
    {
        x = hash >> 14;     // hash / 16384
        z = hash & 16383;   // hash % 16384
    }

    public bool isWater
    {
        get => (rawData & 0b0000_0001) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0000_0001) : (rawData & 0b1111_1110));
    }

    public bool isRoom
    {
        get => (rawData & 0b0000_0010) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0000_0010) : (rawData & 0b1111_1101));
    }

    public bool skipDecoration
    {
        get => (rawData & 0b0000_0100) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0000_0100) : (rawData & 0b1111_1011));
    }

    public bool isRope
    {
        get => (rawData & 0b0000_1000) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0000_1000) : (rawData & 0b1111_0111));
    }

    public bool bool5
    {
        get => (rawData & 0b0001_0000) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0001_0000) : (rawData & 0b1110_1111));
    }

    public bool bool6
    {
        get => (rawData & 0b0010_0000) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0010_0000) : (rawData & 0b1101_1111));
    }

    public bool bool7
    {
        get => (rawData & 0b0100_0000) != 0;
        set => rawData = (byte)(value ? (rawData | 0b0100_0000) : (rawData & 0b1011_1111));
    }

    public bool bool8
    {
        get => (rawData & 0b1000_0000) != 0;
        set => rawData = (byte)(value ? (rawData | 0b1000_0000) : (rawData & 0b0111_1111));
    }

    public CaveBlock() { }

    public CaveBlock(Vector3i position, sbyte density = defaultDensity)
    {
        short chunk_x = (short)(position.x >> 4);
        short chunk_z = (short)(position.z >> 4);

        this.density = density;

        chunkPos = new Vector2s(chunk_x, chunk_z);

        posInChunk = new Vector3bf(
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

        chunkPos = new Vector2s(chunk_x, chunk_z);

        posInChunk = new Vector3bf(
            (byte)(x & 15),
            (byte)y,
            (byte)(z & 15)
        );
    }

    public Vector3i ToVector3i()
    {
        return new Vector3i(x, y, z);
    }

    public override int GetHashCode()
    {
        return GetHashCode(x, y, z);
    }

    public static int GetHashCode(int x, int y, int z)
    {
        return CaveUtils.PositionHashCode(x, y, z);
    }

    public Vector3i ToWorldPos(Vector3i halfWorldSize)
    {
        return ToVector3i() - halfWorldSize;
    }

    public Vector3i ToWorldPos(int halfWorldSize)
    {
        return new Vector3i(
            x - halfWorldSize,
            y,
            z - halfWorldSize
        );
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
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
        if (p1 is null || p2 is null)
        {
            return false;
        }

        return p1.x == p2.x && p1.y == p2.y && p1.z == p2.z;
    }

    public static bool operator !=(CaveBlock p1, CaveBlock p2)
    {
        if (p1 is null || p2 is null)
        {
            return false;
        }

        return p1.x != p2.x || p1.y != p2.y || p1.z != p2.z;
    }

}
